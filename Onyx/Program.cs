// See https://aka.ms/new-console-template for more information

using Onyx.Core;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var board = new Board(Fen.KiwiPeteFen);
        board.ApplyMove(new Move(Piece.WN,"e5d7"));
        //board.ApplyMove(new Move(Piece.BP,"b7b6"));
        PerftSearcher.PerftDivide(board,1);
    }
}