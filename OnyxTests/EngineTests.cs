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
        var bestMove = engine.RequestSearch(1, new TimeControl());
        Assert.Multiple(() =>
        {
            Assert.That(bestMove.score, Is.EqualTo(30000));
            Assert.That(bestMove.bestMove.Notation, Is.EqualTo("d8h4"));
        });
    }

    [Test]
    public void FindsMateInTwo()
    {
        var feb = "6k1/7p/7B/5pp1/8/4bP1P/1q3PK1/5Q2 w - - 0 1";
        var engine = new Engine();
        engine.SetPosition(feb);
        var move = engine.RequestSearch(3, new TimeControl());
        Assert.That(move.bestMove.Notation, Is.EqualTo("f1c4"));

        engine.SetPosition("6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1");
        move = engine.RequestSearch(3, new TimeControl());
        Assert.That(move.bestMove.Notation, Is.EqualTo("g4h6"));
    }

    [Test]
    public void MoveResets()
    {
        var fen = "6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1";
        var engine = new Engine();
        engine.SetPosition(fen);
        engine.RequestSearch(3, new TimeControl());
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
        var result = engine.RequestSearch(10, timeControl);
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(800).And.LessThanOrEqualTo(1100));
    }
}