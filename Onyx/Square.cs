namespace Onyx;

public readonly struct Square
{
    public readonly int RankIndex;
    public readonly int FileIndex;

    public int SquareIndex => RankIndex * 8 + FileIndex;

    public string Notation => $"{(char)('a' + FileIndex)}{(char)('1' + RankIndex)}";

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
}