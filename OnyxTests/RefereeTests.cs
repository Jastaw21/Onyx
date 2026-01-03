using Onyx.Core;
using Onyx.Statics;
using Onyx.UCI;

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
        Assert.That(Referee.IsCheckmate(true,whiteInCheckMate).isCheckmate, Is.True);

        var blackInCheckMate = new Board("rnbqkbnr/ppppp2p/5p2/6pQ/8/8/PPPPPPPP/RNB1KBNR w KQkq g6 0 1");
        Assert.That(Referee.IsCheckmate(false,blackInCheckMate).isCheckmate, Is.True);
        
        var blackNotInCheckMate = new Board("rnbqkbnr/ppppp1pp/5p2/7Q/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1");
        Assert.That(Referee.IsCheckmate(false,blackNotInCheckMate).isCheckmate, Is.False);
        
        var blackInCheckMate2 = new Board("3N1Q2/7P/8/8/8/Qk4PK/4Q3/8 b - - 2 96");
        Assert.That(Referee.IsCheckmate(false,blackInCheckMate2).isCheckmate, Is.True);
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

    [Test]
    public void FurtherRepetitionTests()
    {
        var player = new UciInterface();
        player.HandleCommand("position startpos moves e2e3 b8c6 b1c3 e7e6 d1g4 g8f6 g4f3 f8c5 c3a4 c6b4 e1d1 c5d6 a2a3 b4c6 a4c3 c6e5 f3h3 e5g4 d1e1 g4h2 f1c4 h2g4 d2d4 e6e5 g1e2 e5d4 e3d4 d8e7 c1g5 a7a6 a3a4 d6b4 e1f1 d7d5 c3d5 g4f2 h3h2 f2g4 h2g3 g4e3 g3e3 f6d5 c4d5 e7e3 g5e3 b4e7 a4a5 c7c6 d5e4 c8d7 c2c4 a8d8 c4c5 d7g4 e2c3 f7f5 e4b1 f5f4 e3f2 h7h5 b1d3 h5h4 d4d5 c6d5 a1e1 e8f7 c5c6 b7c6 d3a6 e7b4 h1h4 h8h4 f2h4 d8a8 a6b7 a8a5 h4d8 a5c5 d8e7 c5c3 e7b4 c3b3 e1e7 f7f6 b4c5 b3b2 c5d4 f6e7 d4b2 e7d6 b2g7 g4f5 b7a6 c6c5 g7h6 d6e5 a6e2 f5e4 h6g7 e5f5 f1f2 d5d4 g2g4 f4g3 f2g3 f5g6 g7f8 d4d3 e2d1 c5c4 d1a4 c4c3 f8a3 c3c2 a3c1 e4f5 g3f2 g6f6 f2e3 f5g6 c1b2 f6e6 b2c1 g6f5 a4b3 e6e5 c1b2 e5d6 b2a3 d6c6 a3c1 c6c5 c1d2 f5g6 d2c1 g6f5 c1d2 f5g6 b3a2 g6f5 a2b3");
        
        Assert.That(Referee.IsThreeFoldRepetition(player.Player.Board), Is.True);
    }
}