using Onyx;

namespace OnyxTests;

public class MoveGenTests
{
    [Test]
    public void MoveGenContainsBasicExpectedMoves()
    {
        var board = new Board(Fen.DefaultFen);
        
        var aPawnMoves = MoveGenerator.GetMoves(Piece.WP, new Square(8), ref board);
        Assert.That(aPawnMoves,Has.Member(new Move(Piece.WP,"a2a3")));
        Assert.That(aPawnMoves,Has.Member(new Move(Piece.WP,"a2a4")));
        
        var bKnightMoves = MoveGenerator.GetMoves(Piece.WN, new Square(1), ref board);
        Assert.That(bKnightMoves,Has.Member(new Move(Piece.WN,"b1a3")));
        Assert.That(bKnightMoves,Has.Member(new Move(Piece.WN,"b1c3")));
    }
    
}