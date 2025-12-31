using Onyx.Statics;
using Onyx.UCI;

namespace Onyx.Core;

public class Engine
{
    public static string Version => "0.6.0";

    // data members
    public Board Board = new();
    private TranspositionTable TranspositionTable { get; } = new();
    private TimerManager TimerManager { get; set; } = new();
    private const int MateScore = 30000;
    private CancellationToken _ct; // for threading

    // search members
    private SearchStatistics _statistics;
    private int _currentSearchId;

    public string Position => Board.GetFen();

    // UCI Interface methods
    public void SetPosition(string fen)
    {
        Board = new Board(fen);
    }

    public ulong Perft(int depth)
    {
        return PerftSearcher.GetPerftResults(board: Board, depth);
    }

    public void PerftDivide(int depth)
    {
        PerftSearcher.PerftDivide(Board, depth);
    }

    public void Reset()
    {
        _statistics = new SearchStatistics();
        Board = new Board();
        TimerManager = new TimerManager();
    }

    public SearchResults Search(SearchParameters searchParameters)
    {
        _statistics = new SearchStatistics();
        _currentSearchId++;
        _ct = searchParameters.CancellationToken;

        long timeLimit = long.MaxValue;
        var isTimed = false;
        if (searchParameters.TimeLimit.HasValue)
        {
            timeLimit = searchParameters.TimeLimit.Value;
            isTimed = true;
        }

        else if (searchParameters.TimeControl.HasValue)
        {
            timeLimit = TimeBudgetPerMove(searchParameters.TimeControl.Value);
            isTimed = true;
        }

        TimerManager.Start(timeLimit);
        int depthLimit = searchParameters.MaxDepth ?? 100;
        return IterativeDeepeningSearch(depthLimit, isTimed);
    }

    private SearchResults IterativeDeepeningSearch(int depthLimit, bool isTimed)
    {
        Move bestMove = default;
        var bestScore = 0;

        for (var depth = 1; depth <= depthLimit; depth++)
        {
            // time out
            if (isTimed && TimerManager.ShouldStop)
            {
                _statistics.RunTime = TimerManager.Elapsed;
                return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics };
            }

            // stop flag thrown from gui, return best so far
            if (_ct.IsCancellationRequested)
                return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics };

            var searchResult = ExecuteSearch(depth, isTimed);
            if (!searchResult.completed) continue;

            bestMove = searchResult.bestMove;
            bestScore = searchResult.score;
            _statistics.Depth = depth;
        }

        _statistics.RunTime = TimerManager.Elapsed;
        Logger.Log(LogType.EngineLog, _statistics);

        return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics };
    }


    // timed saerch methods
    private int TimeBudgetPerMove(TimeControl timeControl)
    {
        var time = Board.WhiteToMove ? timeControl.Wtime : timeControl.Btime;
        var increment = Board.WhiteToMove ? timeControl.Winc : timeControl.Binc;

        var safeInc = increment ?? 0;

        int movesToGo = timeControl.movesToGo ?? MovesRemaining(Board);

        var baseTime = time / movesToGo + safeInc * 0.8;

        // use max of 20% remaining time
        var safeMax = time * 0.2;
        int finalBudget = (int)Math.Min(baseTime!.Value, safeMax!.Value);
        
        return Math.Max(finalBudget, 50); 
    }

    private static int MovesRemaining(Board board)
    {
        var ply = board.FullMoves * 2;

        if (ply < 20) return 40; // opening
        if (ply < 60) return 30; // middlegame
        return 20; // endgame
    }

    private int CalculateRemainingTime(int remainingTimeForTurnToMove)
    {
        // keep 5% or 100ms, whichever is larger
        return Math.Max(remainingTimeForTurnToMove - Math.Max(remainingTimeForTurnToMove / 20, 100), 0);
    }


    private (bool completed, Move bestMove, int score)
        ExecuteSearch(int depth, bool timed)
    {
        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(Board, moveBuffer);
        Span<Move> moves = moveBuffer[..moveCount];
        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;

        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, Board)) continue;
            Board.ApplyMove(move);
            var result = AlphaBeta(depth - 1, -beta, -alpha, Board, timed, 1);
            Board.UndoMove(move);
            if (!result.Completed)
                return (false, default, 0);

            var score = -result.Value;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, score);
        }

        return (true, bestMove, bestScore);
    }

    private SearchFlag AlphaBeta(int depth, int alpha, int beta, Board board, bool timed, int ply)
    {
        if ((_statistics.Nodes & 2047) == 0)
        {
            if ((timed && TimerManager.ShouldStop) || _ct.IsCancellationRequested)
                return SearchFlag.Abort;
        }

        _statistics.Nodes++;

        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(board, moveBuffer);
        Span<Move> moves = moveBuffer[..moveCount];
        // no moves, either checkmate or stalemate
        if (moveCount == 0)
        {
            return Referee.IsCheckmate(board)
                ? new SearchFlag(true, -(MateScore - ply))
                : new SearchFlag(true, 0); // stalemate
        }

        // leaf node
        if (depth == 0)
        {
            if (Referee.IsCheckmate(board))
            {
                return new SearchFlag(true, -(MateScore - ply));
            }

            return new SearchFlag(true, Evaluator.Evaluate(board));
        }

        var alphaOrig = alpha;
        var bestValue = int.MinValue + 1;

        // ---- TT probe ----
        var hash = board.Zobrist.HashValue;
        if (TTProbe(depth, alpha, beta, hash, out var searchResult, out var ttMove))
        {
            if (Math.Abs(searchResult.Value) >= MateScore)
                Console.WriteLine("Excessive TT hit");
            else
            {
                return searchResult;
            }
        }

        Evaluator.SortMoves(moves, ttMove, board);

        Move bestMove = default;
        var legalMoveCount = 0;
        // ---- main loop ----
        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, board)) continue;
            legalMoveCount++;
            board.ApplyMove(move);
            var child = AlphaBeta(depth - 1, -beta, -alpha, board, timed, ply + 1);
            board.UndoMove(move);

            if (!child.Completed)
                return SearchFlag.Abort;

            var eval = -child.Value;

            if (eval > bestValue)
            {
                bestValue = eval;
                bestMove = move;
            }

            if (eval > alpha)
                alpha = eval;

            if (alpha < beta) continue;

            _statistics.BetaCutoffs++;
            break;
        }

        // No legal moves were found in the loop -- need to make sure we've cleared the nonsense Int.MaxValue score.
        var endGameScore = 0;
        var endGameScoreModified = false;
        if (legalMoveCount == 0)
        {
            endGameScoreModified = true;
            if (Referee.IsCheckmate(board))
                endGameScore = -(MateScore - ply);
            else
                endGameScore = 0;
        }

        // only store in the transposition table if legal moves were found
        BoundFlag flag;
        if (endGameScoreModified)
            bestValue = endGameScore;
        if (bestValue <= alphaOrig)
            flag = BoundFlag.Upper;
        else if (bestValue >= beta)
            flag = BoundFlag.Lower;
        else
            flag = BoundFlag.Exact;

        TranspositionTable.Store(hash, bestValue, depth, _currentSearchId, flag, bestMove);
        _statistics.TtStores++;


        return new SearchFlag(true, bestValue);
    }

    private bool TTProbe(int depth, int alpha, int beta, ulong hash, out SearchFlag searchFlag, out Move bestMove)
    {
        var entry = TranspositionTable.Retrieve(hash);

        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            switch (entry.Value.BoundFlag)
            {
                case BoundFlag.Exact:
                    _statistics.TtHits++;
                    searchFlag = new SearchFlag(true, entry.Value.Eval);
                    bestMove = entry.Value.bestMove;
                    return true;

                case BoundFlag.Upper:
                    if (entry.Value.Eval <= alpha)
                    {
                        _statistics.TtHits++;
                        searchFlag = new SearchFlag(true, entry.Value.Eval);
                        bestMove = entry.Value.bestMove;
                        return true;
                    }

                    break;

                case BoundFlag.Lower:
                    if (entry.Value.Eval >= beta)
                    {
                        _statistics.TtHits++;
                        searchFlag = new SearchFlag(true, entry.Value.Eval);
                        bestMove = entry.Value.bestMove;
                        return true;
                    }

                    break;
            }
        }

        searchFlag = default;
        bestMove = default;
        return false;
    }

    internal readonly struct SearchFlag
    {
        public bool Completed { get; }
        public int Value { get; }

        public SearchFlag(bool completed, int value)
        {
            Completed = completed;
            Value = value;
        }

        public static SearchFlag Abort => new(false, 0);
    }
}