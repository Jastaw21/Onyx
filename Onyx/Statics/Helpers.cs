using System.Text;
using Onyx.Core;


namespace Onyx.Statics;

public static class Helpers
{
    public static string MovesToString(List<Move>? moves)
    {
        var sb = new StringBuilder();
        if (moves == null || moves.Count == 0) return sb.ToString();
        foreach (var move in moves)
        {
            sb.Append(move.Notation);
            sb.Append(' ');
        }

        sb.Remove(sb.Length - 1, 1); // remove last space
        return sb.ToString();
    }
}