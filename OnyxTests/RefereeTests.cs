using Onyx;
using Onyx.Core;

namespace OnyxTests;

public class RefereeTests
{
    [Test]
    public void AttackedSquares()
    {
        var testBoard = new Board("1b6/7r/qNQ5/1n6/8/8/8/8 w - - 0 1");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsSquareAttacked(new Square("a8"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("a8"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("a7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("a7"), testBoard, Colour.White), Is.False);

            Assert.That(Referee.IsSquareAttacked(new Square("b7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b7"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("c7"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("c7"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("c1"), testBoard, Colour.Black), Is.False);
            Assert.That(Referee.IsSquareAttacked(new Square("c1"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("d6"), testBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("d6"), testBoard, Colour.White), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("e6"), testBoard, Colour.Black), Is.False);
            Assert.That(Referee.IsSquareAttacked(new Square("e6"), testBoard, Colour.White), Is.True);
        });

        var kingBoard = new Board("7K/8/8/8/8/8/8/k7 b - - 0 1");
        Assert.Multiple(() =>
        {
            Assert.That(Referee.IsSquareAttacked(new Square("a2"), kingBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b2"), kingBoard, Colour.Black), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("b1"), kingBoard, Colour.Black), Is.True);

            Assert.That(Referee.IsSquareAttacked(new Square("g8"), kingBoard, Colour.White), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("g7"), kingBoard, Colour.White), Is.True);
            Assert.That(Referee.IsSquareAttacked(new Square("h7"), kingBoard, Colour.White), Is.True);
        });

        var pawnBoard = new Board("8/P7/8/p1P5/8/2p1P2p/8/4p1P1 w - - 0 1");

        Assert.Multiple(() =>
            {
                Assert.That(Referee.IsSquareAttacked(new Square("a8"),pawnBoard,Colour.White),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b8"),pawnBoard,Colour.White),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("b7"),pawnBoard,Colour.White),Is.False);
                
                Assert.That(Referee.IsSquareAttacked(new Square("c6"),pawnBoard,Colour.White),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b6"),pawnBoard,Colour.White),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("d6"),pawnBoard,Colour.White),Is.True);
                
                
                Assert.That(Referee.IsSquareAttacked(new Square("a4"),pawnBoard,Colour.Black),Is.False);
                Assert.That(Referee.IsSquareAttacked(new Square("b4"),pawnBoard,Colour.Black),Is.True);
                Assert.That(Referee.IsSquareAttacked(new Square("b2"),pawnBoard,Colour.Black),Is.True);
            }
        );
    }
}