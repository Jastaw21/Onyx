using System.Diagnostics;
using Onyx.Core;

namespace Onyx.Statics;

public struct PerftResults
{
    public PerftResults(int nodes = 0)
    {
        Nodes = nodes;
    }

    public int Nodes = 0;

    public static PerftResults operator +(PerftResults x, PerftResults y)
    {
        return new PerftResults
        {
            Nodes = x.Nodes + y.Nodes
        };
    }
}

public static class PerftSearcher
{
    public static ulong GetPerftResults(Board board, int depth)
    {
        return ParallelPerft(board, depth);
    }

    public static ulong ParallelPerft(Board board, int depth)
    {
        if (depth == 0)
            return 1;

        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(board.WhiteToMove, board, moveBuffer);
        var moves = moveBuffer[..moveCount].ToArray(); // Convert to array for Parallel.ForEach
        ulong total = 0;
        object lockObj = new();

        Parallel.ForEach(moves, move =>
        {
            var localBoard = board.Clone();
            localBoard.ApplyMove(move);
            if (!Referee.IsInCheck(board.WhiteToMove, localBoard))
            {
                var count = ExecutePerft(localBoard, depth - 1);
                lock (lockObj)
                    total += count;
            }
        });

        return total;
    }

    public static void PerftDivide(Board board, int depth)
    {
        ulong total = 0;

        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(board.WhiteToMove, board, moveBuffer);
        var moves = moveBuffer[..moveCount];

        foreach (var move in moves)
        {
            var side = board.WhiteToMove;
            
            board.ApplyMove(move);

            if (!Referee.IsInCheck(side, board))
            {
                var nodes = ExecutePerft(board, depth - 1);
                total += nodes;
                Console.WriteLine($"{move}: {nodes}");
            }
            board.UndoMove(move);
           
        }

        Console.WriteLine($"Total: {total}");
    }


    private static ulong ExecutePerft(Board board, int depth)
    {
        var results = 0ul;
        if (depth == 0) return 1ul;
        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(board, moveBuffer);
        Span<Move> moves = moveBuffer[..moveCount];
        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, board))
                continue;
            
            var sideToMve = board.WhiteToMove;
            board.ApplyMove(move);

            if (!Referee.IsInCheck(sideToMve, board))
            {
                var childResults = ExecutePerft(board, depth - 1);
                results += childResults;
            }
            board.UndoMove(move);
        }

        return results;
    }
}