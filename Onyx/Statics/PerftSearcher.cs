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
    public static ulong GetPerftResults(Position position, int depth)
    {
        return ParallelPerft(position, depth);
    }

    public static ulong ParallelPerft(Position position, int depth)
    {
        if (depth == 0)
            return 1;

        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(position.WhiteToMove, position, moveBuffer);
        var moves = moveBuffer[..moveCount].ToArray(); // Convert to array for Parallel.ForEach
        ulong total = 0;
        object lockObj = new();

        Parallel.ForEach(moves, move =>
        {
            var localPosition = position.Clone();
            localPosition.ApplyMove(move);
            if (!Referee.IsInCheck(position.WhiteToMove, localPosition))
            {
                var count = ExecutePerft(localPosition, depth - 1);
                lock (lockObj)
                    total += count;
            }
        });

        return total;
    }

    public static void PerftDivide(Position position, int depth)
    {
        ulong total = 0;

        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(position.WhiteToMove, position, moveBuffer);
        var moves = moveBuffer[..moveCount];

        foreach (var move in moves)
        {
            var side = position.WhiteToMove;
            
            position.ApplyMove(move);

            if (!Referee.IsInCheck(side, position))
            {
                var nodes = ExecutePerft(position, depth - 1);
                total += nodes;
                Console.WriteLine($"{move}: {nodes}");
            }
            position.UndoMove(move);
           
        }

        Console.WriteLine($"Total: {total}");
    }


    private static ulong ExecutePerft(Position position, int depth)
    {
        var results = 0ul;
        if (depth == 0) return 1ul;
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(position, moveBuffer);
        var moves = moveBuffer[..moveCount];
        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, position))
                continue;
            
            var sideToMve = position.WhiteToMove;
            position.ApplyMove(move);

            if (!Referee.IsInCheck(sideToMve, position))
            {
                var childResults = ExecutePerft(position, depth - 1);
                results += childResults;
            }
            position.UndoMove(move);
        }

        return results;
    }
}