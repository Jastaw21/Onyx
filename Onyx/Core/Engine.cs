using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime} , TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs},b vn ";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime} , TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}";
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
    private Stopwatch _stopwatch;
    private bool _started;
    private long milliseconds;

    public void Start(long milliseconds_)
    {
        _stopwatch = Stopwatch.StartNew();
        milliseconds = milliseconds_;
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
        milliseconds = 0;
    }

    public long Elapsed => _stopwatch.ElapsedMilliseconds;

    public bool ShouldStop
    {
        get
        {
            if (!_started)
                return false;
            return _stopwatch.ElapsedMilliseconds > milliseconds;
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
    private bool _depthCompleted;
    public string Version { get; } = "0.2.1";

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
        var searchResult = TimedSearch(depth, timeBudgetPerMove);
        return (searchResult.bestMove, searchResult.score, _statistics);
    }

    private static int TimeBudgetPerMove(TimeControl timeControl, [DisallowNull] int? relevantTimeControl)
    {
        var xMovesRemaining = timeControl.movesToGo ?? 5; // always assume 5 moves remaining??
        var timeBudgetPerMove = relevantTimeControl.Value / xMovesRemaining;
        return timeBudgetPerMove;
    }

    public (Move bestMove, int score) DepthSearch(int depth)
    {
        _timerManager.Start();
        _statistics = new SearchStatistics();
        _currentSearchId++;

        var searchResult = ExecuteSearch(depth, false);

        _statistics.RunTime = _timerManager.Elapsed;
        Logger.Log(LogType.EngineLog, _statistics);
        return (searchResult.bestMove, searchResult.score);
    }

    private (bool completed, Move bestMove, int score)
        ExecuteSearch(int depth, bool timed)
    {
        var moves = MoveGenerator.GetLegalMoves(Board);
        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;

        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
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
        var moves = MoveGenerator.GetLegalMoves(board);
        // no moves, either checkmate or stalemate
        if (moves.Count == 0)
        {
            return Referee.IsCheckmate(board)
                ? new SearchResult(true, -(MateScore - ply))
                : new SearchResult(true, 0); // stalemate
        }

        // leaf node
        if (depth == 0)
            return new SearchResult(true, Evaluator.Evaluate(board));

        var alphaOrig = alpha;
        var bestValue = int.MinValue + 1;

        // ---- TT probe ----
        var hash = board.Zobrist.HashValue;
        if (TTProbe(depth, alpha, beta, hash, out var searchResult)) return searchResult;

        // ---- main loop ----
        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var child = AlphaBeta(depth - 1, -beta, -alpha, board, timed, ply+1);
            board.UndoMove(move);
            
            if (!child.Completed)
                return SearchResult.Abort;

            var eval = -child.Value;

            if (eval > bestValue)
                bestValue = eval;

            if (eval > alpha)
                alpha = eval;

            if (alpha < beta) continue;
            
            _statistics.BetaCutoffs++;
            break;
        }
        
        BoundFlag flag;
        if (bestValue <= alphaOrig)
            flag = BoundFlag.Upper;
        else if (bestValue >= beta)
            flag = BoundFlag.Lower;
        else
            flag = BoundFlag.Exact;

        TranspositionTable.Store(hash, bestValue, depth, _currentSearchId, flag);
        _statistics.TtStores++;

        return new SearchResult(true, bestValue);
    }

    private bool TTProbe(int depth, int alpha, int beta, ulong hash, out SearchResult searchResult)
    {
        var entry = TranspositionTable.Retrieve(hash);

        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            switch (entry.Value.BoundFlag)
            {
                case BoundFlag.Exact:
                    _statistics.TtHits++;
                    searchResult = new SearchResult(true, entry.Value.Eval);
                    return true;

                case BoundFlag.Upper:
                    if (entry.Value.Eval <= alpha)
                    {
                        _statistics.TtHits++;
                        searchResult = new SearchResult(true, entry.Value.Eval);
                        return true;
                    }
                    break;

                case BoundFlag.Lower:
                    if (entry.Value.Eval >= beta)
                    {
                        _statistics.TtHits++;
                        searchResult = new SearchResult(true, entry.Value.Eval);
                        return true;
                    }
                    break;
            }
        }

        searchResult = default;
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