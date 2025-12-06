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
}