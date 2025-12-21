namespace Onyx.Core;

public class Zobrist
{
    private int seed = 123111;
    private Random _random;
    
    private ulong[] whitePawn = new ulong[64];
    private ulong[] whiteRook = new ulong[64];
    private ulong[] whiteBishop = new ulong[64];
    private ulong[] whiteKnight = new ulong[64];
    private ulong[] whiteQueen = new ulong[64];
    private ulong[] whiteKing = new ulong[64];
    
    private ulong[] blackPawn = new ulong[64];
    private ulong[] blackRook = new ulong[64];
    private ulong[] blackBishop = new ulong[64];
    private ulong[] blackKnight = new ulong[64];
    private ulong[] blackQueen = new ulong[64];
    private ulong[] blackKing = new ulong[64];

    public Zobrist()
    {
        _random = new Random(seed);
    }
}