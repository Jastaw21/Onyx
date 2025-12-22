using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class Perft
{
    [Test]
    public void StartingPos()
    {
        int[] targetNumbers = [20, 400, 8902, 197_281];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board();
            Assert.That(PerftSearcher.GetPerftResults(board, depth + 1), Is.EqualTo(targetNumbers[depth]));
        }
    }
    
    [Test]
    public void ParallelPerftSame()
    {
        int[] targetNumbers = [20, 400, 8902, 197_281];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board();
            var singleThread = PerftSearcher.GetPerftResults(board, depth + 1);
            var multiThread = PerftSearcher.ParallelPerft(board, depth + 1);
            Assert.That(singleThread, Is.EqualTo(multiThread));
        }
    }

    [Test]
    public void KiwiPos()
    {
        int[] targetNumbers = [48, 2039, 97_862, 4_085_603];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board(Fen.KiwiPeteFen);
            Assert.That(PerftSearcher.ParallelPerft(board, depth + 1), Is.EqualTo(targetNumbers[depth]));
        }
    }


    [Test]
    public void Position3()
    {
        int[] targetNumbers = [14, 191, 2812, 43238, 674624, 11030083];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board(Fen.Pos3Fen);
            Assert.That(PerftSearcher.GetPerftResults(board, depth + 1), Is.EqualTo(targetNumbers[depth]));
        }
    }

    [Test]
    public void Position4()
    {
        int[] targetNumbers = [6, 264, 9467, 422333];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board(Fen.Pos4Fen);
            Assert.That(PerftSearcher.GetPerftResults(board, depth + 1), Is.EqualTo(targetNumbers[depth]));
        }
    }
    
    [Test]
    public void Position5()
    {
        int[] targetNumbers = [44, 1486, 62379,2103487];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board(Fen.Pos5Fen);
            Assert.That(PerftSearcher.GetPerftResults(board, depth + 1), Is.EqualTo(targetNumbers[depth]));
        }
    }
}