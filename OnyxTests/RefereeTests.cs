using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class RefereeTests
{
    [Test]
    public void AttackedSquares()
    {
        var testBoard = new Board("1b6/7r/qNQ5/1n6/8/8/8/8 w - - 0 1");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsSquareAttacked(new Square("a8"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("a8"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("a7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("a7"), testBoard, Colour.White), Is.False);

            Assert.That(Referee.IsSquareAttacked(new Square("b7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b7"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("c7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("c7"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("c1"), testBoard, Colour.Black), Is.False);
            Assert.That(Referee.IsSquareAttacked(new Square("c1"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("d6"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("d6"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("e6"), testBoard, Colour.Black), Is.False);
            Assert.That(Referee.IsSquareAttacked(new Square("e6"), testBoard, Colour.White), Is.True);
        });

        var kingBoard = new Board("7K/8/8/8/8/8/8/k7 b - - 0 1");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsSquareAttacked(new Square("a2"), kingBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b2"), kingBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b1"), kingBoard, Colour.Black), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("g8"), kingBoard, Colour.White), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("g7"), kingBoard, Colour.White), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("h7"), kingBoard, Colour.White), Is.True);
        });

        var pawnBoard = new Board("8/P7/8/p1P5/8/2p1P2p/8/4p1P1 w - - 0 1");

        Assert.Multiple(() =>
            {
                Assert.That(Referee.IsSquareAttacked(new Square("a8"),pawnBoard,Colour.White),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b8"),pawnBoard,Colour.White),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("b7"),pawnBoard,Colour.White),Is.False);
                
                Assert.That(Referee.IsSquareAttacked(new Square("c6"),pawnBoard,Colour.White),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b6"),pawnBoard,Colour.White),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("d6"),pawnBoard,Colour.White),Is.True);
                
                
                Assert.That(Referee.IsSquareAttacked(new Square("a4"),pawnBoard,Colour.Black),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b4"),pawnBoard,Colour.Black),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("b2"),pawnBoard,Colour.Black),Is.True);
            }
        );
    }

    [Test]
    public void KingInCheck()
    {
        var defaultBoard = new Board();
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsInCheck(Colour.White, defaultBoard), Is.False);
            Assert.That(Referee.IsInCheck(Colour.Black, defaultBoard), Is.False);
        });

        var checkBoard = new Board("8/8/8/8/8/8/2n5/K7 w - - 0 1");
        Assert.That(Referee.IsInCheck(Colour.White,checkBoard),Is.True);

        var scholarBoard = new Board("rnbqkbnr/ppppp1pp/8/5p1Q/4P3/8/PPPP1PPP/RNB1KBNR b KQkq - 1 2");
        Assert.That(Referee.IsInCheck(Colour.Black,scholarBoard));
    }

    [Test]
    public void CantMoveIntoCheck()
    {
        var board = new Board("qrn1bnrb/pppp1ppp/N7/3k4/4p3/5B2/PPPPPPPP/QR1K1NRB b - - 0 1");
        var exposeKingMove = new Move(Piece.BP, "e4e3");
        var normalKingMove = new Move(Piece.BK, "d5d6");
        var moveIntoCheck = new Move(Piece.BK, "d5c5");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.MoveIsLegal(exposeKingMove, board), Is.False);
            Assert.That(Referee.MoveIsLegal(normalKingMove, board), Is.True);
            Assert.That(Referee.MoveIsLegal(moveIntoCheck, board), Is.False);
        });
    }

    [Test]
    public void CheckmateTestWorks()
    {
        var whiteInCheckMate = new Board("rnb1kbnr/ppp2ppp/3pp3/6P1/7q/5P2/PPPPP2P/RNBQKBNR w KQkq - 0 1");
        Assert.That(Referee.IsCheckmate(Colour.White,whiteInCheckMate), Is.True);

        var blackInCheckMate = new Board("rnbqkbnr/ppppp2p/5p2/6pQ/8/8/PPPPPPPP/RNB1KBNR w KQkq g6 0 1");
        Assert.That(Referee.IsCheckmate(Colour.Black,blackInCheckMate), Is.True);
        
        var blackNotInCheckMate = new Board("rnbqkbnr/ppppp1pp/5p2/7Q/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1");
        Assert.That(Referee.IsCheckmate(Colour.Black,blackNotInCheckMate), Is.True);
    }

    [Test]
    public void MoveLegalIfCapturePinningPiece()
    {
        var fen = "rnbq1k1r/pp1Pbppp/2p4B/8/2B5/8/PPP1NnPP/RN1QK2R b KQ - 2 8";
        var board = new Board(fen);
        var move = new Move(Piece.BP, "g7h6");
        Assert.That(Referee.MoveIsLegal(move, board), Is.True);
    }
}