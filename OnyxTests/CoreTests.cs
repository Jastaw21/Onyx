namespace OnyxTests;

using Onyx;

public class CoreTests
{
    [Test]
    public void SquareInitFromSquareIndex()
    {
        var bottomLeft = new Square(0);
        Assert.Multiple(() =>
        {
            Assert.That(bottomLeft.RankIndex, Is.EqualTo(0));
            Assert.That(bottomLeft.FileIndex, Is.EqualTo(0));
        });

        var topRight = new Square(63);
        Assert.Multiple(() =>
        {
            Assert.That(topRight.RankIndex, Is.EqualTo(7));
            Assert.That(topRight.FileIndex, Is.EqualTo(7));
        });

        var topLeft = new Square(56);
        Assert.Multiple(() =>
        {
            Assert.That(topLeft.RankIndex, Is.EqualTo(7));
            Assert.That(topLeft.FileIndex, Is.EqualTo(0));
        });

        var bottomRight = new Square(7);
        Assert.Multiple(() =>
        {
            Assert.That(bottomRight.RankIndex, Is.EqualTo(0));
            Assert.That(bottomRight.FileIndex, Is.EqualTo(7));
        });
    }

    [Test]
    public void SquareIndexGetProperty()
    {
        for (int i = 0; i < 64; i++)
        {
            var thisSquare = new Square(i);
            Assert.That(thisSquare.SquareIndex, Is.EqualTo(i));
        }
    }

    [Test]
    public void SquareRepr()
    {
        var bottomLeft = new Square(0);
        Assert.That(bottomLeft.Notation, Is.EqualTo("a1"));

        var topRight = new Square(63);
        Assert.That(topRight.Notation, Is.EqualTo("h8"));

        var topLeft = new Square(56);
        Assert.That(topLeft.Notation, Is.EqualTo("a8"));

        var bottomRight = new Square(7);
        Assert.That(bottomRight.Notation, Is.EqualTo("h1"));
    }

    [Test]
    public void BoardInit()
    {
        var board = new Bitboards();

        ulong testValue = 0;
        foreach (Colour colour in Enum.GetValues<Colour>())
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            board.SetByPiece(new Piece(type, colour), testValue);
            testValue++;
        }

        testValue = 0;

        foreach (Colour colour in Enum.GetValues<Colour>())
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            Assert.That(board.GetByPiece(new Piece(type, colour)), Is.EqualTo(testValue));

            testValue++;
        }

        var board2 = new Bitboards();
        board2.SetByPiece(new Piece(PieceType.Bishop, Colour.Black), 123ul);
        Assert.That(board2.GetByPiece(new Piece(PieceType.Bishop, Colour.Black)), Is.EqualTo(123ul));
        Assert.That(board2.GetByPiece(new Piece(PieceType.Bishop, Colour.White)), Is.EqualTo(0ul));
    }

    [Test]
    public void BoardOccupiedTest()
    {
        var board = new Bitboards();
        board.SetByPiece(new Piece(PieceType.Pawn, colour: Colour.White), 1ul);

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            if (squareIndex == 0)
                Assert.That(board.SquareOccupied(new Square(squareIndex)), Is.True);
            else
                Assert.That(board.SquareOccupied(new Square(squareIndex)), Is.False);
        }

        var allOnBoard = new Bitboards();
        foreach (Colour colour in Enum.GetValues<Colour>())
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            // fill all with on bits
            allOnBoard.SetByPiece(new Piece(type, colour), 0xffffffffffffffff);
        }

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            Assert.That(allOnBoard.SquareOccupied(new Square(squareIndex)), Is.True);
        }
    }

    [Test]
    public void SetZero()
    {
        var board = new Bitboards();
        
        // set all on
        foreach (Colour colour in Enum.GetValues<Colour>())
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            // fill all with on bits
            board.SetByPiece(new Piece(type, colour), 0xffffffffffffffff);
        }
        
        
        board.SetZero(new Square(0));
        
        for (int rank = 0; rank < 8;rank++)
        for (int file = 0; file < 8; file++)
        {
            if (rank == 0 && file == 0)
            {
                Assert.That(board.SquareOccupied(new Square(rank,file)), Is.False);
            }
            else
            {
                Assert.That(board.SquareOccupied(new Square(rank,file)), Is.True);
            }
        }
    }

    [Test]
    public void SetOn()
    {
        var board = new Bitboards();
        
        // put a pawn on a1
        board.SetOn(new Square(0), new Piece(PieceType.Pawn,Colour.White));
        
        // should be occupied
        Assert.That(board.SquareOccupied(new Square(0)), Is.True);

        // all others should not be
        for (int i = 1; i < 64; i++)
        {
            Assert.That(board.SquareOccupied(new Square(i)), Is.False);
        }
    }
}