using Onyx;

namespace OnyxTests;

public class RefereeTests
{
    [Test]
    public void PiecesCantGoToOwnSquare()
    {
        var testBoard = new Board("8/8/8/8/8/NRBBN3/PKQBN3/8 w - - 0 1");
        List<Move> movesToTest =
        [
            new Move(Piece.WP,new Square(8),new Square(16)),
            new Move(Piece.WK,new Square(9),new Square(17)),
            new Move(Piece.WQ,new Square(10),new Square(18)),
            new Move(Piece.WB,new Square(11),new Square(18)),
            new Move(Piece.WB,new Square(12),new Square(18)),
        ];

        foreach (var move in movesToTest)
        {
            Assert.That(Referee.MoveIsPseudoLegal(move,ref testBoard),Is.False);
        }
    }

    [Test]
    public void SlidersCantPassOpponents()
    {
        var testBoard = new Board("8/8/8/8/8/qqq1q3/8/QRB5 w - - 0 1");

        List<Move> illegalMoves =
        [
            new Move(Piece.WQ,"a1a4"),
            new Move(Piece.WR,"b1b4"),
            new Move(Piece.WB,"c1f4"),
        ];
        
        List<Move> legalMoves =
        [
            new Move(Piece.WQ,"a1a3"),
            new Move(Piece.WR,"b1b3"),
            new Move(Piece.WB,"c1e3"),
            new Move(Piece.WB,"c1a3"),
        ];

        foreach (var legalMove in legalMoves)
        {
            Assert.That(Referee.MoveIsPseudoLegal(legalMove,ref testBoard),Is.True);
        }
        foreach (var illegalMove in illegalMoves)
        {
            Assert.That(Referee.MoveIsPseudoLegal(illegalMove,ref testBoard),Is.False);
        }
    }

    [Test]
    public void CastlingIncludedInLegalMoves()
    {
        var board = new Board("rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7");

        var castlingMove = new Move(Piece.WK, new Square("e1"), new Square("g1"));
        Assert.That(Referee.MoveIsPseudoLegal(castlingMove,ref board));
    }
}