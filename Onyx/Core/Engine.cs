using Onyx.Statics;
using Onyx.UCI;

namespace Onyx.Core;

public class TimeManager(Engine engine)
{
    public int TimeBudgetPerMove(TimeControl timeControl)
    {
        var time = engine.Board.WhiteToMove ? timeControl.Wtime : timeControl.Btime;
        var increment = engine.Board.WhiteToMove ? timeControl.Winc : timeControl.Binc;

        var safeInc = increment ?? 0;

        var calcMovesRemaining = MovesRemaining(engine.Board);
        var instructedMovesRemaining = timeControl.movesToGo ?? 0;

        // if moves remaining feels nonsense, use our own calc
        var movesToGo =
            Math.Abs(instructedMovesRemaining - calcMovesRemaining) > 5
                ? calcMovesRemaining
                : instructedMovesRemaining;
        var baseTime = time / movesToGo + safeInc * 0.8;

        // use max of 20% remaining time
        var safeMax = time * 0.2;
        var finalBudget = (int)Math.Min(baseTime!.Value, safeMax!.Value);

        var timeBudgetPerMove = Math.Max(finalBudget, 50);
        return timeBudgetPerMove;
    }

    private static int MovesRemaining(Board board)
    {
        var ply = board.FullMoves * 2;

        if (ply < 20) return 40; // opening
        if (ply < 60) return 30; // middlegame
        return 20; // endgame
    }
}

public class Engine
{
    public static string Version => "0.7.2";

    // data members
    public Board Board = new();
    private TranspositionTable TranspositionTable { get; } = new();
    private StopwatchManager StopwatchManager { get; set; } = new();
    private const int MateScore = 30000;
    private CancellationToken _ct; // for threading
    private Move?[,] _killerMoves = new Move?[128,2];
    
    // search members
    private SearchStatistics _statistics;
    private int _currentSearchId;
    private bool _loggingEnabled;
    private readonly TimeManager _timeManager;

    public Engine()
    {
        _timeManager = new TimeManager(this);
    }

    // UCI Interface methods
    public void SetLogging(bool enabled)
    {
        _loggingEnabled = enabled;
        Evaluator.LoggingEnabled = enabled;
    }

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
        StopwatchManager = new StopwatchManager();
        _killerMoves = new Move?[128,2];
    }

    private void StoreKillerMove(Move move, int ply)
    {
        var existingMove = _killerMoves[ply,0];
        if (existingMove == null)
        {
            _killerMoves[ply, 0] = move;
            return;
        };
        
        // don't store the same move twice
        if (existingMove!.Value.Data == move.Data)
            return;
        
        _killerMoves[ply,0] = move;
        _killerMoves[ply,1] = existingMove;
        
    }
    
    
    public SearchResults Search(SearchParameters searchParameters)
    {
        _statistics = new SearchStatistics();
        _currentSearchId++;
        _ct = searchParameters.CancellationToken;

        var timeLimit = long.MaxValue;
        var isTimed = false;
        if (searchParameters.TimeLimit.HasValue)
        {
            timeLimit = searchParameters.TimeLimit.Value;
            isTimed = true;
        }

        else if (searchParameters.TimeControl.HasValue)
        {
            timeLimit = _timeManager.TimeBudgetPerMove(searchParameters.TimeControl.Value);
            isTimed = true;
        }

        StopwatchManager.Start(timeLimit);
        var depthLimit = searchParameters.MaxDepth ?? 100;
        return IterativeDeepeningSearch(depthLimit, isTimed);
    }

    private SearchResults IterativeDeepeningSearch(int depthLimit, bool isTimed)
    {
        Move bestMove = default;
        var bestScore = 0;

        for (var depth = 1; depth <= depthLimit; depth++)
        {
            // time out
            if (isTimed && StopwatchManager.ShouldStop)
            {
                _statistics.RunTime = StopwatchManager.Elapsed;
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

            if (bestScore > MateScore - 100)
            {
                // We found a way to win. No need to look deeper.
                break;
            }
        }

        _statistics.RunTime = StopwatchManager.Elapsed;
        
            Logger.Log(LogType.EngineLog, _statistics);

        return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics };
    }

    private (bool completed, Move bestMove, int score) ExecuteSearch(int depth, bool timed)
    {
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(Board, moveBuffer);
        var moves = moveBuffer[..moveCount];
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

            if (_loggingEnabled)
                Logger.Log(LogType.Search, $"{Board.GetFen()} {move} Score: {score} Depth: {depth}");
            alpha = Math.Max(alpha, score);
        }

        return (true, bestMove, bestScore);
    }

    private SearchFlag AlphaBeta(int depth, int alpha, int beta, Board board, bool timed, int ply)
    {
        if ((_statistics.Nodes & 2047) == 0)
        {
            if ((timed && StopwatchManager.ShouldStop) || _ct.IsCancellationRequested)
                return SearchFlag.Abort;
        }

        _statistics.Nodes++;

        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(board, moveBuffer);
        var moves = moveBuffer[..moveCount];

        // exit if the end-of-game state
        var flagExit = AssessCheckmateOrStalemate(depth, board, ply, moveCount, out var searchFlag);
        if (flagExit) return searchFlag;

        var alphaOrig = alpha;
        var bestValue = int.MinValue + 1;

        // ---- TT probe ----
        var hash = board.Zobrist.HashValue;
        if (TtProbe(depth, alpha, beta, hash, out var searchResult, out var ttMove))
            return searchResult;

        Evaluator.SortMoves(moves, ttMove,_killerMoves,ply);

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

            // beta cutoff
            _statistics.BetaCutoffs++;
            if (move.CapturedPiece == 0)
                StoreKillerMove(move, ply);
            break;
        }

        bestValue = HandleTtStoring(depth, beta, board, ply, legalMoveCount, bestValue, alphaOrig, hash, bestMove);


        return new SearchFlag(true, bestValue);
    }

    private int HandleTtStoring(int depth, int beta, Board board, int ply, int legalMoveCount, int bestValue,
        int alphaOrig,
        ulong hash, Move bestMove)
    {
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
        return bestValue;
    }

    private static bool AssessCheckmateOrStalemate(int depth, Board board, int ply, int moveCount,
        out SearchFlag flag)
    {
        // no moves, either checkmate or stalemate
        if (moveCount == 0)
        {
            flag = Referee.IsCheckmate(board)
                ? new SearchFlag(true, -(MateScore - ply))
                : new SearchFlag(true, 0); // stalemate
            return true;
        }

        // leaf node
        if (depth == 0)
        {
            if (Referee.IsCheckmate(board))
            {
                flag = new SearchFlag(true, -(MateScore - ply));
                return true;
            }

            flag = new SearchFlag(true, Evaluator.Evaluate(board));
            return true;
        }

        flag = default;
        return false;
    }

    private bool TtProbe(int depth, int alpha, int beta, ulong hash, out SearchFlag searchFlag,
        out Move bestMove)
    {
        var entry = TranspositionTable.Retrieve(hash);

        bestMove = default;
        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            bestMove = entry.Value.BestMove;
            switch (entry.Value.BoundFlag)
            {
                case BoundFlag.Exact:
                    _statistics.TtHits++;
                    searchFlag = new SearchFlag(true, entry.Value.Eval);
                    bestMove = entry.Value.BestMove;
                    return true;

                case BoundFlag.Upper:
                    if (entry.Value.Eval <= alpha)
                    {
                        _statistics.TtHits++;
                        searchFlag = new SearchFlag(true, entry.Value.Eval);
                        bestMove = entry.Value.BestMove;
                        return true;
                    }

                    break;

                case BoundFlag.Lower:
                    if (entry.Value.Eval >= beta)
                    {
                        _statistics.TtHits++;
                        searchFlag = new SearchFlag(true, entry.Value.Eval);
                        bestMove = entry.Value.BestMove;
                        return true;
                    }

                    break;
            }
        }

        searchFlag = default;
        return false;
    }

    internal readonly struct SearchFlag(bool completed, int value)
    {
        public bool Completed { get; } = completed;
        public int Value { get; } = value;

        public static SearchFlag Abort => new(false, 0);
    }
}