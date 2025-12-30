using System.Diagnostics;
using Onyx.Statics;
using Onyx.UCI;


namespace Onyx.Core;

public struct SearchStatistics : ILoggable
{
    public int Nodes;
    public int TtHits;
    public int TtStores;
    public int BetaCutoffs;
    public long RunTime;
    public int Depth;

    public string Get()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)}";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)}";
    }
}

public readonly struct SearchResult
{
    public bool Completed { get; }
    public int Value { get; }

    public SearchResult(bool completed, int value)
    {
        Completed = completed;
        Value = value;
    }

    public static SearchResult Abort => new(false, 0);
}

internal class TimerManager
{
    private Stopwatch _stopwatch = null!;
    private bool _started;
    private long _milliseconds;

    public void Start(long milliseconds_)
    {
        _stopwatch = Stopwatch.StartNew();
        _milliseconds = milliseconds_;
        _started = true;
    }

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
        _started = true;
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _started = false;
        _milliseconds = 0;
    }

    public long Elapsed => _stopwatch.ElapsedMilliseconds;

    public bool ShouldStop
    {
        get
        {
            if (!_started)
                return false;
            return _stopwatch.ElapsedMilliseconds > _milliseconds;
        }
    }
}

public class Engine
{
    public Board Board;
    public TranspositionTable TranspositionTable { get; private set; }
    public string Position => Board.GetFen();

    private SearchStatistics _statistics;
    private int _currentSearchId;
    private TimerManager _timerManager = new();
    public string Version { get; } = "0.5.0";

    public Engine()
    {
        Board = new Board();
        TranspositionTable = new TranspositionTable();
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

    public (Move bestMove, int score, SearchStatistics stats) TimedSearch(int depth, long timeMS)
    {
        _timerManager.Start(timeMS);
        _statistics = new SearchStatistics();
        _currentSearchId++;

        Move bestMove = default;
        var bestScore = 0;

        for (var i = 1; i <= depth; i++)
        {
            // time out
            if (_timerManager.ShouldStop)
            {
                _statistics.RunTime = _timerManager.Elapsed;
                return (bestMove, bestScore, _statistics);
            }

            var searchResult = ExecuteSearch(i, true);
            if (!searchResult.completed) continue;

            bestMove = searchResult.bestMove;
            bestScore = searchResult.score;
            _statistics.Depth = i;
        }

        _statistics.RunTime = _timerManager.Elapsed;

        Logger.Log(LogType.EngineLog, _statistics);
        return (bestMove, bestScore, _statistics);
    }

    public (Move bestMove, int score, SearchStatistics stats) CalcAndDispatchTimedSearch(int depth,
        TimeControl timeControl)
    {
        var relevantTimeControl = Board.TurnToMove == Colour.White ? timeControl.Wtime : timeControl.Btime;

        if (relevantTimeControl is null)
        {
            var result = DepthSearch(depth);
            _statistics.Depth = depth;
            return (result.bestMove, result.score, _statistics);
        }

        var timeBudgetPerMove = TimeBudgetPerMove(timeControl, relevantTimeControl);
        var searchResult = TimedSearch(20, timeBudgetPerMove);
        return (searchResult.bestMove, searchResult.score, _statistics);
    }

    private int TimeBudgetPerMove(TimeControl timeControl, int? relevantTimeControl)
    {
        var xMovesRemaining = MovesRemaining(Board, timeControl);
        var timeBudgetPerMove = CalculateRemainingTime(relevantTimeControl.Value);

        var baseTime = timeBudgetPerMove / xMovesRemaining;
        var maxTime = timeBudgetPerMove / 3;

        return Math.Clamp(baseTime, 100, maxTime);
    }

    private static int MovesRemaining(Board board, TimeControl tc)
    {
        var ply = board.FullMoves * 2;

        if (ply < 40) return 30; // opening
        if (ply < 80) return 20; // middlegame
        return 12; // endgame
    }

    private int CalculateRemainingTime(int remainingTimeForTurnToMove)
    {
        // keep 5% or 100ms, whichever is larger
        return Math.Max(remainingTimeForTurnToMove - Math.Max(remainingTimeForTurnToMove / 20, 100), 0);
    }

    public (Move bestMove, int score) DepthSearch(int depth)
    {
        _timerManager.Start();
        _statistics = new SearchStatistics
        {
            Depth = depth
        };
        _currentSearchId++;

        var searchResult = ExecuteSearch(depth, false);
        _statistics.RunTime = _timerManager.Elapsed;
        Logger.Log(LogType.EngineLog, _statistics);
        return (searchResult.bestMove, searchResult.score);
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

    private SearchResult AlphaBeta(int depth,
        int alpha,
        int beta,
        Board board,
        bool timed, int ply)
    {
        if (timed && _timerManager.ShouldStop)
            return SearchResult.Abort;

        _statistics.Nodes++;

        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(board, moveBuffer);
        Span<Move> moves = moveBuffer[..moveCount];
        // no moves, either checkmate or stalemate
        if (moveCount == 0)
        {
            return Referee.IsCheckmate(board)
                ? new SearchResult(true, -(MateScore - ply))
                : new SearchResult(true, 0); // stalemate
        }

        // leaf node
        if (depth == 0)
        {
            if (Referee.IsCheckmate(board))
            {
                return new SearchResult(true, -(MateScore - ply));
            }

            ;
            return new SearchResult(true, Evaluator.Evaluate(board));
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

        Evaluator.SortMoves(moves, ttMove);

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
                return SearchResult.Abort;

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


        return new SearchResult(true, bestValue);
    }

    private bool TTProbe(int depth, int alpha, int beta, ulong hash, out SearchResult searchResult, out Move bestMove)
    {
        var entry = TranspositionTable.Retrieve(hash);

        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            switch (entry.Value.BoundFlag)
            {
                case BoundFlag.Exact:
                    _statistics.TtHits++;
                    searchResult = new SearchResult(true, entry.Value.Eval);
                    bestMove = entry.Value.bestMove;
                    return true;

                case BoundFlag.Upper:
                    if (entry.Value.Eval <= alpha)
                    {
                        _statistics.TtHits++;
                        searchResult = new SearchResult(true, entry.Value.Eval);
                        bestMove = entry.Value.bestMove;
                        return true;
                    }

                    break;

                case BoundFlag.Lower:
                    if (entry.Value.Eval >= beta)
                    {
                        _statistics.TtHits++;
                        searchResult = new SearchResult(true, entry.Value.Eval);
                        bestMove = entry.Value.bestMove;
                        return true;
                    }
                    break;
            }
        }

        searchResult = default;
        bestMove = default;
        return false;
    }

    private const int MateScore = 30000;

    public void Reset()
    {
        _statistics = new SearchStatistics();
        Board = new Board();
        _timerManager = new TimerManager();
    }
}