using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class MoveGenTests
{
    [Test]
    public void MoveGenContainsBasicExpectedMoves()
    {
        var board = new Position();

        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.WP, 8, board, moveBuffer, ref count);
        var aPawnMoves = moveBuffer[..count].ToArray();
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a3")));
        Assert.That(aPawnMoves, Has.Member(new Move(Piece.WP, "a2a4")));

        count = 0;
        MoveGenerator.GetMoves(Piece.WN, 1, board, moveBuffer, ref count);
        var bKnightMoves = moveBuffer[..count].ToArray();
        Assert.That(bKnightMoves, Has.Member(new Move(Piece.WN, "b1a3")));
        Assert.That(bKnightMoves, Has.Member(new Move(Piece.WN, "b1c3")));
    }

    [Test]
    public void DoesntGenerateKingCaptures()
    {
        var fen = "6k1/8/1p3P2/2q4K/2P5/3R4/8/5r2 b - - 0 1";
        var board = new Position(fen);
        Span<Move> moveBuffer = stackalloc Move[256];
        
        var count = MoveGenerator.GetLegalMoves(board, moveBuffer);
        var moves = moveBuffer[..count].ToArray();
        List<string > movesNotation = [];
        foreach (var move in moves)
        {
            movesNotation.Add(move.Notation);
        }
        Assert.That(movesNotation, Does.Not.Contain("c5h5"));
    }
    
    [Test]
    public void DoesntGenerateStrangeIllegalMoves()
    {
        var fen = "rnbq1rk1/pp2n1pp/4p3/2ppPp2/3P2Q1/P1PB4/2P2PPP/R1B1K1NR w KQ f6 0 1";
        var board = new Position(fen);
        Span<Move> moveBuffer = stackalloc Move[256];
        
        var count = MoveGenerator.GetMoves(board, moveBuffer);
        Assert.That(count, Is.EqualTo(44));
            
        
    }

    [Test]
    public void B1C3NotGen()
    {
        var board = new Position("rnbqkb1r/ppn2ppp/4p3/2ppP3/2BP4/2P2N2/PP3PPP/RNBQK2R w KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        var count = MoveGenerator.GetMoves(board, moveBuffer);
        var moves = moveBuffer[..count];
        List<string> moveNotation = [];
        foreach (var move in moves)
        {
            moveNotation.Add(move.Notation);
        }
        
        Assert.That(moveNotation,Does.Not.Contain("b1c3"));

    }

    [Test]
    public void DoesntGenerateMoveToOwn()
    {
        var testBoard = new Position("8/8/8/8/8/NRBBN3/PKQBN3/8 w - - 0 1");
        List<Move> movesToTest =
        [
            new(Piece.WP, 8, 16),
            new(Piece.WK, 9, 17),
            new(Piece.WQ, 10, 18),
            new(Piece.WB, 11, 18),
            new(Piece.WB, 12, 18),
        ];

        Span<Move> moveBuffer = stackalloc Move[256];
        foreach (var move in movesToTest)
        {
            int count = 0;
            MoveGenerator.GetMoves(move.PieceMoved, move.From, testBoard, moveBuffer, ref count);
            var movesByPieceBySquare = moveBuffer[..count].ToArray();
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }
    }

    [Test]
    public void CantPassOpponents()
    {
        var testBoard = new Position("8/8/8/8/8/qqq1q3/8/QRB5 w - - 0 1");

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

        Span<Move> moveBuffer = stackalloc Move[256];
        foreach (var move in illegalMoves)
        {
            int count = 0;
            MoveGenerator.GetMoves(move.PieceMoved, move.From, testBoard, moveBuffer, ref count);
            var movesByPieceBySquare = moveBuffer[..count].ToArray();
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }

        foreach (var move in legalMoves)
        {
            var localMove = move;
            localMove.CapturedPiece = Piece.BQ;
            int count = 0;
            MoveGenerator.GetMoves(localMove.PieceMoved, localMove.From, testBoard, moveBuffer, ref count);
            var movesByPieceBySquare = moveBuffer[..count].ToArray();
            Assert.That(movesByPieceBySquare, Does.Contain(localMove));
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
            new(Piece.WK, "e1g1"),
            new(Piece.WK, "e1c1"),
            new(Piece.BK, "e8g8"),
            new(Piece.BK, "e8c8")
        ];


        Span<Move> moveBuffer = stackalloc Move[256];
        for (var scen = 0; scen < startingPositions.Count; scen++)
        {
            var pos = new Position(startingPositions[scen]);
            var move = moves[scen];
            int count = 0;
            MoveGenerator.GetMoves(move.PieceMoved, move.From, pos, moveBuffer, ref count);
            var movesByPieceBySquare = moveBuffer[..count].ToArray();
            Assert.That(movesByPieceBySquare, Does.Contain(move));
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
            new(Piece.WK, "e1g1"),
            new(Piece.WK, "e1c1"),
            new(Piece.BK, "e8g8"),
            new(Piece.BK, "e8c8")
        ];


        Span<Move> moveBuffer = stackalloc Move[256];
        for (var scen = 0; scen < startingPositions.Count; scen++)
        {
            var pos = new Position(startingPositions[scen]);
            var move = moves[scen];
            int count = 0;
            MoveGenerator.GetMoves(move.PieceMoved, move.From, pos, moveBuffer, ref count);
            var movesByPieceBySquare = moveBuffer[..count].ToArray();
            Assert.That(movesByPieceBySquare, Does.Not.Contain(move));
        }
    }

    [Test]
    public void MoveGenIncludesPromotionMoves()
    {
        var board = new Position("8/P7/8/8/8/8/8/8 w - - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.WP, RankAndFile.SquareIndex("a7"), board, moveBuffer, ref count);
        Assert.That(count, Is.EqualTo(4));
    }

    [Test]
    public void GetAllMovesForPieceWorksAsExpected()
    {
        var board = new Position();
        List<Move> expectedKnightMoves =
        [
            new(Piece.WN, "b1a3"),
            new(Piece.WN, "b1c3"),
            new(Piece.WN, "g1h3"),
            new(Piece.WN, "g1f3"),
        ];
        
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.WN, board, moveBuffer, ref count);
        var knightMoves = moveBuffer[..count].ToArray();
        Assert.That(knightMoves, Has.Length.EqualTo(4));
        foreach (var expectedMove in expectedKnightMoves)
        {
            Assert.That(knightMoves, Does.Contain(expectedMove));
        }

        count = 0;
        MoveGenerator.GetMoves(Piece.BP, board, moveBuffer, ref count);
        var pawnMoves = moveBuffer[..count].ToArray();
        Assert.That(pawnMoves, Has.Length.EqualTo(16));
    }

    [Test]
    public void GetAllMovesByColour()
    {
        var board = new Position();
        Span<Move> moveBuffer = stackalloc Move[256];
        int whiteCount = MoveGenerator.GetMoves(true, board, moveBuffer);
        var whiteMoves = moveBuffer[..whiteCount].ToArray();
        
        int blackCount = MoveGenerator.GetMoves(false, board, moveBuffer);
        var blackMoves = moveBuffer[..blackCount].ToArray();
        
        Assert.Multiple(() =>
        {
            Assert.That(whiteMoves, Has.Length.EqualTo(20));
            Assert.That(blackMoves, Has.Length.EqualTo(20));
        });
    }

    [Test]
    public void MoveGenEnPassantHasToBeNextTo()
    {
        var board = new Position("rnbqkbnr/pppppppp/8/8/P7/8/1PPPPPPP/RNBQKBNR b KQkq a3 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BP, board, moveBuffer, ref count);
        Assert.That(count, Is.EqualTo(16));
    }

    [Test]
    public void MoveGenPawnsCantAttackStraight()
    {
        var testBoard = new Position("rnbqkbnr/1ppppppp/8/p7/P7/8/1PPPPPPP/RNBQKBNR w KQkq a6 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.WP, testBoard, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        Assert.That(moves, Does.Not.Contain(new Move(Piece.WP,"a4a5")));
    }

    [Test]
    public void CanCastleIgB8Attacked()
    {
        var board = new Position("r3k2r/p1pNqpb1/bn2pnp1/3P4/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BK, board, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        Assert.That(moves, Does.Contain(new Move(Piece.BK,"e8c8")));
    }

    [Test]
    public void CantCastleIfKingAttcked()
    {
        var board = new Position("r3k2r/p1p1qpb1/bn1ppnp1/1B1PN3/1p2P3/P1N2Q1p/1PPB1PPP/R3K2R b KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BK, board, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        Assert.That(moves, Does.Not.Contain(new Move(Piece.BK,"e8c8")));
    }

    [Test]
    public void CanCaptureAndPromoteTogether()
    {
        var board = new Position("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/PPN2Q2/2PBBPpP/R3K2R b KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BP, RankAndFile.SquareIndex("g2"), board, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        var expectedMove = new Move(Piece.BP,"g2h1q");
        expectedMove.CapturedPiece = Piece.WR;
        Assert.That(moves, Does.Contain(expectedMove));
    }

    [Test]
    public void MissingPawnPushFixed()
    {
        var board = new Position("r3k2r/p1ppqpb1/bn2pnN1/3P4/1p2P3/P1N2Q2/1PPBBPpP/R3K2R b KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BP, 44, board, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        Assert.That(moves, Does.Contain(new Move(Piece.BP,"e6e5")));
    }

    [Test]
    public void MovGenDoesntAffectBoard()
    {
        var fen = "rnbqkbnr/p1pppppp/8/3N4/1p6/1P6/P1PPPPPP/R1BQKBNR b KQkq - 1 3";

        var board = new Position(fen);
        Span<Move> moveBuffer = stackalloc Move[256];
        MoveGenerator.GetLegalMoves(board, moveBuffer);
        Assert.That(board.GetFen(), Is.EqualTo(fen));
        MoveGenerator.GetLegalMoves(board, moveBuffer);
        Assert.That(board.GetFen(), Is.EqualTo(fen));
    }

    [Test]
    public void MoveGenDoesntMovePinnedPieces()
    {
        var fen = "6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1";
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = MoveGenerator.GetLegalMoves(new Position(fen), moveBuffer);
        var moves = moveBuffer[..count].ToArray();
        Assert.That(moves,Does.Not.Contain(new Move(Piece.WP,"g2g3")));
    }

    [Test]
    public void FoolsMateMoveGen()
    {
        var fen = "rnbqkbnr/pppp1ppp/4p3/8/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 1";
        var board = new Position(fen);
        Span<Move> moveBuffer = stackalloc Move[256];
        int count = 0;
        MoveGenerator.GetMoves(Piece.BQ, board, moveBuffer, ref count);
        var moves = moveBuffer[..count].ToArray();
        var move = new Move(Piece.BQ, "d8h4");
        Assert.That(moves, Does.Contain(move), "d8h4 should be generated");
        Assert.That(Referee.MoveIsLegal(move, board), Is.True, "d8h4 should be legal");
    }

    [Test]
    public void MoveGenAddsCaptures()
    {
        var board = new Position("rnbqkbnr/ppppppp1/8/7p/8/4P3/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
        Span<Move> moveBuffer = stackalloc Move[256];
        var count = 0;
        var moves = MoveGenerator.GetMoves(Piece.WQ, board, moveBuffer, ref count);
        var movesIndexed = moveBuffer[..moves].ToArray();
        foreach (var move in movesIndexed)
        {
            if (move.Notation == "d1h5")
            {
                Assert.That(move.CapturedPiece, Is.EqualTo(Piece.PieceType(Piece.BP)));
            }
        }
    }
    
    [Test]
    public void EnPassantMovesHaveCapturedPieceAssigned()
    {
        List<string> startingPositions =
        [
            "rnbqkbnr/pp1p1ppp/2p5/3Pp3/8/8/PPP1PPPP/RNBQKBNR w KQkq e6 0 1",
            "rnbqkbnr/ppp1pppp/8/8/3pP3/1PP5/P2P1PPP/RNBQKBNR b KQkq e3 0 1"
        ];

        List<Move> expectedMoves =
        [
            new(Piece.WP, "d5e6"),
            new(Piece.BP, "d4e3")
        ];

        List<string> endingPositions =
        [
            "rnbqkbnr/pp1p1ppp/2p1P3/8/8/8/PPP1PPPP/RNBQKBNR b KQkq - 0 1",
            "rnbqkbnr/ppp1pppp/8/8/8/1PP1p3/P2P1PPP/RNBQKBNR w KQkq - 0 2"
        ];

       var board = new Position(startingPositions[0]);
       Span<Move> moveBuffer = stackalloc Move[256];
       var count = 0;
       var moves = MoveGenerator.GetMoves(Piece.WQ, board, moveBuffer, ref count);
       var movesIndexed = moveBuffer[..moves].ToArray();

       foreach (var move in movesIndexed)
       {
           if (move.Notation == expectedMoves[0].Notation)
           {
               Assert.That(move.CapturedPiece, Is.EqualTo(Piece.PieceType(Piece.WP)));
           }
       }
    }
}