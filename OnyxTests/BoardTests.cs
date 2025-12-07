using Onyx;

namespace OnyxTests;

public class ApplyMove
{
    [Test]
    public void ApplyPawnPush()
    {
        var board = new Board(Fen.DefaultFen);

        var move = new Move(
            Piece.MakePiece(PieceType.Pawn, Colour.White),
            new Square("a2"),
            new Square("a4")
        );

        board.ApplyMove(move);
        Assert.That(board.Bitboards.GetFen(), Is.EqualTo(
            "rnbqkbnr/pppppppp/8/8/P7/8/1PPPPPPP/RNBQKBNR"
        ));

        board.ApplyMove(new Move(Piece.MakePiece(PieceType.Pawn, Colour.Black), "a7a5"));
        Assert.That(board.Bitboards.GetFen(), Is.EqualTo(
            "rnbqkbnr/1ppppppp/8/p7/P7/8/1PPPPPPP/RNBQKBNR"
        ));
    }

    [Test]
    public void CaptureUpdatesBoardState()
    {
        var board = new Board("rnb1kbnr/ppp1pppp/3p4/8/5q2/3P4/PPPQPPPP/RNB1KBNR w KQkq - 0 1");

        var queenCapture = new Move(Piece.MakePiece(PieceType.Queen, Colour.White), "d2f4");
        board.ApplyMove(queenCapture);

        Assert.That(board.GetFen(), Is.EqualTo("rnb1kbnr/ppp1pppp/3p4/8/5Q2/3P4/PPP1PPPP/RNB1KBNR b KQkq - 0 1"));
    }

    [Test]
    public void CastlingUpdatesBoardState()
    {
        // white able to castle king side
        var whiteKingSide = new Board("rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7");

        var castlingMove = new Move(Piece.MakePiece(PieceType.King, Colour.White), "e1g1");

        whiteKingSide.ApplyMove(castlingMove);

        Assert.That(whiteKingSide.GetFen(),
            Is.EqualTo("rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQ1RK1 b q - 0 1"));


        var whiteQueenSide = new Board("rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/R3KBNR w KQkq - 2 6");
        var qsCastlingMove = new Move(Piece.MakePiece(PieceType.King, Colour.White), "e1c1");
        whiteQueenSide.ApplyMove(qsCastlingMove);
        Assert.That(whiteQueenSide.GetFen(),
            Is.EqualTo("rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/2KR1BNR b kq - 0 1"));

        var blackKingSide = new Board("rn1qk2r/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR b kq - 5 7");
        var bKsCastlingMove = new Move(Piece.MakePiece(PieceType.King, Colour.Black), "e8g8");
        blackKingSide.ApplyMove(bKsCastlingMove);
        Assert.That(blackKingSide.GetFen(),
            Is.EqualTo("rn1q1rk1/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR w - - 0 1"));

        var blackQueenSide = new Board("r3k2r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR b kq - 2 10");
        var bQsCastlingMove = new Move(Piece.MakePiece(PieceType.King, Colour.Black), "e8c8");
        blackQueenSide.ApplyMove(bQsCastlingMove);
        Assert.That(blackQueenSide.GetFen(),
            Is.EqualTo("2kr3r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 0 1"));
    }

    [Test]
    public void PromotionUpdatesBoardState()
    {
        // white can promote from here
        var board = new Board("3rb2r/pPqkbpp1/n2ppn1p/8/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 1 13");
        var promotionMove = new Move(Piece.MakePiece(PieceType.Pawn, Colour.White), "b7b8q");

        board.ApplyMove(promotionMove);
        Assert.That(board.GetFen(), Is.EqualTo("1Q1rb2r/p1qkbpp1/n2ppn1p/8/2Q4B/2P5/PP1NPPPP/2KR1BNR b - - 0 1"));
    }

    [Test]
    public void MovingKingLosesCastlingRights()
    {
        var board = new Board("rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPP1PP/RNBQKBNR w KQkq - 0 2");
        var kingMove = new Move(Piece.MakePiece(PieceType.King, Colour.White), "e1f2");
        board.ApplyMove(kingMove);

        Assert.That(board.GetFen(), Is.EqualTo("rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 0 1"));

        var blackBoard = new Board("rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2");
        var blackKingMove = new Move(Piece.MakePiece(PieceType.King, Colour.Black), "e8e7");
        blackBoard.ApplyMove(blackKingMove);
        Assert.That(blackBoard.GetFen(), Is.EqualTo("rnbq1bnr/ppppkppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR w - - 0 1"));
    }

    [Test]
    public void MovingRookLosesCastlingRights()
    {
        var board = new Board("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/R1BQKBNR w KQkq - 2 3");
        var rookMove = new Move(Piece.MakePiece(PieceType.Rook, Colour.White), "a1b1");

        board.ApplyMove(rookMove);
        Assert.Multiple(() =>
        {
            Assert.That(board.GetFen(), Is.EqualTo("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/1RBQKBNR b Kkq - 0 1"));
            Assert.That(board.CastlingRights,
                Is.EqualTo(BoardConstants.WhiteKingsideCastling | BoardConstants.BlackKingsideCastling |
                           BoardConstants.BlackQueensideCastling));
        });

        var blackRookMove = new Move(Piece.MakePiece(PieceType.Rook, Colour.Black), "h8g8");
        board.ApplyMove(blackRookMove);
        Assert.Multiple(() =>
        {
            Assert.That(board.GetFen(), Is.EqualTo("rnbqkbr1/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/1RBQKBNR w Kq - 0 1"));
            Assert.That(board.CastlingRights,
                Is.EqualTo(BoardConstants.WhiteKingsideCastling | BoardConstants.BlackQueensideCastling));
        });
    }

    [Test]
    public void MovingRookBackDoesntResetCastling()
    {
        // black has already lost castling rights but the rook is back in it's starting square
        var board = new Board("rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N3P1/PP1PPPBP/1RBQK1NR b Kq - 2 5");

        // check that black can't castle queen side
        Assert.That(board.CastlingRights,
            Is.EqualTo(BoardConstants.WhiteKingsideCastling | BoardConstants.BlackQueensideCastling));

        // move the rook out to call the castlingrigths function
        var blackRookMove = new Move(Piece.MakePiece(PieceType.Rook, Colour.Black), "h8g8");
        board.ApplyMove(blackRookMove);

        // check nothing has changed
        Assert.That(board.CastlingRights,
            Is.EqualTo(BoardConstants.WhiteKingsideCastling | BoardConstants.BlackQueensideCastling));
    }
}

public class UndoMove
{
    [Test]
    public void UndoPush()
    {
        
    }
}
public class BoardTests
{
    [Test]
    public void InitFromFen()
    {
        var board = new Board(Fen.DefaultFen);

        Assert.That(board.TurnToMove, Is.EqualTo(Colour.White));
        Assert.That(board.Bitboards.GetFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"));
        Assert.That(board.EnPassantSquare.HasValue, Is.False);
    }


    [Test]
    public void GetFenReflectsCastlingRules()
    {
        var board = new Board("r1bqkbr1/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R w Kq - 6 4");

        Assert.That(board.GetFen(), Is.EqualTo("r1bqkbr1/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R w Kq - 0 1"));
    }
}               