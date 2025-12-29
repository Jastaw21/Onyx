namespace Onyx.Core;

public readonly struct Square
{
    public readonly int RankIndex;
    public readonly int FileIndex;

    public int SquareIndex => RankIndex * 8 + FileIndex;

    public string Notation => $"{(char)('a' + FileIndex)}{(char)('1' + RankIndex)}";
    public ulong Bitboard => 1ul << SquareIndex;

    public Square(int rankIndex, int fileIndex)
    {
        FileIndex = fileIndex;
        RankIndex = rankIndex;
    }

    public Square(int squareIndex)
    {
        RankIndex = squareIndex / 8;
        FileIndex = squareIndex % 8;
    }

    public Square(string fen)
    {
        var file = fen[0] - 'a';
        var rank = fen[1] - '1';
        RankIndex = rank;
        FileIndex = file;
    }
}

public static class RankAndFileHelpers
{
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
    public static ulong GetRayBetween(Square from, Square to)
    {
        var result = 0ul;
        var fromFileIndex = from.FileIndex;
        var toFileIndex = to.FileIndex;
        var fromRankIndex = from.RankIndex;
        var toRankIndex = to.RankIndex;

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