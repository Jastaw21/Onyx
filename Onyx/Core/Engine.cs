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
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime} , TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs},b vn ";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime} , TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}";
    }
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
    private int _currentSearchID = 0;
    private TimerManager _timerManager = new TimerManager();

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

    private (Move bestMove, int score) Search(int depth, long timeMS)
    {
        _timerManager.Start(timeMS);
        _statistics = new SearchStatistics();
        _currentSearchID++;
        Move bestMove = default;

        var bestScore = 0;

        for (var i = 1; i <= depth; i++)
        {
            // time out
            if (_timerManager.ShouldStop)
                return (bestMove, bestScore);
            (Move bestMove, int score) searchResult = ExecuteSearch(depth, true);
            bestMove = searchResult.bestMove;
            bestScore = searchResult.score;
            _statistics.Depth = depth;
        }

        _statistics.RunTime = _timerManager.Elapsed;

        Logger.Log(LogType.EngineLog, _statistics);
        return (bestMove, bestScore);
    }

    public (Move bestMove, int score, SearchStatistics stats) RequestSearch(int depth, TimeControl timeControl)
    {
        if (timeControl is not { Btime: not null, Wtime: not null })
        {
            var result = Search(depth);
            _statistics.Depth = depth;
            return (result.bestMove, result.score, _statistics);
        }

        var relevantTime = Board.TurnToMove == Colour.White ? timeControl.Wtime : timeControl.Btime;
        var xMovesRemaining = timeControl.movesToGo ?? 40; // always assume 5 moves remaining??
        var timeBudgetPerMove = relevantTime.Value / xMovesRemaining;
        var searchResult = Search(depth, timeBudgetPerMove);
        return (searchResult.bestMove, searchResult.score, _statistics);
    }

    private (Move bestMove, int score) Search(int depth)
    {
        _timerManager.Start();
        _statistics = new SearchStatistics();
        _currentSearchID++;

        (Move bestMove, int score) searchResult = ExecuteSearch(depth, false);

        _statistics.RunTime = _timerManager.Elapsed;
        Logger.Log(LogType.EngineLog, _statistics);
        return (searchResult.bestMove, searchResult.score);
    }

    private (Move bestMove, int score) ExecuteSearch(int depth, bool timed)
    {
        List<Move> moves = MoveGenerator.GetLegalMoves(Board);
        if (moves.Count == 0)
            throw new InvalidOperationException("No Moves");

        Move bestMove = moves[0];
        var bestScore = int.MinValue + 1;
        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (Move move in moves)
        {
            Board.ApplyMove(move);
            var score = -AlphaBeta(depth - 1, -beta, -alpha, Board, timed);
            Board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, score);
        }

        return (bestMove, bestScore);
    }

    private int AlphaBeta(int depth, int alpha, int beta, Board board, bool timed)
    {
        if (timed && _timerManager.ShouldStop)
            return 0;
        _statistics.Nodes++;
        List<Move> moves = MoveGenerator.GetLegalMoves(board);

        if (moves.Count == 0)
        {
            if (Referee.IsCheckmate(board))
                return -MateScore;
            return 0;
        }

        if (depth == 0)
            return Evaluator.Evaluate(board);

        var maxEval = int.MinValue + 1;
        var startingAlpha = alpha;

        var currentHash = board.Zobrist.HashValue;
        TranspositionTableEntry? entry = TranspositionTable.Retrieve(currentHash);
        if (RetrieveTTEntry(out var alphaBeta)) return alphaBeta;

        foreach (Move move in moves)
        {
            board.ApplyMove(move);
            var eval = -AlphaBeta(depth - 1, -beta, -alpha, board, timed);
            board.UndoMove(move);

            if (timed && _timerManager.ShouldStop)
                return maxEval;

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                _statistics.BetaCutoffs++;
                break;
            }
        }

        StoreTTEntry();

        return maxEval;

        void StoreTTEntry()
        {
            BoundFlag flag;
            if (maxEval < startingAlpha) flag = BoundFlag.Upper;
            else
            {
                flag = maxEval >= beta ? BoundFlag.Lower : BoundFlag.Exact;
            }

            TranspositionTable.Store(currentHash, maxEval, depth, _currentSearchID, flag);
            _statistics.TtStores++;
        }

        bool RetrieveTTEntry(out int valueEval)
        {
            if (entry.HasValue && entry.Value.Depth >= depth)
            {
                switch (entry.Value.BoundFlag)
                {
                    case BoundFlag.Exact:
                        _statistics.TtHits++;
                        valueEval = entry.Value.Eval;
                        return true;

                    case BoundFlag.Upper:
                        if (entry.Value.Eval <= alpha)
                        {
                            _statistics.TtHits++;
                            valueEval = entry.Value.Eval;
                            return true;
                        }

                        break;

                    case BoundFlag.Lower:
                        if (entry.Value.Eval >= beta)
                        {
                            _statistics.TtHits++;
                            valueEval = entry.Value.Eval;
                            return true;
                        }

                        break;
                }
            }

            valueEval = 0;
            return false;
        }
    }

    private const int MateScore = 30000;

    public void Reset()
    {
        _statistics = new SearchStatistics();
        Board = new Board();
        _timerManager = new TimerManager();
    }
}