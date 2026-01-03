using Onyx.Core;

namespace OnyxTests;

public class ZobristTests
{
    [Test]
    public void MultipleInitsGiveSameValue()
    {
        var zob1 = new Zobrist(Fen.DefaultFen);
        var zob2 = new Zobrist(Fen.DefaultFen);
        Assert.That(zob2.HashValue, Is.EqualTo(zob1.HashValue));
    }

    [Test]
    public void DifferentInitsGiveDifferentValue()
    {
        var zob1 = new Zobrist(Fen.DefaultFen);
        var zob2 = new Zobrist(Fen.KiwiPeteFen);
        Assert.That(zob2.HashValue, Is.Not.EqualTo(zob1.HashValue));
    }

    [Test]
    public void ApplyMoveGivesSameAsExpectedFen()
    {
        List<string> startingFen =
        [
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPP1PP/RNBQKBNR w KQkq - 0 2",
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2",
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/R3KBNR w KQkq - 2 6",
            "rn1qk2r/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR b kq - 5 7",
            "r3k2r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR b kq - 2 10",
            "rnbqkbnr/pp1p1ppp/2p5/3Pp3/8/8/PPP1PPPP/RNBQKBNR w KQkq e6 0 1",
            "rnbqkbnr/ppp1pppp/8/8/3pP3/1PP5/P2P1PPP/RNBQKBNR b KQkq e3 0 1",
            "rnb1kbnr/ppp1pppp/3p4/8/5q2/3P4/PPPQPPPP/RNB1KBNR w KQkq - 0 1"
        ];
        List<Move> moves =
        [
            new(Piece.WK, "e1f2"),
            new(Piece.BK, "e8e7"),
            new(Piece.WK, "e1g1"),
            new(Piece.WK, "e1c1"),
            new(Piece.BK, "e8g8"),
            new(Piece.BK, "e8c8"),
            new(Piece.WP, "d5e6"),
            new(Piece.BP, "d4e3"),
            new(Piece.WQ, "d2f4")
        ];

        List<string> PositionsAfter =
        [
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2",
            "rnbq1bnr/ppppkppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR w - - 2 3",
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQ1RK1 b q - 1 7",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/2KR1BNR b kq - 3 6",
            "rn1q1rk1/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR w - - 6 8",
            "2kr3r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR w - - 3 11",
            "rnbqkbnr/pp1p1ppp/2p1P3/8/8/8/PPP1PPPP/RNBQKBNR b KQkq - 0 1",
            "rnbqkbnr/ppp1pppp/8/8/8/1PP1p3/P2P1PPP/RNBQKBNR w KQkq - 0 2",
            "rnb1kbnr/ppp1pppp/3p4/8/5Q2/3P4/PPP1PPPP/RNB1KBNR b KQkq - 0 1"
        ];

        for (int i = 0; i < startingFen.Count; i++)
        {
            var board = new Board(startingFen[i]);
            var zobFromFen = new Zobrist(PositionsAfter[i]);
            var move = moves[i];
            board.ApplyMove(move);
            Assert.That(board.Zobrist.HashValue, Is.EqualTo(zobFromFen.HashValue));
        }
    }
    
    [Test]
    public void UndoMoveWorks()
    {
        List<string> startingFen =
        [
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPP1PP/RNBQKBNR w KQkq - 0 2",
            "rnbqkbnr/pppp1ppp/4p3/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 1 2",
            "rnbqkb1r/1pp2ppp/p2p1n2/4p3/2P5/2N2NP1/PP1PPPBP/1RBQK2R w Kq - 0 7",
            "rn1qkb1r/pppb1pp1/3ppn1p/6B1/Q2P4/2P5/PP1NPPPP/R3KBNR w KQkq - 2 6",
            "rn1qk2r/pppbbpp1/3ppn1p/6B1/1Q1P4/2P5/PP1NPPPP/2KR1BNR b kq - 5 7",
            "r3k2r/ppqbbpp1/n1pppn1p/3P4/2Q4B/2P5/PP1NPPPP/2KR1BNR b kq - 2 10",
            "rnbqkbnr/pp1p1ppp/2p5/3Pp3/8/8/PPP1PPPP/RNBQKBNR w KQkq e6 0 1",
            "rnbqkbnr/ppp1pppp/8/8/3pP3/1PP5/P2P1PPP/RNBQKBNR b KQkq e3 0 1",
            "rnb1kbnr/ppp1pppp/3p4/8/5q2/3P4/PPPQPPPP/RNB1KBNR w KQkq - 0 1"
        ];
        List<Move> moves =
        [
            new(Piece.WK, "e1f2"),
            new(Piece.BK, "e8e7"),
            new(Piece.WK, "e1g1"),
            new(Piece.WK, "e1c1"),
            new(Piece.BK, "e8g8"),
            new(Piece.BK, "e8c8"),
            new(Piece.WP, "d5e6"),
            new(Piece.BP, "d4e3"),
            new (Piece.WQ, "d2f4")
        ];
        

        for (int i = 0; i < startingFen.Count; i++)
        {
            var board = new Board(startingFen[i]);
            var hashPre = board.Zobrist.HashValue;
            var move = moves[i];
            board.ApplyMove(move);
            board.UndoMove(move);
            Assert.That(board.Zobrist.HashValue, Is.EqualTo(hashPre));
        }
    }
    [Test]
    public void ZobristHashIsStableWithRepeatedMoves()
    {
        var board = new Board("8/8/8/2k2b2/8/1B1pK3/2pB4/8 b - - 23 69");
        var hashPre = board.Zobrist.HashValue;
        board.ApplyMove(new Move(Piece.BB, "f5g6"));
        board.ApplyMove(new Move(Piece.WB, "b3a2"));
        board.ApplyMove(new Move(Piece.BB, "g6f5"));
        board.ApplyMove(new Move(Piece.WB, "a2b3"));
        Assert.That(board.Zobrist.HashValue, Is.EqualTo(hashPre));
    }
}