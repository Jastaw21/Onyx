namespace Onyx.Core;

public static class MoveFlags
{
    public static readonly uint ToSquareBits = 0x3f;
    public static readonly uint FromSquareBits = 0xfc0;
    public static readonly uint Promotion = 1 << 14;
    public static readonly uint EnPassant = 2 << 14;
    public static readonly uint Castling = 3 << 14;
}

// 00   00       000000 000000
//      special  from   to  

public struct Move
{
    public Move(sbyte pieceMoved, int from, int to)
    {
        PieceMoved = pieceMoved;
        Data |= (uint)from << 6;
        Data |= (uint)to;
    }


    public Move(sbyte pieceMoved, string notation)
    {
        PieceMoved = pieceMoved;
        var fromSquare = notation[..2];
        var toSquare = notation.Length == 4 ? notation[^2..] : notation[2..4];
        var fromIndex = RankAndFile.SquareIndex(fromSquare);
        var toIndex = RankAndFile.SquareIndex(toSquare);
        if (notation.Length == 5)
        {
            PromotedPiece = char.ToLower(notation[4]) switch
            {
                'q' => Piece.MakePiece(Piece.Queen, Piece.IsWhite(PieceMoved)),
                'b' => Piece.MakePiece(Piece.Bishop, Piece.IsWhite(PieceMoved)),
                'n' => Piece.MakePiece(Piece.Knight, Piece.IsWhite(PieceMoved)),
                'r' => Piece.MakePiece(Piece.Rook, Piece.IsWhite(PieceMoved)),
                _ => PromotedPiece
            };
        }
        Data |= (uint)fromIndex << 6;
        Data |= (uint)toIndex;
    }

    public sbyte PieceMoved { get; }

    public int To => (int)(Data & MoveFlags.ToSquareBits);
    public int From => (int)((Data & MoveFlags.FromSquareBits) >> 6);

    public sbyte? PromotedPiece = null;
    public uint Data { get; set; } = 0;

    public string Notation
    {
        get
        {
            var fromNotation = RankAndFile.Notation(From);
            var toNotation = RankAndFile.Notation(To);
            var isPromotion = PromotedPiece.HasValue;
            if (isPromotion)
                return $"{fromNotation}{toNotation}{Fen.GetCharFromPiece(PromotedPiece!.Value)}";
            return $"{fromNotation}{toNotation}";
        }
    }


    public bool IsPromotion
    {
        get => (Data &(3<<14)) == MoveFlags.Promotion;
        set => Data |= MoveFlags.Promotion;
    }
    public bool IsCastling
    {
        get => (Data &(3<<14)) ==  MoveFlags.Castling;
        set => Data |= MoveFlags.Castling;
    }


    public bool IsEnPassant
    {
        get => (Data &(3<<14)) ==   MoveFlags.EnPassant;
        set => Data |= MoveFlags.EnPassant;
    }

    public override string ToString()
    {
        return Notation;
    }
}