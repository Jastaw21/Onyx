
using Onyx.Statics;

namespace Onyx.Core;

public class Engine
{
    public Board Board;
    public TranspositionTable TranspositionTable { get; private set; }
    public string Position => Board.GetFen();

    private int CurrentSearchID = 0;
    
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
        CurrentSearchID++;
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

        return (bestMove, bestScore);
    }

    private int AlphaBeta(int depth, int alpha, int beta, Board board)
    {
        var moves = MoveGenerator.GetLegalMoves(Board);

        if (moves.Count == 0)
        {
            if (Referee.IsCheckmate(Board))
                return -MATE_SCORE;

            return 0;
        }
        
        if (depth == 0)
            return Evaluator.Evaluate(Board);

        var maxEval = int.MinValue + 1;
   
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

        return maxEval;
    }

    private const int MATE_SCORE = 30000;
}