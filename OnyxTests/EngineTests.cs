using System.Diagnostics;
using Onyx.Core;
using Onyx.Statics;
using Onyx.UCI;

namespace OnyxTests;

public class EngineTests
{
    [Test]
    public void FindsMateInOne()
    {
        var fen = "rnbqkbnr/pppp1ppp/4p3/8/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 1";
        var engine = new Engine();
        engine.SetPosition(fen);
        var bestMove = engine.DepthSearch(1);
        Assert.Multiple(() =>
        {
            //Assert.That(bestMove.score, Is.EqualTo(30000));
            Assert.That(bestMove.bestMove.Notation, Is.EqualTo("d8h4"));
        });
        var timed = engine.TimedSearch(1, 1000);
        Assert.Multiple(() => { Assert.That(timed.bestMove.Notation, Is.EqualTo("d8h4")); });
    }

    [Test]
    public void FindsMateInThree()
    {
        var fen = "3NQQ2/P6P/8/8/8/1k4PK/8/8 w - - 0 94";
        var engine = new Engine();
        engine.SetPosition(fen);
        var bestMove = engine.DepthSearch(6);
        //Assert.Multiple(() => { Assert.That(bestMove.bestMove.Notation, Is.EqualTo("e8e2")); });
        
        engine.SetPosition("Q2NQQQQ/8/8/8/8/8/5K2/1k6 w - - 5 104");
        bestMove = engine.DepthSearch(3);
        Assert.Multiple(() => { Assert.That(bestMove.score, Is.GreaterThan(20000)); });
    }

    [Test]
    public void returnsBestMoveFromDepthOneIfTimedOut()
    {
        var fen = "r1b1kbbR/8/3p4/2n5/3K1Pnp/3N4/2q5/R2b4 b q - 3 38";
        var engine = new Engine();
        engine.SetPosition(fen);

       

        for (int i = 2; i < 7; i++)
        {
            var timedSearch = engine.TimedSearch(i, 2000);
            var depthReached = timedSearch.stats.Depth;
            var directResult = engine.DepthSearch(depthReached);
            var match = timedSearch.bestMove.Notation == directResult.bestMove.Notation;
            var passString = match ? "passes" : $"fails with {timedSearch.bestMove}";
            TestContext.Out.WriteLine($"Depth {i} {passString}");
            Assert.That(match);
        }
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
        move = engine.TimedSearch(3, 4000);
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
}