using Onyx.Core;

namespace OnyxTests;

public class ApplyMove
{
    [Test]
    public void HalfMoveIncrements()
    {
        var board = new Board();
        board.ApplyMove(new Move(Piece.WP,"a2a4"));
        Assert.Multiple(() =>
        {
            Assert.That(board.FullMoves, Is.EqualTo(1));
            Assert.That(board.HalfMoves, Is.EqualTo(0));
        });

        board.ApplyMove(new Move(Piece.BN,"b8c6"));
        Assert.Multiple(() =>
        {
            Assert.That(board.FullMoves, Is.EqualTo(2));
            Assert.That(board.HalfMoves, Is.EqualTo(1));
        });

        board.ApplyMove(new Move(Piece.WP,"h2h4"));
        Assert.Multiple(() =>
        {
            Assert.That(board.FullMoves, Is.EqualTo(2));
            Assert.That(board.HalfMoves, Is.EqualTo(0));
        });
    }
    
    [Test]
    public void ApplyPawnPush()
    {
        var board = new Board();

        var move = new Move(
            Piece.WP,
            RankAndFile.SquareIndex("a2"),
            RankAndFile.SquareIndex("a4")
        );

        board.ApplyMove(move);
        Assert.That(board.Bitboards.GetFen(), Is.EqualTo(
            "rnbqkbnr/pppppppp/8/8/P7/8/1PPPPPPP/RNBQKBNR"
        ));

        board.ApplyMove(new Move(Piece.BP, "a7a5"));
        Assert.That(board.Bitboards.GetFen(), Is.EqualTo(
            "rnbqkbnr/1ppppppp/8/p7/P7/8/1PPPPPPP/RNBQKBNR"
        ));
    }

    [Test]
    public void CaptureUpdatesBoardState()
    {
        var board = new Board("rnb1kbnr/ppp1pppp/3p4/8/5q2/3P4/PPPQPPPP/RNB1KBNR w KQkq - 0 1");

        var queenCapture = new Move(Piece.WQ, "d2f4");
        board.ApplyMove(queenCapture);

        Assert.That(board.GetFen(), Is.EqualTo("rnb1kbnr/ppp1pppp/3p4/8/5Q2/3P4/PPP1PPPP/RNB1KBNR b KQkq - 0 1"));
    }

    [Test]
    public void CastlingUpdatesBoardState()
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

        List<string> endingPositions =
        [
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQ1RK1 b q - 1 7",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/2KR1BNR b kq - 3 6",
            "rn1q1rk1/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR w - - 6 8",
            "2kr3r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 3 11"
        ];

        for (var test = 0; test < startingPositions.Count; test++)
        {
            var board = new Board(startingPositions[test]);
            board.ApplyMove(moves[test]);
            Assert.That(board.GetFen(), Is.EqualTo(endingPositions[test]));
        }
    }

    [Test]
    public void PromotionUpdatesBoardState()
    {
        // white can promote from here
        var board = new Board("3rb2r/pPqkbpp1/n2ppn1p/8/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 1 13");
        var promotionMove = new Move(Piece.WP, "b7b8q");

        board.ApplyMove(promotionMove);
        Assert.That(board.GetFen(), Is.EqualTo("1Q1rb2r/p1qkbpp1/n2ppn1p/8/2Q4B/2P5/PP1NPPPP/2KR1BNR b - - 0 13"));
    }

    [Test]
    public void MovingKingLosesCastlingRights()
    {
        List<string> startingPositions =
        [
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPP1PP/RNBQKBNR w KQkq - 0 2",
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2"
        ];

        List<Move> moves =
        [
            new(Piece.WK, "e1f2"),
            new(Piece.BK, "e8e7")
        ];

        List<string> PositionsAfter =
        [
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2",
            "rnbq1bnr/ppppkppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR w - - 2 3"
        ];

        VerifyMoves(startingPositions, moves, PositionsAfter);
    }

    private static void VerifyMoves(List<string> startingPositions, List<Move> moves, List<string> PositionsAfter)
    {
        for (var test = 0; test < startingPositions.Count; test++)
        {
            var board = new Board(startingPositions[test]);
            board.ApplyMove(moves[test]);
            Assert.That(board.GetFen(), Is.EqualTo(PositionsAfter[test]));
        }
    }

    [Test]
    public void MovingRookLosesCastlingRights()
    {
        var board = new Board("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/R1BQKBNR w KQkq - 2 3");
        var rookMove = new Move(Piece.WR, "a1b1");

        board.ApplyMove(rookMove);
        Assert.Multiple(() =>
        {
            Assert.That(board.GetFen(),
                Is.EqualTo("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/1RBQKBNR b Kkq - 3 3"));
            Assert.That(board.CastlingRights,
                Is.EqualTo(BoardConstants.WhiteKingsideCastlingFlag | BoardConstants.BlackKingsideCastlingFlag |
                           BoardConstants.BlackQueensideCastlingFlag));
        });

        var blackRookMove = new Move(Piece.BR, "h8g8");
        board.ApplyMove(blackRookMove);
        Assert.Multiple(() =>
        {
            Assert.That(board.GetFen(),
                Is.EqualTo("rnbqkbr1/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/1RBQKBNR w Kq - 4 4"));
            Assert.That(board.CastlingRights,
                Is.EqualTo(BoardConstants.WhiteKingsideCastlingFlag | BoardConstants.BlackQueensideCastlingFlag));
        });
    }

    [Test]
    public void MovingRookBackDoesntResetCastling()
    {
        // black has already lost castling rights but the rook is back in it's starting square
        var board = new Board("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N3P1/PP1PPPBP/1RBQK1NR b Kq - 2 5");

        // check that black can't castle queen side
        Assert.That(board.CastlingRights,
            Is.EqualTo(BoardConstants.WhiteKingsideCastlingFlag | BoardConstants.BlackQueensideCastlingFlag));

        // move the rook out to call the castlingrigths function
        var blackRookMove = new Move(Piece.BR, "h8g8");
        board.ApplyMove(blackRookMove);

        // check nothing has changed
        Assert.That(board.CastlingRights,
            Is.EqualTo(BoardConstants.WhiteKingsideCastlingFlag | BoardConstants.BlackQueensideCastlingFlag));
    }

    [Test]
    public void BasicEnPassantSquareSetting()
    {
        var board = new Board();
        board.ApplyMove(new Move(Piece.WP, "d2d4"));
        Assert.That(board.GetFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq d3 0 1"));

        board.ApplyMove(new Move(Piece.BP, "d7d5"));
        Assert.That(board.GetFen(), Is.EqualTo("rnbqkbnr/ppp1pppp/8/3p4/3P4/8/PPP1PPPP/RNBQKBNR w KQkq d6 0 2"));
    }

    [Test]
    public void EnPassantAppliesBoardState()
    {
        List<string> startingPositions =
        [
            "rnbqkbnr/pp1p1ppp/2p5/3Pp3/8/8/PPP1PPPP/RNBQKBNR w KQkq e6 0 1",
            "rnbqkbnr/ppp1pppp/8/8/3pP3/1PP5/P2P1PPP/RNBQKBNR b KQkq e3 0 1"
        ];

        List<Move> moves =
        [
            new(Piece.WP, "d5e6"),
            new(Piece.BP, "d4e3")
        ];

        List<string> endingPositions =
        [
            "rnbqkbnr/pp1p1ppp/2p1P3/8/8/8/PPP1PPPP/RNBQKBNR b KQkq - 0 1",
            "rnbqkbnr/ppp1pppp/8/8/8/1PP1p3/P2P1PPP/RNBQKBNR w KQkq - 0 2"
        ];

        VerifyMoves(startingPositions, moves, endingPositions);
    }
    
    
}

public class UndoMove
{
    [Test]
    public void UndoPush()
    {
        var board = new Board();
        var fenPrior = board.GetFen();
        var push = new Move(Piece.WP, "b2b4");
        board.ApplyMove(push);

        board.UndoMove(push);
        Assert.That(board.GetFen(), Is.EqualTo(fenPrior));

        var knight = new Move(Piece.WN, "b1c3");
        board.ApplyMove(knight);
        board.UndoMove(knight);
        Assert.That(board.GetFen(), Is.EqualTo(fenPrior));
    }

    [Test]
    public void UndoCapture()
    {
        var board = new Board("rnb1kbnr/pp1ppppp/2p5/2q5/8/2N1PP2/PPPP2PP/R1BQKBNR b KQkq - 0 1");
        var fenBefore = board.GetFen();
        var capture = new Move(Piece.BQ, "c5c3");
        board.ApplyMove(capture);

        Assert.That(board.GetFen(), Is.Not.EqualTo(fenBefore));

        board.UndoMove(capture);
        Assert.That(board.GetFen(), Is.EqualTo(fenBefore));
    }

    [Test]
    public void UndoPromotion()
    {
        var board = new Board("3rb2r/pPqkbpp1/n2ppn1p/8/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 1 13");
        var fenBefore = board.GetFen();
        var promotionMove = new Move(Piece.WP, "b7b8q");

        board.ApplyMove(promotionMove);

        Assert.That(board.GetFen(), Is.Not.EqualTo(fenBefore));

        board.UndoMove(promotionMove);

        Assert.That(board.GetFen(), Is.EqualTo(fenBefore));

        board = new Board("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/PPN2Q2/2PBBPpP/R3K2R b KQkq - 0 2");
        var move = new Move(Piece.BP, "g2h1b");
        board.ApplyMove(move);
        board.UndoMove(move);
        Assert.That(board.GetFen(), Is.EqualTo("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/PPN2Q2/2PBBPpP/R3K2R b KQkq - 0 2"));

        board = new Board("1r2k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1R1K w k - 2 2");
        move = new Move(Piece.WP, "a7b8B");
        board.ApplyMove(move);
        board.UndoMove(move);
        Assert.That(board.GetFen(), Is.EqualTo("1r2k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1R1K w k - 2 2"));


    }

    [Test]
    public void UndoCastling()
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

        List<string> endingPositions =
        [
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQ1RK1 b q - 0 1",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/2KR1BNR b kq - 0 1",
            "rn1q1rk1/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR w - - 0 1",
            "2kr3r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 0 1"
        ];

        VerifyUndoingMoves(startingPositions, moves);
    }

    private static void VerifyUndoingMoves(List<string> startingPositions, List<Move> moves)
    {
        for (var test = 0; test < startingPositions.Count; test++)
        {
            var board = new Board(startingPositions[test]);
            var fenBefore = board.GetFen();
            board.ApplyMove(moves[test]);
            board.UndoMove(moves[test]);

            Assert.That(board.GetFen(), Is.EqualTo(fenBefore));
        }
    }

    [Test]
    public void UndoLossOfCastlingRights()
    {
        var board = new Board("rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPP1PP/RNBQKBNR w KQkq - 0 2");
        var rightsBefore = board.CastlingRights;
        var kingMove = new Move(Piece.WK, "e1f2");
        board.ApplyMove(kingMove);
        board.UndoMove(kingMove);
        Assert.That(board.CastlingRights, Is.EqualTo(rightsBefore));
    }

    [Test]
    public void UndoEnPassant()
    {
        List<string> startingPositions =
        [
            "rnbqkbnr/pp1p1ppp/2p5/3Pp3/8/8/PPP1PPPP/RNBQKBNR w KQkq e6 0 1",
            "rnbqkbnr/ppp1pppp/8/8/3pP3/1PP5/P2P1PPP/RNBQKBNR b KQkq e3 0 1"
        ];

        List<Move> moves =
        [
            new(Piece.WP, "d5e6"),
            new(Piece.BP, "d4e3")
        ];

        VerifyUndoingMoves(startingPositions, moves);
    }

    [Test]
    public void UndoLossOfEnPassantSquare()
    {
        var board = new Board();
        var previousEP = board.EnPassantSquare;
        var move = new Move(Piece.WP, "d2d4");
        board.ApplyMove(move);
        board.UndoMove(move);

        Assert.That(previousEP, Is.EqualTo(board.EnPassantSquare));
    }

    [Test]
    public void UndoQueenCapture()
    {
        var board = new Board("rnbqkbnr/p1pppppp/8/1p6/Q7/2P5/PP1PPPPP/RNB1KBNR b KQkq - 0 1");
        var captureMove = new Move(Piece.BP, "b5a4");
        var fenBefore = board.GetFen();
        board.ApplyMove(captureMove);
        var fenDuring = board.GetFen();
        
        board.UndoMove(captureMove);
        var fenAfter = board.GetFen();
        Assert.That(fenBefore,Is.EqualTo(fenAfter));
    }
}

public class BoardTests
{
    [Test]
    public void InitFromFen()
    {
        var board = new Board();

        Assert.Multiple(() =>
        {
            Assert.That(board.WhiteToMove, Is.True);
            Assert.That(board.Bitboards.GetFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"));
            Assert.That(board.EnPassantSquare.HasValue, Is.False);
        });
    }


    [Test]
    public void GetFenReflectsCastlingRules()
    {
        var board = new Board("r1bqkbr1/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R w Kq - 6 4");

        Assert.That(board.GetFen(), Is.EqualTo("r1bqkbr1/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R w Kq - 6 4"));
    }

    [Test]
    public void SetFenGivesSameResultAsInit()
    {
        
        var fen = "r1bqkbr1/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R w Kq - 6 4";
        var boardFromFen = new Board(fen);
        var board = new Board();
        board.SetFen(fen);

        Assert.Multiple(() =>
        {
            // fens equal
            Assert.That(board.GetFen(), Is.EqualTo(boardFromFen.GetFen()));

            //  move history equal
            Assert.That(board.History.Length, Is.EqualTo(boardFromFen.History.Length));

            // boards equal
            Assert.That(board.Bitboards.AllPieces, Is.EqualTo(boardFromFen.Bitboards.AllPieces));
            Assert.That(board.Bitboards._whitePieces, Is.EqualTo(boardFromFen.Bitboards._whitePieces));
            Assert.That(board.Bitboards.Boards, Is.EqualTo(boardFromFen.Bitboards.Boards));

            // zobrist equal
            Assert.That(board.Zobrist.HashValue, Is.EqualTo(boardFromFen.Zobrist.HashValue));
        });

    }
}