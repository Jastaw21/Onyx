// See https://aka.ms/new-console-template for more information

using Onyx.Core;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var board = new Board(Fen.KiwiPeteFen);
        PerftSearcher.PerftDivide(board,4);
    }
}