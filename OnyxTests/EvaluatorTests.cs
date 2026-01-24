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

    [Test]
    public void MoveSortingChangesOrder()
    {
        var pos = new Position();
        Span<Move> moveBuffer = stackalloc Move[256];
        var legalMoveCount = MoveGenerator.GetLegalMoves(pos, moveBuffer);
        var moves = moveBuffer[..legalMoveCount];
        var ttMove = moves[4];
        Assert.That(moves[0], Is.Not.EqualTo(ttMove));
        Evaluator.SortMoves(moves, ttMove, null, 0);
        Assert.That(moves[0], Is.EqualTo(ttMove));
    }

    [Test]
    public void KingShield()
    {
        // equal 
        var board = new Position();
        var allShields = Evaluator.KingSafetyScore(board, true);
        
        
        // remove one white shielder
        board.SetFen("rnbqkbnr/pppppppp/8/8/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");
        var oneDown = Evaluator.KingSafetyScore(board,true);
        Assert.That(oneDown, Is.LessThan(allShields));
        
        // remove the second
        board.SetFen("rnbqkbnr/pppppppp/8/8/8/8/PPP2PPP/RNBQKBNR b KQkq - 0 1");
        var twoDown = Evaluator.KingSafetyScore(board,true);
        Assert.That(twoDown, Is.LessThan(oneDown));
        
        // remove the third
        board.SetFen("rnbqkbnr/pppppppp/8/8/8/8/PPP3PP/RNBQKBNR b KQkq - 0 1");
        var threeDown = Evaluator.KingSafetyScore(board,true);
        Assert.That(threeDown, Is.LessThan(twoDown));
        
        // remove a random, non shielding pawn
        board.SetFen("rnbqkbnr/pppppppp/8/8/8/8/1PP3PP/RNBQKBNR b KQkq - 0 1");
        var randomPawn = Evaluator.KingSafetyScore(board,true);
        Assert.That(randomPawn, Is.EqualTo(threeDown));
    }

    [Test]
    public void OpenFilesNearKing()
    {
        // equal 
        var board = new Position();
        var allShields = Evaluator.KingSafetyScore(board, true);
        
        // open the file
        board.SetFen("rnbqkbnr/pppp1ppp/8/8/8/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
        var openFileEvale = Evaluator.KingSafetyScore(board,true);
        Assert.That(openFileEvale, Is.LessThan(allShields));
        
        // close the file with an opposing pawn
        board.SetFen("rnbqkbnr/pppppppp/8/8/8/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
        var closeFileEvale = Evaluator.KingSafetyScore(board,true);
        Assert.That(closeFileEvale, Is.GreaterThan(openFileEvale));
    }

    [Test]
    public void EvalTableKeepsSense()
    {
        var evalTable = new EvaluationTable();
        var board = new Position("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");

        var eval = evalTable.Evaluate(board,0);
        var secondEval = evalTable.Evaluate(board,0);
        
        board.MakeNullMove();

        var flippedEval = evalTable.Evaluate(board,0);
        Assert.Multiple(() =>
        {
            Assert.That(eval, Is.EqualTo(secondEval));
            Assert.That(flippedEval, Is.EqualTo(-secondEval));
        });
    }
}