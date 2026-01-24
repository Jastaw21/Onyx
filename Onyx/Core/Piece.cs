using System.Diagnostics.CodeAnalysis;

namespace Onyx.Core;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Piece
{
    // first 0th-3rd bits encode the type, 4th the colour
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int King = 5;
    public const int Queen = 6;
    public const int IsBlack_ = 1 << 4;

    public static sbyte MakePiece(int piece, bool isWhite) => (sbyte)(piece | (isWhite ? 0 : IsBlack_));

    public static int PieceType(sbyte piece) => piece & 0xf;
    public static int PieceTypeIndex(sbyte piece) => (piece & 0xf) - 1;
    public static bool IsWhite(sbyte piece) => (piece & IsBlack_) == 0;
    public static bool IsBlack(sbyte piece) => !IsWhite(piece);

    public static readonly sbyte WP = Pawn;
    public static readonly sbyte WN = Knight;
    public static readonly sbyte WB = Bishop;
    public static readonly sbyte WR = Rook;
    public static readonly sbyte WK = King;
    public static readonly sbyte WQ = Queen;

    public static readonly sbyte BP = Pawn | IsBlack_;
    public static readonly sbyte BN = Knight | IsBlack_;
    public static readonly sbyte BB = Bishop | IsBlack_;
    public static readonly sbyte BR = Rook | IsBlack_;
    public static readonly sbyte BK = King | IsBlack_;
    public static readonly sbyte BQ = Queen | IsBlack_;

    public static int BitboardIndex(sbyte piece)
    {
        return (piece & IsBlack_) > 0 ? PieceType(piece) - 1 + 6 : PieceType(piece) - 1;
    }

    public static readonly sbyte[] AllPieces =
    [
        WP, WB, WK, WQ, WN, WR,
        BP, BB, BK, BQ, BN, BR
    ];
    public static readonly sbyte[] _whitePieces =
    [
        WP, WB, WK, WQ, WN, WR
    ];
    public static readonly sbyte[] _blackPieces =
    [
        BP, BB, BK, BQ, BN, BR
    ];

    public static readonly sbyte[] _whitePromotionTypes = [WQ, WN, WR, WB];
    public static readonly sbyte[] _blackPromotionTypes = [BQ, BN, BR, BB];
}