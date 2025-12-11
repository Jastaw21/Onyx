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
        for (var i = 0; i < 64; i++)
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
        foreach (var colour in Enum.GetValues<Colour>())
        foreach (var type in Enum.GetValues<PieceType>())
        {
            board.SetByPiece(new Piece(type, colour), testValue);
            testValue++;
        }

        testValue = 0;

        foreach (var colour in Enum.GetValues<Colour>())
        foreach (var type in Enum.GetValues<PieceType>())
        {
            Assert.That(board.OccupancyByPiece(new Piece(type, colour)), Is.EqualTo(testValue));

            testValue++;
        }

        var board2 = new Bitboards();
        board2.SetByPiece(new Piece(PieceType.Bishop, Colour.Black), 123ul);
        Assert.That(board2.OccupancyByPiece(new Piece(PieceType.Bishop, Colour.Black)), Is.EqualTo(123ul));
        Assert.That(board2.OccupancyByPiece(new Piece(PieceType.Bishop, Colour.White)), Is.EqualTo(0ul));
    }

    [Test]
    public void BoardOccupiedTest()
    {
        var board = new Bitboards();
        board.SetByPiece(new Piece(PieceType.Pawn, colour: Colour.White), 1ul);

        for (var squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            if (squareIndex == 0)
                Assert.That(board.SquareOccupied(new Square(squareIndex)), Is.True);
            else
                Assert.That(board.SquareOccupied(new Square(squareIndex)), Is.False);
        }

        var allOnBoard = new Bitboards();
        foreach (var colour in Enum.GetValues<Colour>())
        foreach (var type in Enum.GetValues<PieceType>())
        {
            // fill all with on bits
            allOnBoard.SetByPiece(new Piece(type, colour), 0xffffffffffffffff);
        }

        for (var squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            Assert.That(allOnBoard.SquareOccupied(new Square(squareIndex)), Is.True);
        }
    }

    [Test]
    public void SetZero()
    {
        var board = new Bitboards();

        // set all on
        foreach (var colour in Enum.GetValues<Colour>())
        foreach (var type in Enum.GetValues<PieceType>())
        {
            // fill all with on bits
            board.SetByPiece(new Piece(type, colour), 0xffffffffffffffff);
        }


        board.SetAllOff(new Square(0));

        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++)
        {
            if (rank == 0 && file == 0)
            {
                Assert.That(board.SquareOccupied(new Square(rank, file)), Is.False);
            }
            else
            {
                Assert.That(board.SquareOccupied(new Square(rank, file)), Is.True);
            }
        }
    }

    [Test]
    public void SetOn()
    {
        var board = new Bitboards();

        // put a pawn on a1
        board.SetOn(new Piece(PieceType.Pawn, Colour.White), new Square(0));

        // should be occupied
        Assert.That(board.SquareOccupied(new Square(0)), Is.True);

        // all others should not be
        for (var i = 1; i < 64; i++)
        {
            Assert.That(board.SquareOccupied(new Square(i)), Is.False);
        }
    }

    [Test]
    public void MoveUCI()
    {
        var fullMove = new Move(new Piece(PieceType.Bishop, Colour.White), new Square(0, 0), new Square(7, 7));
        Assert.That(fullMove.Notation, Is.EqualTo("a1h8"));
    }

    [Test]
    public void BitBoardFromFen()
    {
        var board = new Bitboards(Fen.DefaultFen);
        Assert.That(board.SquareOccupied(new Square(0)), Is.True);

        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Pawn, Colour.White)), Is.EqualTo(0xff00));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Rook, Colour.White)), Is.EqualTo(0x81));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Knight, Colour.White)), Is.EqualTo(0x42));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Bishop, Colour.White)), Is.EqualTo(0x24));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.King, Colour.White)), Is.EqualTo(0x10));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Queen, Colour.White)), Is.EqualTo(0x8));

        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Pawn, Colour.Black)), Is.EqualTo(0xff000000000000));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Rook, Colour.Black)), Is.EqualTo(0x8100000000000000));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Bishop, Colour.Black)), Is.EqualTo(0x2400000000000000));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Knight, Colour.Black)), Is.EqualTo(0x4200000000000000));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.King, Colour.Black)), Is.EqualTo(0x1000000000000000));
        Assert.That(board.OccupancyByPiece(new Piece(PieceType.Queen, Colour.Black)), Is.EqualTo(0x800000000000000));
    }

    [Test]
    public void PieceHelpersAll()
    {
        var result = Piece.All();
        Assert.That(result.Count, Is.EqualTo(12));

        Assert.That(result, Contains.Item(new Piece(PieceType.Pawn, Colour.White)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Rook, Colour.White)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Bishop, Colour.White)));
        Assert.That(result, Contains.Item(new Piece(PieceType.King, Colour.White)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Queen, Colour.White)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Knight, Colour.White)));

        Assert.That(result, Contains.Item(new Piece(PieceType.Pawn, Colour.Black)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Rook, Colour.Black)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Bishop, Colour.Black)));
        Assert.That(result, Contains.Item(new Piece(PieceType.King, Colour.Black)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Queen, Colour.Black)));
        Assert.That(result, Contains.Item(new Piece(PieceType.Knight, Colour.Black)));
    }

    [Test]
    public void BitBoardPiece()
    {
        var board = new Bitboards(Fen.DefaultFen);

        Assert.That(board.PieceAtSquare(new Square(1, 0)), Is.EqualTo(new Piece(PieceType.Pawn, Colour.White)));
        Assert.That(board.PieceAtSquare(new Square(6, 7)), Is.EqualTo(new Piece(PieceType.Pawn, Colour.Black)));
        Assert.That(board.PieceAtSquare(new Square(3, 3)).HasValue, Is.False);
    }

    [Test]
    public void CharToFen()
    {
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Pawn, Colour.White)), Is.EqualTo('P'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Rook, Colour.White)), Is.EqualTo('R'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Queen, Colour.White)), Is.EqualTo('Q'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.King, Colour.White)), Is.EqualTo('K'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Knight, Colour.White)), Is.EqualTo('N'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Bishop, Colour.White)), Is.EqualTo('B'));

        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Pawn, Colour.Black)), Is.EqualTo('p'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Rook, Colour.Black)), Is.EqualTo('r'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Queen, Colour.Black)), Is.EqualTo('q'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.King, Colour.Black)), Is.EqualTo('k'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Knight, Colour.Black)), Is.EqualTo('n'));
        Assert.That(Fen.GetCharFromPiece(new Piece(PieceType.Bishop, Colour.Black)), Is.EqualTo('b'));
    }

    [Test]
    public void GetFenFromBitboard()
    {
        var board = new Bitboards(Fen.DefaultFen);
        Assert.That(board.GetFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"));

        board.LoadFen(Fen.KiwiPeteFen);
        Assert.That(board.GetFen(), Is.EqualTo("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R"));
    }

    [Test]
    public void SquareFromFen()
    {
        Assert.That(new Square("a1").Notation, Is.EqualTo("a1"));
        Assert.That(new Square("c6").Notation, Is.EqualTo("c6"));
    }

    [Test]
    public void MoveFromNotation()
    {
        var move = new Move(Piece.MakePiece(PieceType.Pawn, Colour.White), "a2a4");
        Assert.Multiple(() =>
            {
                Assert.That(move.PieceMoved, Is.EqualTo(Piece.MakePiece(PieceType.Pawn, Colour.White)));
                Assert.That(move.From.FileIndex, Is.EqualTo(0));
                Assert.That(move.From.RankIndex, Is.EqualTo(1));

                Assert.That(move.To.FileIndex, Is.EqualTo(0));
                Assert.That(move.To.RankIndex, Is.EqualTo(3));
            }
        );
    }

    [Test]
    public void MoveInitPromotionInferred()
    {
        var testMove = new Move(Piece.MakePiece(PieceType.Pawn, Colour.White), "g7g8q");
        Assert.That(testMove.PromotedPiece is { Type: PieceType.Queen, Colour: Colour.White });
    }

    [Test]
    public void MoveString()
    {
        Assert.That(Piece.MakePiece(PieceType.King, Colour.White).ToString(), Is.EqualTo("White King"));
    }
}

class HelperTests
{
    [Test]
    public void RankAndFileHelpersTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFileHelpers.FileIndex(0), Is.EqualTo(0));
            Assert.That(RankAndFileHelpers.FileIndex(1), Is.EqualTo(1));
            Assert.That(RankAndFileHelpers.FileIndex(2), Is.EqualTo(2));
            Assert.That(RankAndFileHelpers.FileIndex(3), Is.EqualTo(3));
            Assert.That(RankAndFileHelpers.FileIndex(4), Is.EqualTo(4));
            Assert.That(RankAndFileHelpers.FileIndex(5), Is.EqualTo(5));
            Assert.That(RankAndFileHelpers.FileIndex(6), Is.EqualTo(6));
            Assert.That(RankAndFileHelpers.FileIndex(7), Is.EqualTo(7));
        });
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFileHelpers.RankIndex(0), Is.EqualTo(0));
            Assert.That(RankAndFileHelpers.RankIndex(8), Is.EqualTo(1));
            Assert.That(RankAndFileHelpers.RankIndex(16), Is.EqualTo(2));
            Assert.That(RankAndFileHelpers.RankIndex(24), Is.EqualTo(3));
            Assert.That(RankAndFileHelpers.RankIndex(32), Is.EqualTo(4));
            Assert.That(RankAndFileHelpers.RankIndex(40), Is.EqualTo(5));
            Assert.That(RankAndFileHelpers.RankIndex(48), Is.EqualTo(6));
            Assert.That(RankAndFileHelpers.RankIndex(56), Is.EqualTo(7));
        });
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFileHelpers.SquareIndex(0, 0), Is.EqualTo(0));
            Assert.That(RankAndFileHelpers.SquareIndex(1, 0), Is.EqualTo(8));
            Assert.That(RankAndFileHelpers.SquareIndex(1, 7), Is.EqualTo(15));
            Assert.That(RankAndFileHelpers.SquareIndex(2, 1), Is.EqualTo(17));
        });
    }

    [Test]
    public void SquareToBitBoard()
    {
        ulong targetBitboard = 1;
        for (int square = 0; square < 64; square++)
        {
            var s = new Square(square);
            Assert.That(s.Bitboard, Is.EqualTo(targetBitboard));
            
            
            if (square == 63) 
                Assert.That(s.Bitboard == 0x8000000000000000);
            
            targetBitboard <<= 1;
        }
    }
    
}