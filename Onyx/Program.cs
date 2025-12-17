// See https://aka.ms/new-console-template for more information

using Onyx.Core;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var board = new Board(Fen.KiwiPeteFen);
        board.ApplyMove(new Move(Piece.WN,"e5g6"));
        board.ApplyMove(new Move(Piece.BP,"h3g2"));
        board.ApplyMove(new Move(Piece.WN,"g6h8"));
        // // //
        PerftSearcher.PerftDivide(board,1);
    }
}