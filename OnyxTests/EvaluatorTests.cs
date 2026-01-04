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
}