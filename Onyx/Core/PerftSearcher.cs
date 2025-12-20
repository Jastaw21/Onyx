namespace Onyx.Core;

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
        return ExecutePerft(board, depth);
    }

    public static void PerftDivide(Board board, int depth)
    {
        ulong total = 0;
        var moves = MoveGenerator.GetMoves(board.TurnToMove, board);

        foreach (var move in moves)
        {
            var side = board.TurnToMove;
            board.ApplyMove(move);

            if (!Referee.IsInCheck(side, board))
            {
                ulong nodes = ExecutePerft(board, depth - 1);
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

        var moves = MoveGenerator.GetMoves(board.TurnToMove, board);
        foreach (var move in moves)
        {
            var sideToMve = board.TurnToMove;
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