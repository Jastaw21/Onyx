using System.ComponentModel.Design;
using System.Security.Cryptography;
using Onyx;
using Onyx.Core;

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
            new(Piece.WP, new Square(8), new Square(16)),
            new(Piece.WK, new Square(9), new Square(17)),
            new(Piece.WQ, new Square(10), new Square(18)),
            new(Piece.WB, new Square(11), new Square(18)),
            new(Piece.WB, new Square(12), new Square(18)),
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
            new(Piece.WQ, "a1a4"),
            new(Piece.WR, "b1b4"),
            new(Piece.WB, "c1f4"),
        ];

        List<Move> legalMoves =
        [
            new(Piece.WQ, "a1a3"),
            new(Piece.WR, "b1b3"),
            new(Piece.WB, "c1e3"),
            new(Piece.WB, "c1a3"),
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
        List<string> startingPositions =
        [
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/R3KBNR w KQkq - 2 6",
            "rn1qk2r/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR b kq - 5 7",
            "r3k2r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR b kq - 2 10"
        ];

        List<Move> moves =
        [
            new(Piece.MakePiece(PieceType.King, Colour.White), "e1g1"),
            new(Piece.MakePiece(PieceType.King, Colour.White), "e1c1"),
            new(Piece.MakePiece(PieceType.King, Colour.Black), "e8g8"),
            new(Piece.MakePiece(PieceType.King, Colour.Black), "e8c8")
        ];


        for (var scen = 0; scen < startingPositions.Count; scen++)
        {
            var pos = new Board(startingPositions[scen]);
            var move = moves[scen];
            Assert.That(MoveGenerator.GetMoves(move.PieceMoved, move.From, ref pos), Does.Contain(move));
        }
    }

    [Test]
    public void MoveGenExcludesCastlingWhenAttacked()
    {
        List<string> startingPositions =
        [
            "rnbqk2r/1pp2ppp/p2p1n2/4p3/2P5/2N1bNP1/PP1PP1BP/1RBQK2R b Kq - 0 1",
            "rn1qkb1r/pppb1pp1/3pp2p/6B1/Q2P4/2Pn4/PP1NPPPP/R3KBNR w KQkq - 0 1",
            "rn1qk2r/pppbbpp1/3ppnNp/6B1/1Q1P4/2P5/PP2PPPP/2KR1BNR b kq - 0 1",
            "r3k2r/ppQbbpp1/n1pppn1p/3P4/7B/2P5/PP1NPPPP/2KR1BNR b kq - 0 1"
        ];

        List<Move> moves =
        [
            new(Piece.MakePiece(PieceType.King, Colour.White), "e1g1"),
            new(Piece.MakePiece(PieceType.King, Colour.White), "e1c1"),
            new(Piece.MakePiece(PieceType.King, Colour.Black), "e8g8"),
            new(Piece.MakePiece(PieceType.King, Colour.Black), "e8c8")
        ];


        for (var scen = 0; scen < startingPositions.Count; scen++)
        {
            var pos = new Board(startingPositions[scen]);
            var move = moves[scen];
            Assert.That(MoveGenerator.GetMoves(move.PieceMoved, move.From, ref pos), Does.Not.Contain(move));
        }
    }

    [Test]
    public void MoveGenIncludesPromotionMoves()
    {
        var board = new Board("8/P7/8/8/8/8/8/8 w - - 0 1");
        var moves = MoveGenerator.GetMoves(Piece.WP, new Square("a7"), board: ref board);
        Assert.That(moves.Count, Is.EqualTo(4));
    }

    [Test]
    public void GetAllMovesForPieceWorksAsExpected()
    {
        var board = new Board();
        List<Move> expectedKnightMoves =
        [
            new Move(Piece.WN, "b1a3"),
            new Move(Piece.WN, "b1c3"),
            new Move(Piece.WN, "g1h3"),
            new Move(Piece.WN, "g1f3"),
        ];
        var knightMoves = MoveGenerator.GetMoves(Piece.WN, ref board);
        Assert.That(knightMoves, Has.Count.EqualTo(4));
        foreach (var expectedMove in expectedKnightMoves)
        {
            Assert.That(knightMoves, Does.Contain(expectedMove));
        }

        var pawnMoves = MoveGenerator.GetMoves(Piece.BP, ref board);
        Assert.That(pawnMoves,Has.Count.EqualTo(16));
    }

    [Test]
    public void GetAllMovesByColour()
    {
        var board = new Board();
        var whiteMoves = MoveGenerator.GetMoves(Colour.White, ref board);
        var blackMoves = MoveGenerator.GetMoves(Colour.Black, ref board);
        Assert.Multiple(() =>
        {
            Assert.That(whiteMoves, Has.Count.EqualTo(20));
            Assert.That(blackMoves, Has.Count.EqualTo(20));
        });
    }
}