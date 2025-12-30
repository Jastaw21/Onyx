namespace Onyx.Core;



public static class RankAndFileHelpers
{
    public static int SquareIndex(string notation)
    {
        var file = notation[0] - 'a';
        var rank = notation[1] - '1';
        return SquareIndex(rank, file);
    }

    public static string Notation(int square)
    {
        var rankIndex = RankIndex(square);
        var fileIndex = FileIndex(square);
        return $"{(char)('a' + fileIndex)}{(char)('1' + rankIndex)}";
    }
    public static int RankIndex(int square)
    {
        return square / 8;
    }

    public static int FileIndex(int square)
    {
        return square % 8;
    }

    public static int SquareIndex(int rank, int file)
    {
        return rank * 8 + file;
    }

    public static ulong GetRayBetween(int from, int to)
    {
        var result = 0ul;
        var fromFileIndex = FileIndex(from);
        var toFileIndex = FileIndex(to);
        var fromRankIndex = RankIndex(from);
        var toRankIndex = RankIndex(to);

        var diag = Math.Abs(fromFileIndex - toFileIndex) == Math.Abs(fromRankIndex - toRankIndex);
        var straight = fromFileIndex == toFileIndex || fromRankIndex == toRankIndex;

        if (!diag && !straight)
            return 0ul;

        var deltaRank = toRankIndex - fromRankIndex;
        var deltaFile = toFileIndex - fromFileIndex;
        var steps = diag ? deltaRank : deltaFile != 0 ? deltaFile : deltaRank;
        var rankSign = Math.Sign(deltaRank);
        var fileSign = Math.Sign(deltaFile);
        for (var step = 0; step <= Math.Abs(steps); step++)
        {
            var rank = fromRankIndex + step * rankSign;
            var file = fromFileIndex + step * fileSign;
            var squareIndex = SquareIndex(rank, file);
            var thisSquare = 1ul << squareIndex;
            result |= thisSquare;
        }
        return result;
    }
    
}