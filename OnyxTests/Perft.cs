using Onyx.Core;

namespace OnyxTests;

public class Perft
{
    [Test]
    public void StartingPosDepth()
    {
        int[] targetNumbers = [20, 400, 8902];
        for (var depth = 0; depth < targetNumbers.Length; depth++)
        {
            var board = new Board();
            Assert.That(PerftSearcher.GetPerftResults(board,depth+1),Is.EqualTo(targetNumbers[depth]));
        }

    }
  
}