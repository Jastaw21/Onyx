using System.ComponentModel.Design;
using System.Security.Cryptography;
using Onyx;

namespace OnyxTests;

public class MoveGenTests
{
    [Test]
    public void MoveGenContainsBasicExpectedMoves()
    {
        var board = new Board(Fen.DefaultFen);

        var aPawnMoves = MoveGenerator.GetMoves(Piece.WP, new Square(8), ref board);
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a3")));
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a4")));

        var bKnightMoves = MoveGenerator.GetMoves(Piece.WN, new Square(1), ref board);
        Assert.That(bKnightMoves, Has.Member(new Move(Piece.WN, "b1a3")));
        Assert.That(bKnightMoves, Has.Member(new Move(Piece.WN, "b1c3")));
    }

    [Test]
    public void DoesntGenerateMoveToOwn()
    {
        var testBoard = new Board("8/8/8/8/8/NRBBN3/PKQBN3/8 w - - 0 1");
        List<Move> movesToTest =
        [
            new Move(Piece.WP, new Square(8), new Square(16)),
            new Move(Piece.WK, new Square(9), new Square(17)),
            new Move(Piece.WQ, new Square(10), new Square(18)),
            new Move(Piece.WB, new Square(11), new Square(18)),
            new Move(Piece.WB, new Square(12), new Square(18)),
        ];

        foreach (var move in movesToTest)
        {
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From, ref testBoard);
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }
    }

    [Test]
    public void CantPassOpponents()
    {
        var testBoard = new Board("8/8/8/8/8/qqq1q3/8/QRB5 w - - 0 1");

        List<Move> illegalMoves =
        [
            new Move(Piece.WQ, "a1a4"),
            new Move(Piece.WR, "b1b4"),
            new Move(Piece.WB, "c1f4"),
        ];

        List<Move> legalMoves =
        [
            new Move(Piece.WQ, "a1a3"),
            new Move(Piece.WR, "b1b3"),
            new Move(Piece.WB, "c1e3"),
            new Move(Piece.WB, "c1a3"),
        ];

        foreach (var move in illegalMoves)
        {
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From, ref testBoard);
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }

        foreach (var move in legalMoves)
        {
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From, ref testBoard);
            Assert.That(movesByPieceBySquare, Does.Contain(move));
        }
    }

    [Test]
    public void MoveGenIncludesCastling()
    {
        var castlingPos = new Board("rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7");
        var expectedCastlingMove = new Move(Piece.WK, "e1g1");
        Assert.That(MoveGenerator.GetMoves(Piece.WK,expectedCastlingMove.From,ref castlingPos), Does.Contain(expectedCastlingMove));
    }
}