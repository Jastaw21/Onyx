namespace Onyx.Core;

public class Zobrist
{
    private readonly int seed = 123111;
    private readonly Random _random;

    private readonly ulong[] whitePawn = new ulong[64];
    private readonly ulong[] whiteRook = new ulong[64];
    private readonly ulong[] whiteBishop = new ulong[64];
    private readonly ulong[] whiteKnight = new ulong[64];
    private readonly ulong[] whiteQueen = new ulong[64];
    private readonly ulong[] whiteKing = new ulong[64];

    private readonly ulong[] blackPawn = new ulong[64];
    private readonly ulong[] blackRook = new ulong[64];
    private readonly ulong[] blackBishop = new ulong[64];
    private readonly ulong[] blackKnight = new ulong[64];
    private readonly ulong[] blackQueen = new ulong[64];
    private readonly ulong[] blackKing = new ulong[64];

    public Zobrist()
    {
        _random = new Random(seed);

        FillRandomArray(whitePawn);
        FillRandomArray(whiteRook);
        FillRandomArray(whiteBishop);
        FillRandomArray(whiteQueen);
        FillRandomArray(whiteKing);
        FillRandomArray(whitePawn);
        FillRandomArray(whiteKnight);

        FillRandomArray(blackPawn);
        FillRandomArray(blackRook);
        FillRandomArray(blackBishop);
        FillRandomArray(blackQueen);
        FillRandomArray(blackKing);
        FillRandomArray(blackPawn);
        FillRandomArray(blackKnight);
    }

    private void FillRandomArray(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = NextUlong();
        }

        return;


        ulong NextUlong()
        {
            var buffer = new byte[8];
            _random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}