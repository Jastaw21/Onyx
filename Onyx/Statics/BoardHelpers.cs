using Onyx.Core;

namespace Onyx.Statics;

public static class BoardHelpers
{
    public static ulong GetFileFromIndex(int index)
    {
        return index switch
        {
            0 => FileA,
            1 => FileB,
            2 => FileC,
            3 => FileD,
            4 => FileE,
            5 => FileF,
            6 => FileG,
            7 => FileH,
            _ => 0
        };
    }

    public static ulong GetRankFromIndex(int index)
    {
        return index switch
        {
            0 => Rank1,
            1 => Rank2,
            2 => Rank3,
            3 => Rank4,
            4 => Rank5,
            5 => Rank6,
            6 => Rank7,
            7 => Rank8,
            _ => 0
        };
    }

    public static ulong NeighbouringFiles(int square)
    {
        var thisFile = RankAndFile.FileIndex(square);
        var result = GetFileFromIndex(thisFile);

        if (thisFile > 0) result |= GetFileFromIndex(thisFile - 1);
        if (thisFile < 7) result |= GetFileFromIndex(thisFile + 1);

        return result;
    }

    public static ulong NeighbouringRanks(int square)
    {
        var thisRank = RankAndFile.RankIndex(square);
        var result = GetRankFromIndex(thisRank);
        if (thisRank >0) result |= GetRankFromIndex(thisRank - 1);
        if (thisRank < 7) result |= GetRankFromIndex(thisRank + 1);
        return result;
    }

    public static bool FileIsOpen(int file, ulong pawns)
    {
        return (pawns & GetFileFromIndex(file)) == 0;
    }

    public const int WhiteKingsideCastlingFlag = 1 << 0;
    public const int WhiteQueensideCastlingFlag = 1 << 1;
    public const int BlackKingsideCastlingFlag = 1 << 2;
    public const int BlackQueensideCastlingFlag = 1 << 3;
    public const ulong WhiteKingSideCastlingSquares = 0x60;
    public const ulong BlackKingSideCastlingSquares = 0x6000000000000000;
    public const ulong WhiteQueenSideCastlingSquares = 0xe;
    public const ulong BlackQueenSideCastlingSquares = 0xe00000000000000;
    public const int A1 = 0;
    public const int H1 = 7;
    public const int A8 = 56;
    public const int H8 = 63;
    public const int E1 = 4;
    public const int E8 = 60;
    public const int G1 = 6;
    public const int G8 = 62;
    public const int C1 = 2;
    public const int C8 = 58;
    public const int B8 = 57;
    public const int B1 = 1;
    public const ulong Rank1 = 0xff;
    public const ulong Rank2 = 0xff00;
    public const ulong Rank3 = 0xff0000;
    public const ulong Rank4 = 0xff000000;
    public const ulong Rank5 = 0xff00000000;
    public const ulong Rank6 = 0xff0000000000;
    public const ulong Rank7 = 0xff000000000000;
    public const ulong Rank8 = 0xff00000000000000;
    public const ulong FileA = 0x101010101010101;
    public const ulong FileB = 0x202020202020202;
    public const ulong FileC = 0x404040404040404;
    public const ulong FileD = 0x808080808080808;
    public const ulong FileE = 0x1010101010101010;
    public const ulong FileF = 0x2020202020202020;
    public const ulong FileG = 0x4040404040404040;
    public const ulong FileH = 0x8080808080808080;
    public static readonly int[][] KnightMoves =
        [[2, -1], [2, 1], [1, -2], [1, 2], [-1, -2], [-1, 2], [-2, -1], [-2, 1]];
    public static readonly int[][] KingMoves =
        [[1, 1], [1, 0], [1, -1], [0, 1], [0, -1], [-1, 1], [-1, 0], [-1, -1]];
}