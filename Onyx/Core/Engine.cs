
using Onyx.Statics;

namespace Onyx.Core;

public class Engine
{
    public Board board;
    
    public string Position => board.GetFen();
    public Engine()
    {
        board = new Board();
    }

    public void SetPosition(string fen)
    {
        board = new Board(fen);
    }

    public ulong Perft(int depth)
    {
        return PerftSearcher.GetPerftResults(board: board, depth);
    }

    public (Move bestMove, int score) Search(int depth)
    {
        var moves = MoveGenerator.GetLegalMoves(board);
        if (moves.Count == 0)
            throw new InvalidOperationException("No Moves");

        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;
        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var score = -AlphaBeta(depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            alpha = Math.Max(alpha, score);
        }

        return (bestMove, bestScore);
    }

    private int AlphaBeta(int depth, int alpha, int beta)
    {
        var fenPreGen = board.GetFen();
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
   
        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var eval = -AlphaBeta(depth - 1, -beta, -alpha);
            board.UndoMove(move);

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
                break;
        }

        return maxEval;
    }

    private const int MATE_SCORE = 30000;
}