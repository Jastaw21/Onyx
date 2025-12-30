namespace Onyx.Core;

public static class PreMoveFlags
{
    public static readonly int NoFlag = 0;
    public static readonly int EnPassant = 1 << 0;
    public static readonly int Promotion = 1 << 1;
    public static readonly int Castle = 1 << 2;
}

public static class PostMoveFlags
{
    public static readonly int NoFlag = 0;
    public static readonly int Capture = 0;
}

public static class MoveFlags
{
    public static readonly uint ToSquareBits = 0x3f;
    public static readonly uint FromSquareBits = 0xfc0;
    public static readonly uint Promotion = 1 << 14;
    public static readonly uint EnPassant = 2 << 14;
    public static readonly uint Castling = 3 << 14;
}

// 0000  000000 000000
//       from   to  

public struct Move
{
    public Move(Piece pieceMoved, Square from, Square to)
    {
        PieceMoved = pieceMoved;
        Data |= (uint)from.SquareIndex << 6;
        Data |= (uint)to.SquareIndex;
    }

    public Move(Piece pieceMoved, int from, int to)
    {
        PieceMoved = pieceMoved;
        Data |= (uint)from << 6;
        Data |= (uint)to;
    }


    public Move(Piece pieceMoved, string notation)
    {
        PieceMoved = pieceMoved;
        var fromSquare = notation[..2];
        var toSquare = notation.Length == 4 ? notation[^2..] : notation[2..5];
        var fromIndex = RankAndFileHelpers.SquareIndex(fromSquare);
        var toIndex = RankAndFileHelpers.SquareIndex(toSquare);
        if (notation.Length == 5)
        {
            PromotedPiece = notation[4] switch
            {
                'q' => Piece.MakePiece(PieceType.Queen, pieceMoved.Colour),
                'b' => Piece.MakePiece(PieceType.Bishop, pieceMoved.Colour),
                'n' => Piece.MakePiece(PieceType.Knight, pieceMoved.Colour),
                'r' => Piece.MakePiece(PieceType.Rook, pieceMoved.Colour),
                _ => PromotedPiece
            };
        }


        Data |= (uint)fromIndex << 6;
        Data |= (uint)toIndex;
    }

    public Piece PieceMoved { get; }

    public int To => (int)(Data & MoveFlags.ToSquareBits);
    public int From => (int)((Data & MoveFlags.FromSquareBits) >> 6);

    public Piece? PromotedPiece = null;
    public int PreMoveFlag = PreMoveFlags.NoFlag;
    public int PostMoveFlag = PostMoveFlags.NoFlag;
    private uint Data = 0;

    public string Notation
    {
        get
        {
            var fromNotation = RankAndFileHelpers.Notation(From);
            var toNotation = RankAndFileHelpers.Notation(To);
            var isPromotion = PromotedPiece.HasValue;
            if (isPromotion)
                return $"{fromNotation}{toNotation}{Fen.GetCharFromPiece(PromotedPiece.Value)}";
            return $"{fromNotation}{toNotation}";
        }
    }


    public bool IsPromotion => (PreMoveFlag & PreMoveFlags.Promotion) > 0;
    public bool IsCastling => (PreMoveFlag & PreMoveFlags.Castle) > 0;
    public bool IsEnPassant => (PreMoveFlag & PreMoveFlags.EnPassant) > 0;

    public override string ToString()
    {
        return Notation;
    }
}