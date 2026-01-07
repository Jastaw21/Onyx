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
        var bestMove = engine.Search(new SearchParameters { MaxDepth = 1 });
        Assert.Multiple(() =>
        {
            //Assert.That(bestMove.score, Is.EqualTo(30000));
            Assert.That(bestMove.BestMove.Notation, Is.EqualTo("d8h4"));
        });
        var timed = engine.Search(new SearchParameters { MaxDepth = 1, TimeLimit = 1000 });
        Assert.Multiple(() => { Assert.That(timed.BestMove.Notation, Is.EqualTo("d8h4")); });
    }

    [Test]
    public void FindsCheckmate()
    {
        var engine = new Engine();
        List<string> startingPositions =
        [
            "4R3/8/8/6P1/5k2/8/Q5R1/1K6 w - - 17 77",
            "4R3/8/8/6P1/8/5k2/Q5R1/2K5 w - - 15 76",
            "4R3/8/8/6P1/5k2/8/6R1/1Q2K3 w - - 9 73",
            "r3kbnr/pppp1pp1/8/2P1p3/4q2p/PP5P/b2P1PP1/2BK1BNR b kq - 1 11",
            "r4bnr/pp1k2p1/2p2p2/4p3/PP5p/B2b3P/3P1P2/3K2Nq b - - 3 20"
        ];

        foreach (var startingPosition in startingPositions)
        {
            engine.SetPosition(startingPosition);
            var bestMove = engine.Search(new SearchParameters { MaxDepth = 12 });
            engine.Position.ApplyMove(bestMove.BestMove);
            Assert.Multiple(() =>
            {
                Assert.That(bestMove.Score, Is.GreaterThan(20000));
                Assert.That(Referee.GetBoardState(engine.Position), Is.EqualTo(BoardStatus.Checkmate));
            });
            engine.Position.UndoMove(bestMove.BestMove);
        }
        
        engine.Reset();
        
        // now timed
        foreach (var startingPosition in startingPositions)
        {
            engine.SetPosition(startingPosition);
            var parameters = new SearchParameters
            {
                TimeControl = new TimeControl { Wtime = 32000, Btime = 32000, movesToGo = 3},
                CancellationToken = new CancellationToken(false)
            };
            var bestMove = engine.Search(parameters);
            engine.Position.ApplyMove(bestMove.BestMove);
            Assert.Multiple(() =>
            {
                Assert.That(bestMove.Score, Is.GreaterThan(20000));
                Assert.That(Referee.GetBoardState(engine.Position), Is.EqualTo(BoardStatus.Checkmate));
            });
            engine.Position.UndoMove(bestMove.BestMove);
        }
    }

    [Test]
    public void FindsMateInThree()
    {
        var fen = "3NQQ2/P6P/8/8/8/1k4PK/8/8 w - - 0 94";
        var engine = new Engine();
        engine.SetPosition(fen);
        engine.SetPosition("Q2NQQQQ/8/8/8/8/8/5K2/1k6 w - - 5 104");
        var bestMove = engine.Search(new SearchParameters { MaxDepth = 3 });
        Assert.Multiple(() => { Assert.That(bestMove.Score, Is.GreaterThan(20000)); });
    }

    [Test]
    public void returnsBestMoveFromDepthOneIfTimedOut()
    {
        var fen = "r1b1kbbR/8/3p4/2n5/3K1Pnp/3N4/2q5/R2b4 b q - 3 38";
        var engine = new Engine();
        engine.SetPosition(fen);

        for (int i = 2; i < 4; i++)
        {
            var timedSearch = engine.Search(new SearchParameters { MaxDepth = i, TimeLimit = 2000 });
            var depthReached = engine._statistics.Depth;
            var directResult = engine.Search(new SearchParameters { MaxDepth = depthReached+2 });
            var match = timedSearch.BestMove.Notation == directResult.BestMove.Notation;
            var passString = match ? "passes" : $"fails with {timedSearch.BestMove}";
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
        var move = engine.Search(new SearchParameters { MaxDepth = 3 });
        Assert.That(move.BestMove.Notation, Is.EqualTo("f1c4"));

        engine.SetPosition("6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1");
        move = engine.Search(new SearchParameters { MaxDepth = 3, TimeLimit = 4000 });
        Assert.That(move.BestMove.Notation, Is.EqualTo("g4h6"));
    }

    [Test]
    public void MoveResets()
    {
        var fen = "6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1";
        var engine = new Engine();
        engine.SetPosition(fen);
        var tc = new TimeControl
        {
            Wtime = 1000,
            Btime = 1000
        };
        engine.Search(new SearchParameters { MaxDepth = 3, TimeControl = tc, CancellationToken = new CancellationToken(false) });
        Assert.That(engine.Position.GetFen(), Is.EqualTo(fen));
    }

    [Test]
    public void MoveResetExtension()
    {
        var board = new Position("5k2/4pp1p/p5pN/1p1q4/4b3/P1Q4P/1PP3P1/7K w - - 0 1");
        var move = new Move(Piece.WQ, "c3h8");
        var fenPre = board.GetFen();
        board.ApplyMove(move);
        var unused = Referee.GetBoardState(board);
        board.UndoMove(move);
        Assert.That(board.GetFen(), Is.EqualTo(fenPre));
    }

    [Test]
    public void TimedSearchExitsRight()
    {
        var engine = new Engine();

        var sw = Stopwatch.StartNew();
        var unused = engine.Search(new SearchParameters { MaxDepth = 10, TimeLimit = 1000 });
        sw.Stop();

        Assert.That(sw.Elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(900).And.LessThanOrEqualTo(1100));
    }
}