using Onyx.Core;

namespace OnyxTests;

public class Perft
{
    [Test]
    public void StartingPosDepth1()
    {
        var board = new Board();
        Assert.That(PerftSearcher.GetPerftResults(board,1).Nodes,Is.EqualTo(20));
    }
    [Test]
    public void StartingPosDepth2()
    {
        var board = new Board();
        Assert.That(PerftSearcher.GetPerftResults(board,2).Nodes,Is.EqualTo(40));
    }
}