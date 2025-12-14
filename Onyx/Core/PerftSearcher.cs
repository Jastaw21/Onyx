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
    public static PerftResults GetPerftResults(Board board, int depth)
    {
        return ExecutePerft(board, depth);
    }

    private static PerftResults ExecutePerft(Board board, int depth)
    {
        if (depth == 0)
        {
            return new PerftResults(nodes: 1);
        }

        var results = new PerftResults();
        var movesInPosition = MoveGenerator.GetMoves(board.TurnToMove, ref board);

        foreach (var move in movesInPosition)
        {
            board.ApplyMove(move);
            var childResults = ExecutePerft(board, depth - 1);
            if (depth == 1)
                results += childResults;
            board.UndoMove(move);
        }

        return results;
    }
}