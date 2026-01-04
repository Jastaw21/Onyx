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
        player.HandleCommand("ucinewgame");
        player.HandleCommand("position startpos moves b1c3 e7e6 e2e3 b8c6 g1f3 g8f6 f1b5 a7a6 b5f1 e6e5 d2d4 e5e4 d4d5 c6a5 f3d2 f8b4 a2a3 b4c3 b2c3 d7d6 f1e2 c7c6 d5c6 a5c6 c1b2 d6d5 c3c4 d5c4 d2c4 c8e6 c4d6 e8f8 d1d2 d8d7 d6e4 f6e4 d2d7 e6d7 a1d1 d7f5 f2f4 e4f6 e2f3 f5g4 b2f6 g4f3 g2f3 g7f6 e3e4 a8c8 d1d7 f8e8 d7b7 c6d4 b7b6 a6a5 e1d1 d4c2 b6f6 c2a3 f6f5 c8b8 f5a5 f7f6 a5a3 b8b1 d1c2 b1h1 a3a8 e8f7 a8h8 h1h2 c2d3 h7h5 f4f5 h5h4 f3f4 h4h3 h8h7 f7g8 h7h6 h2a2 h6g6 g8f7 g6g1 h3h2 g1b1 f7e7 e4e5 a2a3 d3e4 a3a4 e4f3 a4b4 b1e1 f6e5 f4e5 b4b3 f3g2 b3b2 g2g3 b2e2 f5f6 e7e6 e1a1 e6f7 a1f1 e2e5 g3h2 e5e6 h2g1 e6f6 g1g2 f7e6 f1a1 e6d5 g2g1 f6b6 g1f1 d5c4 f1e1 c4c3 e1d1 c3b2 a1a4 b6b7 d1e1 b2b1 e1d1 b1b2 d1e1 b7c7 e1d1 c7d7 d1e2 b2b1 e2e1 d7e7 e1d1 b1b2 d1d2 e7b7 d2e2 b2b1 e2e1 b1c1 e1e2 c1b1 e2d1 b1b2");
        Assert.That(Referee.IsThreeFoldRepetition(player.Player.Board), Is.True);
        
        player.HandleCommand("ucinewgame");
        player.HandleCommand("position startpos moves e2e3 b8c6 b1c3 e7e6 d1g4 g8f6 g4f3 f8c5 c3a4 c6b4 e1d1 c5d6 a2a3 b4c6 a4c3 c6e5 f3e2 f6d5 g1f3 d5c3 b2c3 e5f3 g2f3 b7b6 d2d4 c7c5 c1b2 c8b7 e3e4 c5d4 c3d4 d6f4 h1g1 e8g8 h2h3 d7d5 d1e1 h7h5 e2d3 a7a5 a3a4 d8e7 b2a3 f4d6 a3d6 e7d6 e4e5 d6e7 d3a3 e7a3 a1a3 a8c8 e1d1 c8b8 g1g5 h5h4 a3b3 b7c6 f1b5 c6b5 b3b5 f7f6 e5f6 f8f6 b5b3 b6b5 g5g4 b5b4 g4h4 b8f8 d1e2 f8c8 e2d2 c8f8 d2e2 f8c8 e2d2 c8f8 d2e2");
        Assert.That(Referee.IsThreeFoldRepetition(player.Player.Board), Is.True);
    }
}