using Onyx.Statics;

namespace Onyx.Core;

internal struct SearchStatistics
{
    public int CurrentSearchId;
    public int Nodes;
    public int TtHits;
    public int TtStores;
    public int BetaCutoffs;
    public TimeSpan RunTime;

    public override string ToString()
    {
        return $"Search: {CurrentSearchId}, Nodes Searched: {Nodes}, Time (ms): {RunTime.TotalMilliseconds} , TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}";
    }
}

public class Engine
{
    public Board Board;
    public TranspositionTable TranspositionTable { get; private set; }
    public string Position => Board.GetFen();

    private SearchStatistics _statistics;

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

    public (Move bestMove, int score) Search(int depth)
    {
        var startTime = System.Diagnostics.Stopwatch.StartNew();
        _statistics = new SearchStatistics();
        _statistics.CurrentSearchId++;
        var moves = MoveGenerator.GetLegalMoves(Board);
        if (moves.Count == 0)
            throw new InvalidOperationException("No Moves");

        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;
        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
            Board.ApplyMove(move);
            var score = -AlphaBeta(depth - 1, -beta, -alpha, Board);
            Board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, score);
        }
        
        startTime.Stop();
        _statistics.RunTime = startTime.Elapsed;

        Console.WriteLine(_statistics);
        return (bestMove, bestScore);
    }

    private int AlphaBeta(int depth, int alpha, int beta, Board board)
    {
        _statistics.Nodes++;
        var moves = MoveGenerator.GetLegalMoves(board);

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
        var entry = TranspositionTable.Retrieve(currentHash);
        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            _statistics.TtHits++;
            switch (entry.Value.BoundFlag)
            {
                case BoundFlag.Exact:
                    _statistics.TtHits++;
                    return entry.Value.Eval;

                case BoundFlag.Upper:
                    if (entry.Value.Eval <= alpha)
                    {
                        _statistics.TtHits++;
                        return entry.Value.Eval;
                    }
                    break;

                case BoundFlag.Lower:
                    if (entry.Value.Eval >= beta)
                    {
                        _statistics.TtHits++;
                        return entry.Value.Eval;
                    }
                    break;
            }
        }

        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var eval = -AlphaBeta(depth - 1, -beta, -alpha, board);
            board.UndoMove(move);

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                _statistics.BetaCutoffs++;
                break;
            }
        }

        BoundFlag flag;
        if (maxEval < startingAlpha) flag = BoundFlag.Upper;
        else
        {
            flag = maxEval >= beta ? BoundFlag.Lower : BoundFlag.Exact;
        }

        TranspositionTable.Store(currentHash, maxEval, depth, _statistics.CurrentSearchId, flag);
        _statistics.TtStores++;

        return maxEval;
    }

    private const int MateScore = 30000;
}