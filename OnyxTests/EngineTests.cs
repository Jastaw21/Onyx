using System.Diagnostics;
using Onyx.Core;
using Onyx.Statics;
using Onyx.UCI;


namespace OnyxTests;

public class EngineTests
{
    private string mateOne = "r1b1kbbR/8/3p4/2n5/3K1Pnp/3N4/2q5/R2b4 b q - 3 38";
    [Test]
    public void FindsMateInOne()
    {
        var fen = "rnbqkbnr/pppp1ppp/4p3/8/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 1";
        var engine = new Engine();
        engine.SetPosition(fen);
        var bestMove = engine.CalcAndDispatchTimedSearch(1, new TimeControl());
        Assert.Multiple(() =>
        {
            Assert.That(bestMove.score, Is.EqualTo(30000));
            Assert.That(bestMove.bestMove.Notation, Is.EqualTo("d8h4"));
        });
        var timed = engine.TimedSearch(1, 1000);
        Assert.Multiple(() => { Assert.That(timed.bestMove.Notation, Is.EqualTo("d8h4")); });
    }

    [Test]
    public void FindsMateInTwo()
    {
        var feb = "6k1/7p/7B/5pp1/8/4bP1P/1q3PK1/5Q2 w - - 0 1";
        var engine = new Engine();
        engine.SetPosition(feb);
        var move = engine.TimedSearch(3, 600);
        Assert.That(move.bestMove.Notation, Is.EqualTo("f1c4"));

        engine.SetPosition("6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1");
        move = engine.TimedSearch(3, 600);
        Assert.That(move.bestMove.Notation, Is.EqualTo("g4h6"));
    }

    [Test]
    public void MoveResets()
    {
        var fen = "6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1";
        var engine = new Engine();
        engine.SetPosition(fen);
        engine.CalcAndDispatchTimedSearch(3, new TimeControl());
        Assert.That(engine.Position, Is.EqualTo(fen));
    }

    [Test]
    public void MoveResetExtension()
    {
        var board = new Board("5k2/4pp1p/p5pN/1p1q4/4b3/P1Q4P/1PP3P1/7K w - - 0 1");
        var move = new Move(Piece.WQ, "c3h8");
        var fenPre = board.GetFen();
        board.ApplyMove(move);
        var result = Referee.IsCheckmate(board);
        board.UndoMove(move);
        Assert.That(board.GetFen(), Is.EqualTo(fenPre));
    }

    [Test]
    public void TimedSearchExitsRight()
    {
        var engine = new Engine();

        var sw = Stopwatch.StartNew();
        var timeControl = new TimeControl
        {
            Btime = 1000,
            Wtime = 1000
        };
        var result = engine.TimedSearch(10, 1000);
        sw.Stop();

        Assert.That(sw.Elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(900).And.LessThanOrEqualTo(1100));
    }

    
    [Test]
    public void EDPResults()
    {
        List<string> startpos =
        [
            "1rbq1rk1/p1b1nppp/1p2p3/8/1B1pN3/P2B4/1P3PPP/2RQ1R1K w - - 0 1",
            "3r2k1/p2r1p1p/1p2p1p1/q4n2/3P4/PQ5P/1P1RNPP1/3R2K1 b - - 0 1"
        ];
        List<bool> bestMoves =
        [
            true,
            true
        ];
        List<Move> expectedMove =
        [
            new Move(Piece.WN, "e4f6"),
            new Move(Piece.BN, "f5d4")
        ];
        var engine = new Engine();
        var passCount = 0;
        var failCount = 0;
        for (var i = 0; i < startpos.Count; i++)
        {
            engine.SetPosition(startpos[i]);
            var result = engine.TimedSearch(10, 3000);
            if (bestMoves[i])
                if (result.bestMove.Notation == expectedMove[i].Notation)
                    passCount++;
                else
                    failCount++;
            else
            {
                if (result.bestMove.Notation == expectedMove[i].Notation)
                    failCount++;
                else
                    passCount++;
            }

            var type = bestMoves[i] ? "hit" : "avoid";
            TestContext.Out.WriteLine($"Position {i}, expected to {type} move {expectedMove[i].Notation}, got {result.bestMove.Notation}");
            
        }
        Assert.That(failCount, Is.EqualTo(0));
    }
}