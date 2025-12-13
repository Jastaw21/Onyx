using Onyx.Core;

namespace Onyx.MagicBitboards;

file struct MoveDir(int deltaRank, int deltaFile)
{
    public readonly int DeltaRank = deltaRank;
    public readonly int DeltaFile = deltaFile;

    public static MoveDir[] All()
    {
        return
        [
            new MoveDir(1, 1),
            new MoveDir(1, -1),
            new MoveDir(-1, -1),
            new MoveDir(-1, 1)
        ];
    }
}

public static class MaskGenerator
{
    public static ulong GenerateStraightMoves(int square)
    {
        var result = 0ul;

        var rank = RankAndFileHelpers.RankIndex(square);
        var file = RankAndFileHelpers.FileIndex(square);

        // ignore the borders
        for (var r = 1; r < 7; r++)
        {
            if (r != rank)
                result |= 1ul << RankAndFileHelpers.SquareIndex(r, file);
        }

        for (var f = 1; f < 7; f++)
        {
            if (f != file)
                result |= 1ul << RankAndFileHelpers.SquareIndex(rank, f);
        }

        return result;
    }

    public static ulong GenerateDiagonalMoves(int square)
    {
        var result = 0ul;

        var rank = RankAndFileHelpers.RankIndex(square);
        var file = RankAndFileHelpers.FileIndex(square);

        foreach (var direction in MoveDir.All())
        {
            var targetRank = rank + direction.DeltaRank;
            var targetFile = file + direction.DeltaFile;

            while (targetRank is > 0 and < 7 && targetFile is > 0 and < 7)
            {
                result |= 1ul << RankAndFileHelpers.SquareIndex(targetRank, targetFile);

                targetFile += direction.DeltaFile;
                targetRank += direction.DeltaRank;
            }
        }

        return result;
    }

    public static ulong GetThisOccupancy(int index, ulong existingPath)
    {
        var occupancy = 0ul;
        var mask = existingPath;

        // this is the number of bits in the fully set path, so the number of different ones to toggle off
        var numBitsInMask = (int)ulong.PopCount(existingPath);

        for (var i = 0; i < numBitsInMask; i++)
        {
            var bottomBitIndex = (int)ulong.TrailingZeroCount(mask);

            mask &= mask - 1; // pop this bit to zero

            if ((index & (1 << i)) != 0)
            {
                // index trick - if this number is set in the index, we should keep it
                occupancy |= 1ul << bottomBitIndex;
            }
        }

        return occupancy;
    }

    public static ulong GetStraightAttacks(int square, ulong occupancies)
    {
        var result = 0ul;
        var rank = RankAndFileHelpers.RankIndex(square);
        var file = RankAndFileHelpers.FileIndex(square);

        // move up in the files from current place
        for (var f = file + 1; f < 8; f++)
        {
            result |= 1ul << RankAndFileHelpers.SquareIndex(rank, f);

            if ((occupancies & 1ul << RankAndFileHelpers.SquareIndex(rank, f)) > 0)
                break;
        }

        // move down in the files
        for (var f = file - 1; f >= 0; f--)
        {
            result |= 1ul << RankAndFileHelpers.SquareIndex(rank, f);

            if ((occupancies & 1ul << RankAndFileHelpers.SquareIndex(rank, f)) > 0)
                break;
        }

        for (var r = rank + 1; r < 8; r++)
        {
            result |= 1ul << RankAndFileHelpers.SquareIndex(r, file);

            if ((occupancies & 1ul << RankAndFileHelpers.SquareIndex(r, file)) > 0)
                break;
        }

        for (var r = rank - 1; r >= 0; r--)
        {
            result |= 1ul << RankAndFileHelpers.SquareIndex(r, file);

            if ((occupancies & 1ul << RankAndFileHelpers.SquareIndex(r, file)) > 0)
                break;
        }

        return result;
    }

    public static ulong GetDiagonalAttacks(int square, ulong occupancy)
    {
        var result = 0ul;
        var rank = RankAndFileHelpers.RankIndex(square);
        var file = RankAndFileHelpers.FileIndex(square);

        foreach (var direction in MoveDir.All())
        {
            var r = rank + direction.DeltaRank;
            var f = file + direction.DeltaFile;

            while (r >=0 && r < 8 && f >= 0 && f < 8 )
            {
                result |= 1ul << RankAndFileHelpers.SquareIndex(r, f);
                if ((occupancy & 1ul << RankAndFileHelpers.SquareIndex(r, f)) >0)
                    break;

                r += direction.DeltaRank;
                f += direction.DeltaFile;
            }
        }

        return result;
    }
}