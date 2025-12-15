// See https://aka.ms/new-console-template for more information

using Onyx.Core;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var board = new Board();
        board.ApplyMove(new Move(Piece.WP,"a2a4"));
        board.ApplyMove(new Move(Piece.BP,"a7a5"));
        PerftSearcher.PerftDivide(board,1);
    }
}