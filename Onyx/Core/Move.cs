namespace Onyx.Core;

public static class MoveFlags
{
    public static readonly uint ToSquareBits = 0x3f;
    public static readonly uint FromSquareBits = 0xfc0;
    public static readonly uint CapturedPieceBits = 0xf0000;
    public static readonly uint Promotion = 1 << 14;
    public static readonly uint EnPassant = 2 << 14;
    public static readonly uint Castling = 3 << 14;
}

// Bits 0: to square.
// 6-11: from square.
// 14-15: special move flag.
// 16-19: encode captured piece.
// 31 - has capture been checked
public struct Move : IEquatable<Move>
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
    public bool HasCaptureBeenChecked
    {
        get => (Data & 0x80000000) != 0;
        set => Data |= value ? 0x80000000 : 0;
    }
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
                return $"{fromNotation}{toNotation}{char.ToLower(Fen.GetCharFromPiece(PromotedPiece!.Value))}";
            return $"{fromNotation}{toNotation}";
        }
    }

    public sbyte CapturedPiece
    {
        get => (sbyte)((Data & MoveFlags.CapturedPieceBits) >> 16);
        set
        {
            Data &= ~MoveFlags.CapturedPieceBits;
            Data |= (uint)(value << 16);
            Data |= 0x80000000;
        }
    }

    public bool IsPromotion
    {
        get => (Data & (3 << 14)) == MoveFlags.Promotion;
        set => Data |= MoveFlags.Promotion;
    }
    public bool IsCastling
    {
        get => (Data & (3 << 14)) == MoveFlags.Castling;
        set => Data |= MoveFlags.Castling;
    }


    public bool IsEnPassant
    {
        get => (Data & (3 << 14)) == MoveFlags.EnPassant;
        set => Data |= MoveFlags.EnPassant;
    }

    public bool Equals(Move other)
    {
        // Functional equality: from, to, piece moved, promoted piece, and move type flags.
        // We ignore internal state: HasCaptureBeenChecked and CapturedPiece bits.
        
        // Mask for From (6-11), To (0-5), and Special Flags (14-15)
        const uint functionalMask = 0xFC0 | 0x3F | (3 << 14);
        
        return PieceMoved == other.PieceMoved &&
               (Data & functionalMask) == (other.Data & functionalMask) &&
               PromotedPiece == other.PromotedPiece;
    }

    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    public override int GetHashCode()
    {
        const uint functionalMask = 0xFC0 | 0x3F | (3 << 14);
        return HashCode.Combine(PieceMoved, Data & functionalMask, PromotedPiece);
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);

    public override string ToString()
    {
        return Notation;
    }
}