using Onyx.Statics;

namespace Onyx.Core;

public class Engine
{
    public Board Board;
    public TranspositionTable TranspositionTable { get; private set; }
    public string Position => Board.GetFen();

    private int _currentSearchId = 0;
    private int _ttHits;

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
        _currentSearchId++;
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

        Console.WriteLine(_ttHits);
        return (bestMove, bestScore);
    }

    private int AlphaBeta(int depth, int alpha, int beta, Board board)
    {
        var moves = MoveGenerator.GetLegalMoves(board);

        if (moves.Count == 0)
        {
            if (Referee.IsCheckmate(board))
                return -MATE_SCORE;

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
            _ttHits++;
            return entry.Value.Eval;
        }

        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var eval = -AlphaBeta(depth - 1, -beta, -alpha, board);
            board.UndoMove(move);

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
                break;
        }

        BoundFlag flag;
        if (maxEval < startingAlpha) flag = BoundFlag.Upper;
        else
        {
            flag = maxEval >= beta ? BoundFlag.Lower : BoundFlag.Exact;
        }

        TranspositionTable.Store(currentHash, maxEval, depth, _currentSearchId, flag);

        return maxEval;
    }

    private const int MATE_SCORE = 30000;
}