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
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a8"), testBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a8"), testBoard, true), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a7"), testBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a7"), testBoard, true), Is.False);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b7"), testBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b7"), testBoard, true), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("c7"), testBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("c7"), testBoard, true), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("c1"), testBoard, false), Is.False);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("c1"), testBoard, true), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("d6"), testBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("d6"), testBoard, true), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("e6"), testBoard, false), Is.False);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("e6"), testBoard, true), Is.True);
        });

        var kingBoard = new Board("7K/8/8/8/8/8/8/k7 b - - 0 1");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a2"), kingBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b2"), kingBoard, false), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b1"), kingBoard, false), Is.True);

            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("g8"), kingBoard, true), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("g7"), kingBoard, true), Is.True);
            Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("h7"), kingBoard, true), Is.True);
        });

        var pawnBoard = new Board("8/P7/8/p1P5/8/2p1P2p/8/4p1P1 w - - 0 1");

        Assert.Multiple(() =>
            {
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a8"),pawnBoard,true),Is.False);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b8"),pawnBoard,true),Is.True);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b7"),pawnBoard,true),Is.False);
                
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("c6"),pawnBoard,true),Is.False);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b6"),pawnBoard,true),Is.True);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("d6"),pawnBoard,true),Is.True);
                
                
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("a4"),pawnBoard,false),Is.False);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b4"),pawnBoard,false),Is.True);
                Assert.That(Referee.IsSquareAttacked(RankAndFile.SquareIndex("b2"),pawnBoard,false),Is.True);
            }
        );
    }

    [Test]
    public void KingInCheck()
    {
        var defaultBoard = new Board();
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsInCheck(true, defaultBoard), Is.False);
            Assert.That(Referee.IsInCheck(false, defaultBoard), Is.False);
        });

        var checkBoard = new Board("8/8/8/8/8/8/2n5/K7 w - - 0 1");
        Assert.That(Referee.IsInCheck(true,checkBoard),Is.True);

        var scholarBoard = new Board("rnbqkbnr/ppppp1pp/8/5p1Q/4P3/8/PPPP1PPP/RNB1KBNR b KQkq - 1 2");
        Assert.That(Referee.IsInCheck(false,scholarBoard));
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
        Assert.That(Referee.IsCheckmate(true,whiteInCheckMate), Is.True);

        var blackInCheckMate = new Board("rnbqkbnr/ppppp2p/5p2/6pQ/8/8/PPPPPPPP/RNB1KBNR w KQkq g6 0 1");
        Assert.That(Referee.IsCheckmate(false,blackInCheckMate), Is.True);
        
        var blackNotInCheckMate = new Board("rnbqkbnr/ppppp1pp/5p2/7Q/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1");
        Assert.That(Referee.IsCheckmate(false,blackNotInCheckMate), Is.False);
        
        var blackInCheckMate2 = new Board("3N1Q2/7P/8/8/8/Qk4PK/4Q3/8 b - - 2 96");
        Assert.That(Referee.IsCheckmate(false,blackInCheckMate2), Is.True);
    }

    [Test]
    public void MoveLegalIfCapturePinningPiece()
    {
        var fen = "rnbq1k1r/pp1Pbppp/2p4B/8/2B5/8/PPP1NnPP/RN1QK2R b KQ - 2 8";
        var board = new Board(fen);
        var move = new Move(Piece.BP, "g7h6");
        Assert.That(Referee.MoveIsLegal(move, board), Is.True);
    }

    [Test]
    public void ThreefoldRepetition()
    {
        var whiteMove = new Move(Piece.WN, "b1c3");
        var blackMove = new Move(Piece.BN, "b8c6");
        
        var reverseWhiteMove = new Move(Piece.WN, "c3b1");
        var reverseBlackMove = new Move(Piece.BN, "c6b8");

        var board = new Board();
        // start in the state (1), apply and reverse once (2) and a second time (3)
        
        // apply once
        ApplyMovesInSequence();
        Assert.That(Referee.IsThreeFoldRepetition(board), Is.False);
        
        // apply twice
        ApplyMovesInSequence();
        Assert.That(Referee.IsThreeFoldRepetition(board), Is.True);

        void ApplyMovesInSequence()
        {
            board.ApplyMove(whiteMove);
            board.ApplyMove(blackMove);
            board.ApplyMove(reverseWhiteMove);
            board.ApplyMove(reverseBlackMove);
        }
    }
}