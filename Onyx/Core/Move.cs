namespace Onyx.Core;

public static class MoveFlags
{
    public static readonly uint ToMask = 0x3f; // 0-5
    public static readonly int ToShift = 0;

    public static readonly uint FromMask = 0xfc0; // 6-11
    public static readonly int FromShift = 6;

    public static readonly uint PieceMovedMask = 0xf000; // 12-15
    public static readonly int PieceMovedShift = 12;

    public static readonly uint CaptureMask = 0xf0000; // 16-19
    public static readonly int CaptureShift = 16;

    public static readonly uint PromotedPieceMask = 0xf00000; // 20-23
    public static readonly int PromotedPieceShift = 20;

    public static readonly int SpecialFlagsShift = 24;
    public static readonly uint Promotion = 1u << SpecialFlagsShift; // 24-25
    public static readonly uint EnPassant = 2u << SpecialFlagsShift;
    public static readonly uint Castling = 3u << SpecialFlagsShift;

    public static readonly uint CaptureCheckedMask = 0x4000000; // 26
    public static readonly int CaptureCheckedShift = 26;

    public static uint FunctionalMask => ToMask | FromMask | PieceMovedMask | CaptureMask | PromotedPieceMask;
}

// Bits 0-5: to square.
// 6-11: from square.
// 12-15 moved piece
// 16-19 captured piece
// 20-23 promotion piece
// 24-25 special move flag
// 26 capture checked
// 27 all move flags scanned
// 28-31 spare

public struct Move : IEquatable<Move>
{
    public uint Data { get; set; } = 0;

    public Move(byte pieceMoved, int from, int to)
    {
        Data |= (uint)(from << MoveFlags.FromShift);
        Data |= (uint)(to << MoveFlags.ToShift);
        Data |= (uint)(pieceMoved << MoveFlags.PieceMovedShift);
    }

    public Move(byte pieceMoved, string notation)
    {
        var fromSquare = notation[..2];
        var toSquare = notation.Length == 4 ? notation[^2..] : notation[2..4];
        var from = RankAndFile.SquareIndex(fromSquare);
        var to = RankAndFile.SquareIndex(toSquare);
        
        Data |= (uint)(from << MoveFlags.FromShift);
        Data |= (uint)(to << MoveFlags.ToShift);
        Data |= (uint)(pieceMoved << MoveFlags.PieceMovedShift);
        
        if (notation.Length == 5)
        {
            var promotedPiece = char.ToLower(notation[4]) switch
            {
                'q' => PieceTypes.MakePiece(PieceTypes.Queen, PieceTypes.IsWhite(PieceMoved)),
                'b' => PieceTypes.MakePiece(PieceTypes.Bishop, PieceTypes.IsWhite(PieceMoved)),
                'n' => PieceTypes.MakePiece(PieceTypes.Knight, PieceTypes.IsWhite(PieceMoved)),
                'r' => PieceTypes.MakePiece(PieceTypes.Rook, PieceTypes.IsWhite(PieceMoved))
            };
            Data |= (uint)(promotedPiece << MoveFlags.PromotedPieceShift);
        }

        
    }

    public byte PieceMoved => (byte)((Data & MoveFlags.PieceMovedMask) >> MoveFlags.PieceMovedShift);
    public int To => (int)((Data & MoveFlags.ToMask) >> MoveFlags.ToShift);
    public int From => (int)((Data & MoveFlags.FromMask) >> MoveFlags.FromShift);
    public byte PromotedPiece
    {
        get => (byte)((Data & MoveFlags.PromotedPieceMask) >> MoveFlags.PromotedPieceShift);
        set => Data |= (uint)(value << MoveFlags.PromotedPieceShift);
    }
    public byte CapturedPiece
    {
        get => (byte)((Data & MoveFlags.CaptureMask) >> MoveFlags.CaptureShift);
        set
        {
            Data &= ~MoveFlags.CaptureMask;
            Data |= (uint)(value << MoveFlags.CaptureShift);
            Data |= MoveFlags.CaptureCheckedMask;
        }
    }
    public bool HasCaptureBeenChecked
    {
        get => (Data & MoveFlags.CaptureCheckedMask) != 0;
        set => Data |= value ? MoveFlags.CaptureCheckedMask : 0;
    }

    public string Notation
    {
        get
        {
            var fromNotation = RankAndFile.Notation(From);
            var toNotation = RankAndFile.Notation(To);
            var isPromotion = PromotedPiece != 0;
            if (isPromotion)
                return $"{fromNotation}{toNotation}{char.ToLower(Fen.GetCharFromPiece(PromotedPiece))}";
            return $"{fromNotation}{toNotation}";
        }
    }


    public bool IsPromotion
    {
        get => (Data & (3 << MoveFlags.SpecialFlagsShift)) == MoveFlags.Promotion;
        set => Data |= MoveFlags.Promotion;
    }
    public bool IsCastling
    {
        get => (Data & (3 << MoveFlags.SpecialFlagsShift)) == MoveFlags.Castling;
        set => Data |= MoveFlags.Castling;
    }
    public bool IsEnPassant
    {
        get => (Data & (3 << MoveFlags.SpecialFlagsShift)) == MoveFlags.EnPassant;
        set => Data |= MoveFlags.EnPassant;
    }

    public bool Equals(Move other)
    {
        return (Data & MoveFlags.FunctionalMask) == (other.Data & MoveFlags.FunctionalMask);
    }

    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(Data & MoveFlags.FunctionalMask);
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);

    public override string ToString()
    {
        return Notation;
    }
}