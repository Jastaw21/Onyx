using Onyx.Core;

namespace OnyxTests;

public class CoreTests
{

    [Test]
    public void BoardInit()
    {
        var board = new Bitboards();

        ulong testValue = 0;
        foreach (var piece in Pc.AllPieces)
        {
            board.SetByPiece(piece, testValue);
            testValue++;
        }

        testValue = 0;

        foreach (var piece in Pc.AllPieces)
        {
            Assert.That(board.OccupancyByPiece(piece), Is.EqualTo(testValue));

            testValue++;
        }

        var board2 = new Bitboards();
        board2.SetByPiece(Pc.MakePiece(Pc.Bishop,true), 123ul);
        Assert.Multiple(() =>
        {
            Assert.That(board2.OccupancyByPiece(Pc.MakePiece(Pc.Bishop,true)), Is.EqualTo(123ul));
            Assert.That(board2.OccupancyByPiece(Pc.MakePiece(Pc.Bishop,false)), Is.EqualTo(0ul));
        });
    }

    [Test]
    public void BoardOccupiedTest()
    {
        var board = new Bitboards();
        board.SetByPiece(Pc.MakePiece(Pc.Pawn, false), 1ul);

        for (var squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            if (squareIndex == 0)
                Assert.That(board.SquareOccupied( squareIndex), Is.True);
            else
                Assert.That(board.SquareOccupied( squareIndex), Is.False);
        }

        var allOnBoard = new Bitboards();
        foreach (var piece in Pc.AllPieces)
        {
            // fill all with on bits
            allOnBoard.SetByPiece(piece, 0xffffffffffffffff);
        }

        for (var squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            Assert.That(allOnBoard.SquareOccupied( squareIndex), Is.True);
        }
    }

    [Test]
    public void SetZero()
    {
        var board = new Bitboards();

        // set all on
        foreach (var piece in Pc.AllPieces)
        {
            // fill all with on bits
            board.SetByPiece(piece, 0xffffffffffffffff);
        }


        board.SetAllOff( 0);

        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++)
        {
            if (rank == 0 && file == 0)
            {
                Assert.That(board.SquareOccupied( RankAndFile.SquareIndex(rank, file)), Is.False);
            }
            else
            {
                Assert.That(board.SquareOccupied( RankAndFile.SquareIndex(rank, file)), Is.True);
            }
        }
    }

    [Test]
    public void SetOn()
    {
        var board = new Bitboards();

        // put a pawn on a1
        board.SetOn(Pc.MakePiece(Pc.Pawn, false),  0);

        // should be occupied
        Assert.That(board.SquareOccupied( 0), Is.True);

        // all others should not be
        for (var i = 1; i < 64; i++)
        {
            Assert.That(board.SquareOccupied( i), Is.False);
        }
    }

    [Test]
    public void MoveUCI()
    {
        var fullMove = new Move(Pc.MakePiece(Pc.Bishop, false), RankAndFile.SquareIndex(0, 0), RankAndFile.SquareIndex(7, 7));
        Assert.That(fullMove.Notation, Is.EqualTo("a1h8"));
    }

    [Test]
    public void BitBoardFromFen()
    {
        var board = new Bitboards(Fen.DefaultFen);
        Assert.Multiple(() =>
        {
            Assert.That(board.SquareOccupied( 0), Is.True);

            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Pawn, false)), Is.EqualTo(0xff00));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Rook, false)), Is.EqualTo(0x81));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Knight, false)), Is.EqualTo(0x42));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Bishop, false)), Is.EqualTo(0x24));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.King, false)), Is.EqualTo(0x10));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Queen, false)), Is.EqualTo(0x8));

            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Pawn, true)), Is.EqualTo(0xff000000000000));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Rook, true)), Is.EqualTo(0x8100000000000000));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Bishop, true)), Is.EqualTo(0x2400000000000000));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Knight, true)), Is.EqualTo(0x4200000000000000));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.King, true)), Is.EqualTo(0x1000000000000000));
            Assert.That(board.OccupancyByPiece(Pc.MakePiece(Pc.Queen, true)), Is.EqualTo(0x800000000000000));
        });
    }

    [Test]
    public void PieceHelpersAll()
    {
        var result = Pc.AllPieces;
        Assert.That(result.Count, Is.EqualTo(12));

        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Pawn, false)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Rook, false)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Bishop, false)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.King, false)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Queen, false)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Knight, false)));

        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Pawn, true)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Rook, true)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Bishop, true)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.King, true)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Queen, true)));
        Assert.That(result, Contains.Item(Pc.MakePiece(Pc.Knight, true)));
    }

    [Test]
    public void BitBoardPiece()
    {
        var board = new Bitboards(Fen.DefaultFen);

        Assert.Multiple(() =>
        {
            Assert.That(board.PieceAtSquare(RankAndFile.SquareIndex(1, 0)), Is.EqualTo(Pc.MakePiece(Pc.Pawn, false)));
            Assert.That(board.PieceAtSquare(RankAndFile.SquareIndex(6, 7)), Is.EqualTo(Pc.MakePiece(Pc.Pawn, true)));
            Assert.That(board.PieceAtSquare(RankAndFile.SquareIndex(3, 3)).HasValue, Is.False);
        });
    }

    [Test]
    public void CharToFen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Fen.GetCharFromPiece( Pc.MakePiece(Pc.Pawn, false)), Is.EqualTo('P'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Rook, false)), Is.EqualTo('R'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Queen, false)), Is.EqualTo('Q'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.King, false)), Is.EqualTo('K'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Knight, false)), Is.EqualTo('N'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Bishop, false)), Is.EqualTo('B'));

            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Pawn, true)), Is.EqualTo('p'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Rook, true)), Is.EqualTo('r'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Queen, true)), Is.EqualTo('q'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.King, true)), Is.EqualTo('k'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Knight, true)), Is.EqualTo('n'));
            Assert.That(Fen.GetCharFromPiece(Pc.MakePiece(Pc.Bishop, true)), Is.EqualTo('b'));
        });
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
    public void MoveFromNotation()
    {
        var move = new Move(Pc.MakePiece(Pc.Pawn, false), "a2a4");
        Assert.Multiple(() =>
            {
                Assert.That(move.PieceMoved, Is.EqualTo(Pc.MakePiece(Pc.Pawn, false)));
                Assert.That(RankAndFile.FileIndex(move.From), Is.EqualTo(0));
                Assert.That(RankAndFile.RankIndex(move.From), Is.EqualTo(1));

                Assert.That(RankAndFile.FileIndex(move.To), Is.EqualTo(0));
                Assert.That(RankAndFile.RankIndex(move.To), Is.EqualTo(3));
            }
        );
    }

    [Test]
    public void MoveInitPromotionInferred()
    {
        var testMove = new Move(Pc.MakePiece(Pc.Pawn, true), "g7g8q");
        Assert.That(testMove.PromotedPiece, Is.EqualTo(Pc.MakePiece(Pc.Queen, false)));
    }

    [Test]
    public void PieceToString()
    {
        Assert.That(Pc.MakePiece(Pc.King, false).ToString(), Is.EqualTo("White King"));
    }

    [Test]
    public void MoveToString()
    {
        var move = new Move(Pc.BB, "a1b2");
        Assert.That(move.ToString(), Is.EqualTo("a1b2"));

        var promotionMove = new Move(Pc.WP, "a7a8");
        promotionMove.PromotedPiece = Pc.WQ;
        Assert.That(promotionMove.ToString(), Is.EqualTo("a7a8Q"));
    }
}

class HelperTests
{
    [Test]
    public void RankAndFileHelpersTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFile.FileIndex(0), Is.EqualTo(0));
            Assert.That(RankAndFile.FileIndex(1), Is.EqualTo(1));
            Assert.That(RankAndFile.FileIndex(2), Is.EqualTo(2));
            Assert.That(RankAndFile.FileIndex(3), Is.EqualTo(3));
            Assert.That(RankAndFile.FileIndex(4), Is.EqualTo(4));
            Assert.That(RankAndFile.FileIndex(5), Is.EqualTo(5));
            Assert.That(RankAndFile.FileIndex(6), Is.EqualTo(6));
            Assert.That(RankAndFile.FileIndex(7), Is.EqualTo(7));
        });
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFile.RankIndex(0), Is.EqualTo(0));
            Assert.That(RankAndFile.RankIndex(8), Is.EqualTo(1));
            Assert.That(RankAndFile.RankIndex(16), Is.EqualTo(2));
            Assert.That(RankAndFile.RankIndex(24), Is.EqualTo(3));
            Assert.That(RankAndFile.RankIndex(32), Is.EqualTo(4));
            Assert.That(RankAndFile.RankIndex(40), Is.EqualTo(5));
            Assert.That(RankAndFile.RankIndex(48), Is.EqualTo(6));
            Assert.That(RankAndFile.RankIndex(56), Is.EqualTo(7));
        });
        Assert.Multiple(() =>
        {
            Assert.That(RankAndFile.SquareIndex(0, 0), Is.EqualTo(0));
            Assert.That(RankAndFile.SquareIndex(1, 0), Is.EqualTo(8));
            Assert.That(RankAndFile.SquareIndex(1, 7), Is.EqualTo(15));
            Assert.That(RankAndFile.SquareIndex(2, 1), Is.EqualTo(17));
        });
    }

   

    [Test]
    public void RayBetween()
    {
        var a1 = RankAndFile.SquareIndex("a1");
        var h8 = RankAndFile.SquareIndex("h8");
        var c3 = RankAndFile.SquareIndex("c3");
        var d1 = RankAndFile.SquareIndex("d1");
        var h1 = RankAndFile.SquareIndex("h1");
        var a8 = RankAndFile.SquareIndex("a8");
        Assert.Multiple(() =>
        {
            // easy diagonal
            Assert.That(RankAndFile.GetRayBetween(a1, h8), Is.EqualTo(0x8040201008040201));
            Assert.That(RankAndFile.GetRayBetween(h8, a1), Is.EqualTo(0x8040201008040201));
            Assert.That(RankAndFile.GetRayBetween(a1, c3), Is.EqualTo(0x40201));
            Assert.That(RankAndFile.GetRayBetween(c3, a1), Is.EqualTo(0x40201));
            
            // backwards diagonal
            Assert.That(RankAndFile.GetRayBetween(h8, c3), Is.EqualTo(0x8040201008040000));
            
            // not on a line
            Assert.That(RankAndFile.GetRayBetween(d1, c3), Is.EqualTo(0));
            Assert.That(RankAndFile.GetRayBetween(c3, d1), Is.EqualTo(0));
            Assert.That(RankAndFile.GetRayBetween(d1, h8), Is.EqualTo(0));
            Assert.That(RankAndFile.GetRayBetween(h8, d1), Is.EqualTo(0));
            
            // backwards
            Assert.That(RankAndFile.GetRayBetween(a8, h1), Is.EqualTo(0x102040810204080));
            Assert.That(RankAndFile.GetRayBetween(h1, a8), Is.EqualTo(0x102040810204080));
            
            // straight line
            Assert.That(RankAndFile.GetRayBetween(a1, a8), Is.EqualTo(0x101010101010101));
            Assert.That(RankAndFile.GetRayBetween(a8, a1), Is.EqualTo(0x101010101010101));
            Assert.That(RankAndFile.GetRayBetween(a1, h1), Is.EqualTo(0xff));
            Assert.That(RankAndFile.GetRayBetween(h1, a1), Is.EqualTo(0xff));
        });

    }
    
}