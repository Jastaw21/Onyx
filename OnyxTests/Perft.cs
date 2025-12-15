using Onyx.Core;

namespace OnyxTests;

public class Perft
{
    [Test]
    public void StartingPos()
    {
        int[] targetNumbers = [20, 400, 8902,197_281];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board();
            Assert.That(PerftSearcher.GetPerftResults(board,depth+1),Is.EqualTo(targetNumbers[depth]));
        }
    }
    
    [Test]
    public void KiwiPos(){
        int[] targetNumbers = [48, 2039, 97_862,4_085_603];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board(Fen.KiwiPeteFen);
            Assert.That(PerftSearcher.GetPerftResults(board,depth+1),Is.EqualTo(targetNumbers[depth]));
        }
    }
  
}