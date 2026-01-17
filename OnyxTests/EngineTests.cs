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
        var bestMove = engine.Search(new SearchParameters { TimeLimit = 1000 });
        Assert.Multiple(() =>
        {
            //Assert.That(bestMove.score, Is.EqualTo(30000));
            Assert.That(bestMove.BestMove.Notation, Is.EqualTo("d8h4"));
        });
        var timed = engine.Search(new SearchParameters { TimeLimit = 1000 });
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
            var bestMove = engine.Search(new SearchParameters { MaxDepth = 10 });
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
        var bestMove = engine.Search(new SearchParameters { MaxDepth = 10 });
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
            var directResult = engine.Search(new SearchParameters { MaxDepth = depthReached + 2 });
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
        var move = engine.Search(new SearchParameters { MaxDepth = 6 });
        Assert.That(move.BestMove.Notation, Is.EqualTo("f1c4"));

        engine.SetPosition("6k1/4pp1p/p5p1/1p1q4/4b1N1/P1Q4P/1PP3P1/7K w - - 0 1");
        move = engine.Search(new SearchParameters { TimeLimit = 4000 });
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
        engine.Search(new SearchParameters
            { MaxDepth = 3, TimeControl = tc, CancellationToken = new CancellationToken(false) });
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
        var unused = engine.Search(new SearchParameters { TimeLimit = 1000 });
        sw.Stop();

        Assert.That(sw.Elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(950).And.LessThanOrEqualTo(1050));
    }

    [Test]
    public void TimeManagerHandlesZeroMovesToGo()
    {
        var engine = new Engine();
        var tc = new TimeControl
        {
            Wtime = 1000,
            Btime = 1000,
            movesToGo = 0
        };
        // Should not throw DivideByZeroException
        var result = engine.Search(new SearchParameters { TimeControl = tc });
        Assert.That(result.BestMove.Notation, Is.Not.Null);
    }

    [Test]
    public void TestNullMoveOnTimeout()
    {
        var engine = new Engine();
        // Start position
        engine.SetPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // Search with a very short time limit, enough that depth 1 might not complete
        var result = engine.Search(new SearchParameters { TimeLimit = 1 });

        Console.WriteLine($"[DEBUG_LOG] Best move: {result.BestMove.Notation}");
        Assert.That(result.BestMove.Data, Is.Not.EqualTo(0),
            "Best move should not be zero (null) even on immediate timeout");
    }

    [Test]
    public void TestTimeOutWithWrapper()
    {
        var uci = new UciInterface();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            //uci.HandleCommand("position startpos moves e2e3 b8c6 b1c3 e7e6 d1g4 g8f6 g4f3 f8c5 c3a4 c6b4 e1d1 c5d6 a2a3 b4c6 a4c3 c6e5 f3h3 e5g4 d1e1 g4h2 f1c4 h2g4 d2d4 e6e5 g1e2 e5d4 e3d4 d8e7 c1g5 a7a6 a3a4 d6b4 e1f1 d7d5 c3d5 g4f2 h3h2 f2g4 h2g3 g4e3 g3e3 f6d5 c4d5 g5e3 g5e3 b4e7 a4a5 c7c6 d5e4 c8d7 c2c4 a8d8 c4c5 d7g4 e2c3 f7f5 e4b1 f5f4 e3f2 h7h5 b1d3 h5h4 d4d5 c6d5 a1e1 e8f7 c5c6 b7c6 d3a6 e7b4 h1h4 h8h4 f2h4 d8a8 a6b7 a8a5 h4d8 a5c5 d8e7 c5c3 e7b4 c3b3 e1e7 f7f6 b4c5 b3b2 c5d4 f6e7 d4b2 e7d6 b2g7 g4f5 b7a6 c6c5 g7h6 d6e5 a6e2 f5e4 h6g7 e5f5 f1f2 d5d4 g2g4 f4g3 f2g3 f5g6 g7f8 d4d3 e2d1 c5c4 d1a4 c3c2 a3c1 e4f5 g3f2 g6f6 f2e3 f5g6 c1b2 f6e6 b2c1 g6f5 a4b3 e6e5 c1b2 e5d6 b2a3 d6c6 a3c1 c6c5 c1d2 f5g6 d2c1 g6f5 c1d2 f5g6 b3a2 g6f5 a2b3");
            uci.HandleCommand("go depth 5");

            // Wait for the bestmove to appear in the output, with a timeout
            var stopwatch = Stopwatch.StartNew();
            string output = "";
            while (stopwatch.ElapsedMilliseconds < 5000)
            {
                output = sw.ToString();
                if (output.Contains("bestmove"))
                    break;
                Thread.Sleep(100);
            }

            TestContext.Out.WriteLine(output);
            Assert.That(output, Does.Contain("bestmove"));
            Assert.That(output, Does.Contain(" pv "));

            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var infoLines = lines.Where(l => l.StartsWith("info")).ToList();
            Assert.That(infoLines, Is.Not.Empty);

            foreach (var line in infoLines)
            {
                var pvIndex = line.IndexOf(" pv ");
                if (pvIndex != -1)
                {
                    var pvContent = line.Substring(pvIndex + 4).Trim();
                    Assert.That(pvContent, Is.Not.Empty, $"PV should not be empty in line: {line}");
                }
            }

            var bestMoveLine = lines.FirstOrDefault(l => l.StartsWith("bestmove"));
            Assert.That(bestMoveLine, Is.Not.Null);

            var move = bestMoveLine.Substring(9).Trim();
            Assert.That(move, Is.Not.EqualTo("a1a1"));
            Assert.That(move.Length, Is.AtLeast(4));
        }
        finally
        {
            Console.SetOut(originalOut);
            sw.Dispose();
        }
    }

    [Test]
    public void StressGame()
    {
        var uciInterface = new UciInterface();
        var testerboard = new Position();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        List<string> SentCommands =
        [
            "setoption name threads value 1",
            "isready",
            "ucinewgame",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1",
            "isready",
            "go wtime 12100 btime 12100 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5",
            "isready",
            "go wtime 11869 btime 11872 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7",
            "isready",
            "go wtime 11650 btime 11651 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6",
            "isready",
            "go wtime 11435 btime 11436 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6",
            "isready",
            "go wtime 11227 btime 11228 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7",
            "isready",
            "go wtime 11023 btime 11024 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4",
            "isready",
            "go wtime 10824 btime 10825 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8",
            "isready",
            "go wtime 10631 btime 10630 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6",
            "isready",
            "go wtime 10441 btime 10441 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6",
            "isready",
            "go wtime 10256 btime 10256 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8",
            "isready",
            "go wtime 9991 btime 9991 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6",
            "isready",
            "go wtime 9734 btime 9734 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6",
            "isready",
            "go wtime 9487 btime 9487 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5",
            "isready",
            "go wtime 9246 btime 9247 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7",
            "isready",
            "go wtime 9015 btime 9014 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8",
            "isready",
            "go wtime 8791 btime 8790 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5",
            "isready",
            "go wtime 8574 btime 8573 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7",
            "isready",
            "go wtime 8364 btime 8365 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5",
            "isready",
            "go wtime 8163 btime 8163 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4",
            "isready",
            "go wtime 7968 btime 7968 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6",
            "isready",
            "go wtime 7779 btime 7780 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6",
            "isready",
            "go wtime 7597 btime 7598 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8",
            "isready",
            "go wtime 7419 btime 7421 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8",
            "isready",
            "go wtime 7249 btime 7250 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1",
            "isready",
            "go wtime 7085 btime 7084 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5",
            "isready",
            "go wtime 6924 btime 6925 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5",
            "isready",
            "go wtime 6771 btime 6772 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8",
            "isready",
            "go wtime 6613 btime 6623 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7",
            "isready",
            "go wtime 6468 btime 6480 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8",
            "isready",
            "go wtime 6329 btime 6341 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8",
            "isready",
            "go wtime 6088 btime 6100 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4",
            "isready",
            "go wtime 5860 btime 5871 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4",
            "isready",
            "go wtime 5643 btime 5653 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8",
            "isready",
            "go wtime 5436 btime 5446 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7",
            "isready",
            "go wtime 5241 btime 5249 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6",
            "isready",
            "go wtime 5055 btime 5064 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7",
            "isready",
            "go wtime 4880 btime 4887 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6",
            "isready",
            "go wtime 4714 btime 4719 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8",
            "isready",
            "go wtime 4555 btime 4560 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6",
            "isready",
            "go wtime 4404 btime 4409 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8",
            "isready",
            "go wtime 4261 btime 4264 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5",
            "isready",
            "go wtime 4125 btime 4127 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5",
            "isready",
            "go wtime 3996 btime 3998 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5",
            "isready",
            "go wtime 3873 btime 3875 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5 g1h2 f5e4",
            "isready",
            "go wtime 3756 btime 3758 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5 g1h2 f5e4 e2f2 h7g8",
            "isready",
            "go wtime 3646 btime 3648 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5 g1h2 f5e4 e2f2 h7g8 b2a3 g7h6",
            "isready",
            "go wtime 3541 btime 3543 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5 g1h2 f5e4 e2f2 h7g8 b2a3 g7h6 a3c1 e4b1",
            "isready",
            "go wtime 3440 btime 3442 winc 100 binc 100",
            "position fen rnq1kb1r/pbppp2p/1p3np1/5p2/8/3P1NP1/PPPNPPBP/R1BQ1RK1 w kq - 0 1 moves e2e3 d7d5 f3e5 f8g7 d2f3 b8c6 e5c6 b7c6 f3e5 c6b7 d3d4 f6e4 b2b4 c8d8 c1b2 d8d6 a2a3 e7e6 d1d3 e8g8 f2f3 e4f6 b4b5 a7a6 a3a4 a6b5 a4b5 f6d7 f3f4 f8b8 b2a3 d7e5 f4e5 d6d7 a3b4 g6g5 b4c3 g5g4 f1d1 c7c6 b5c6 d7c6 d1c1 b8c8 a1a8 c8a8 c1a1 a8a1 c3a1 b6b5 a1c3 h7h5 d3d2 c6c8 g1f1 c8c7 f1f2 g8h8 h2h3 h8g8 h3g4 f5g4 f2g1 c7c4 c3b2 g8h8 d2a5 h8h7 a5b6 b7c6 c2c3 c6d7 b6d6 c4c6 d6e7 d7e8 e7b4 c6b6 g2f1 b6d8 f1b5 e8b5 b4b5 d8g5 b5e2 g5f5 g1h2 f5e4 e2f2 h7g8 b2a3 g7h6 a3c1 e4b1 c3c4 b1c1",
            "isready",
            "go wtime 3345 btime 3347 winc 100 binc 100",
            "isready",
        ];

        foreach (var command in SentCommands)
        {
            if (command.Contains("position"))
            {
                var movesStart = command.IndexOf("moves");
                if (movesStart>0)
                {
                    var movesString = command[movesStart..];
                }
            }
            uciInterface.HandleCommand(command);
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 1000)
            {
                var output = sw.ToString();
                if (output.Contains("bestmove"))
                    break;
                Thread.Sleep(100);
            }
        }
    }
}