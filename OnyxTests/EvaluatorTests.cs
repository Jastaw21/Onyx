using Onyx.Core;
using Onyx.Statics;


namespace OnyxTests;

public class EvaluatorTests
{
    [Test]
    public void EvaluateAheadOnMaterial()
    {
        var board = new Board("8/p7/8/8/3P4/8/P7/8 b - - 0 1");
        Assert.That(Evaluator.Evaluate(board), Is.LessThan(0));
        board = new Board("8/p7/8/8/3P4/8/P7/8 w - - 0 1");
        Assert.That(Evaluator.Evaluate(board),Is.GreaterThan(0));
    }
}