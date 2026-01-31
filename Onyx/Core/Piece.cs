using System.Diagnostics.CodeAnalysis;

namespace Onyx.Core;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PieceTypes
{
    // first 0th-3rd bits encode the type, 4th the colour
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int King = 5;
    public const int Queen = 6;
    public const int IsBlack_ = 1 << 3;

    public static byte MakePiece(int piece, bool isWhite) => (byte)(piece | (isWhite ? 0 : IsBlack_));

    public static int PieceType(byte piece) => piece & 0x7;
    public static int PieceTypeIndex(byte piece) => (piece & 0x7) - 1;
    public static bool IsWhite(byte piece) => (piece & IsBlack_) == 0;
    public static bool IsBlack(byte piece) => !IsWhite(piece);

    public static readonly byte WP = Pawn;
    public static readonly byte WN = Knight;
    public static readonly byte WB = Bishop;
    public static readonly byte WR = Rook;
    public static readonly byte WK = King;
    public static readonly byte WQ = Queen;

    public static readonly byte BP = Pawn | IsBlack_;
    public static readonly byte BN = Knight | IsBlack_;
    public static readonly byte BB = Bishop | IsBlack_;
    public static readonly byte BR = Rook | IsBlack_;
    public static readonly byte BK = King | IsBlack_;
    public static readonly byte BQ = Queen | IsBlack_;

    public static int BitboardIndex(byte piece)
    {
        return (piece & IsBlack_) > 0 ? PieceType(piece) - 1 + 6 : PieceType(piece) - 1;
    }

    public static readonly byte[] AllPieces =
    [
        WP, WB, WK, WQ, WN, WR,
        BP, BB, BK, BQ, BN, BR
    ];
    public static readonly byte[] _whitePieces =
    [
        WP, WB, WK, WQ, WN, WR
    ];
    public static readonly byte[] _blackPieces =
    [
        BP, BB, BK, BQ, BN, BR
    ];

    public static readonly byte[] _whitePromotionTypes = [WQ, WN, WR, WB];
    public static readonly byte[] _blackPromotionTypes = [BQ, BN, BR, BB];
}

public struct Piece
{
    public byte Value { get; } = 0;
    public Piece(byte piece) => Value = piece;
    public string Notation => Fen.GetCharFromPiece(Value).ToString();
    public override string ToString() => Notation;
    public static Piece WP => new(PieceTypes.WP);
    public static Piece WN => new(PieceTypes.WN);
    public static Piece WB => new(PieceTypes.WB);
    public static Piece WR => new(PieceTypes.WR);
    public static Piece WK => new(PieceTypes.WK);
    public static Piece WQ => new(PieceTypes.WQ);
    public static Piece BP => new(PieceTypes.BP);
    public static Piece BN => new(PieceTypes.BN);
    public static Piece BB => new(PieceTypes.BB);
    public static Piece BR => new(PieceTypes.BR);
    public static Piece BK => new(PieceTypes.BK);
    public static Piece BQ => new(PieceTypes.BQ);
}