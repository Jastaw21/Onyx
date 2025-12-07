using Onyx;

namespace MagicBitboardGenerator;

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
}