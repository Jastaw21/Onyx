using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class EvaluatorTests
{
    [Test]
    public void PieceSquareLogic()
    {
        Assert.That(Evaluator.GetPieceValueOnSquare(48, Piece.WP), Is.EqualTo(50));
        Assert.That(Evaluator.GetPieceValueOnSquare(48, Piece.BP), Is.EqualTo(5));
        Assert.That(Evaluator.GetPieceValueOnSquare(12, Piece.WP), Is.EqualTo(-20));
        Assert.That(Evaluator.GetPieceValueOnSquare(12, Piece.BP), Is.EqualTo(50));
    }

    [Test]
    public void EvaluateNeutral()
    {
        var board = new Position();
        Assert.That(Evaluator.Evaluate(board), Is.EqualTo(0));
    }

    [Test]
    public void EvaluateAheadOnMaterial()
    {
        // black missing a queen
        var board = new Position("rnb1kbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // white ahead
        Assert.That(Evaluator.Evaluate(board), Is.GreaterThan(0));
        board = new Position("rnb1kbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1");

        // black ahead
        Assert.That(Evaluator.Evaluate(board), Is.LessThan(0));
    }

    [Test]
    public void EndGameEvaluatesDifferentlyPawns()
    {
        var startPawn = Evaluator.GetPieceValueOnSquare(16, Piece.WP, false);
        var endPawn = Evaluator.GetPieceValueOnSquare(16, Piece.WP, true);
        Assert.That(endPawn, Is.Not.EqualTo(startPawn));
    }

    [Test]
    public void MoveSortingStabilityTests()
    {
        var pos = new Position("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
        Span<Move> moveBuffer = stackalloc Move[256];
        var legalMoveCount = MoveGenerator.GetLegalMoves(pos, moveBuffer);
        var moves = moveBuffer[..legalMoveCount];
        var _killerMoves = new Move?[128, 2];
        // test double killer moves

        var random = new Random();
        for (var i = 0; i < 20000; i++)
        {
            var km1 = random.Next(legalMoveCount);
            var km2 = random.Next(legalMoveCount);
            var tt = random.Next(legalMoveCount);
            _killerMoves[1,0] = moves[km1];
            _killerMoves[1,1] = moves[km2];
            var ttMoves = moves[tt];
            Evaluator.SortMoves(moves, ttMoves, _killerMoves, 1);
        }
        
        Assert.Pass();
    }
}