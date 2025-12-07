using Onyx;

namespace OnyxTests;


public class BoardTests
{
    [Test]
    public void InitFromFen()
    {
        var board = new Board(Fen.DefaultFen);
        
        Assert.That(board.TurnToMove, Is.EqualTo(Colour.White));
        Assert.That(board.bitboards.ToFen(),Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"));
        Assert.That(board.enPassantSquare.HasValue,Is.False);
    }
}