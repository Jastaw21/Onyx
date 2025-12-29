using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class MoveGenTests
{
    [Test]
    public void MoveGenContainsBasicExpectedMoves()
    {
        var board = new Board();

        var aPawnMoves = MoveGenerator.GetMoves(Piece.WP, 8, board);
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a3")));
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a4")));

        var bKnightMoves = MoveGenerator.GetMoves(Piece.WN, 1, board);
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
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From.SquareIndex, testBoard);
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
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From.SquareIndex, testBoard);
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }

        foreach (var move in legalMoves)
        {
            var movesByPieceBySquare = MoveGenerator.GetMoves(move.PieceMoved, move.From.SquareIndex, testBoard);
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
            Assert.That(MoveGenerator.GetMoves(move.PieceMoved, move.From.SquareIndex, pos), Does.Contain(move));
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
            Assert.That(MoveGenerator.GetMoves(move.PieceMoved, move.From.SquareIndex, pos), Does.Not.Contain(move));
        }
    }

    [Test]
    public void MoveGenIncludesPromotionMoves()
    {
        var board = new Board("8/P7/8/8/8/8/8/8 w - - 0 1");
        var moves = MoveGenerator.GetMoves(Piece.WP, new Square("a7").SquareIndex, board: board);
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
        var knightMoves = MoveGenerator.GetMoves(Piece.WN, board);
        Assert.That(knightMoves, Has.Count.EqualTo(4));
        foreach (var expectedMove in expectedKnightMoves)
        {
            Assert.That(knightMoves, Does.Contain(expectedMove));
        }

        var pawnMoves = MoveGenerator.GetMoves(Piece.BP, board);
        Assert.That(pawnMoves,Has.Count.EqualTo(16));
    }

    [Test]
    public void GetAllMovesByColour()
    {
        var board = new Board();
        var whiteMoves = MoveGenerator.GetMoves(Colour.White, board);
        var blackMoves = MoveGenerator.GetMoves(Colour.Black, board);
        Assert.Multiple(() =>
        {
            Assert.That(whiteMoves, Has.Count.EqualTo(20));
            Assert.That(blackMoves, Has.Count.EqualTo(20));
        });
    }

    [Test]
    public void MoveGenEnPassantHasToBeNextTo()
    {
        var board = new Board("rnbqkbnr/pppppppp/8/8/P7/8/1PPPPPPP/RNBQKBNR b KQkq a3 0 1");
        var moves = MoveGenerator.GetMoves(Piece.BP, board);
        Assert.That(moves, Has.Count.EqualTo(16));
    }

    [Test]
    public void MoveGenPawnsCantAttackStraight()
    {
        var testBoard = new Board("rnbqkbnr/1ppppppp/8/p7/P7/8/1PPPPPPP/RNBQKBNR w KQkq a6 0 1");
        var moves = MoveGenerator.GetMoves(Piece.WP, testBoard);
        Assert.That(MoveGenerator.GetMoves(Piece.WP,testBoard), Does.Not.Contain(new Move(Piece.WP,"a4a5")));
    }

    [Test]
    public void CanCastleIgB8Attacked()
    {
        var board = new Board("r3k2r/p1pNqpb1/bn2pnp1/3P4/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1");
        Assert.That(MoveGenerator.GetMoves(Piece.BK,board),Does.Contain(new Move(Piece.BK,"e8c8")));
    }

    [Test]
    public void CantCastleIfKingAttcked()
    {
        var board = new Board("r3k2r/p1p1qpb1/bn1ppnp1/1B1PN3/1p2P3/P1N2Q1p/1PPB1PPP/R3K2R b KQkq - 0 1");
        Assert.That(MoveGenerator.GetMoves(Piece.BK,board),Does.Not.Contain(new Move(Piece.BK,"e8c8")));
    }

    [Test]
    public void CanCaptureAndPromoteTogether()
    {
        var board = new Board("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/PPN2Q2/2PBBPpP/R3K2R b KQkq - 0 1");
        var moves = MoveGenerator.GetMoves(Piece.BP, new Square("g2").SquareIndex, board);
        Assert.That(moves,Does.Contain(new Move(Piece.BP,"g2h1q")));
    }

    [Test]
    public void MissingPawnPushFixed()
    {
        var board = new Board("r3k2r/p1ppqpb1/bn2pnN1/3P4/1p2P3/P1N2Q2/1PPBBPpP/R3K2R b KQkq - 0 1");
        Assert.That(MoveGenerator.GetMoves(Piece.BP,new Square(44).SquareIndex,board),Does.Contain(new Move(Piece.BP,"e6e5")));
    }

    [Test]
    public void MovGenDoesntAffectBoard()
    {
        var fen = "rnbqkbnr/p1pppppp/8/3N4/1p6/1P6/P1PPPPPP/R1BQKBNR b KQkq - 1 3";

        var board = new Board(fen);
        Span<Move> moveBuffer = stackalloc Move[256];
        MoveGenerator.GetLegalMoves(board);
        Assert.That(board.GetFen(), Is.EqualTo(fen));
        MoveGenerator.GetLegalMoves(board);
        Assert.That(board.GetFen(), Is.EqualTo(fen));
    }

    [Test]
    public void MoveGenDoesntMovePinnedPieces()
    {
        var fen = "6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1";
        var moves = MoveGenerator.GetLegalMoves(new Board(fen));
        Assert.That(moves,Does.Not.Contain(new Move(Piece.WP,"g2g3")));
    }

    [Test]
    public void PinnedPieceCanMoveStayingPinned()
    {
        var fen = "rnQq1k1r/pp2bppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R b KQ - 0 1";
        var moves = MoveGenerator.GetLegalMoves(new Board(fen));
        Assert.That(moves,Does.Contain(new Move(Piece.BQ,"d8e8")));
    }
}