namespace Onyx.Core;

public class Zobrist
{
    private readonly int seed = 123111;
    private Random _random;

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

    private ulong whiteToMove;
    public ulong HashValue { get; private set; }

    public Zobrist(string fen)
    {
        InitZobrist();
        BuildZobristFromFen(fen);
    }

    private void BuildZobristFromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);
        HashValue = 0;
        if (fenDetails.ColourToMove == Colour.White)
            HashValue ^= whiteToMove;

        int rank = 7;
        int file = 0;
        int i = 0;
        while (i < fenDetails.PositionFen.Length)
        {
            // next line indicator
            if (fenDetails.PositionFen[i] == '/')
            {
                rank--; // move to the next rank down
                file = 0; // and back to the start
            }
            // empty cells indicator
            else if (Char.IsAsciiDigit(fenDetails.PositionFen[i]))
                file += fenDetails.PositionFen[i] - '0';

            // break at space, as the rest is all castling/en passant stuff, not relevant to us
            else if (fenDetails.PositionFen[i] == ' ')
                break;

            // this is a piece, so set it and move the file on
            else
            {
                var square = rank * 8 + file;
                var valueHere = GetArrayFromChar(fenDetails.PositionFen[i])[square];
                HashValue ^= valueHere;
                file++;
            }

            i++;
        }
    }

    private void InitZobrist()
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

        whiteToMove = NextUlong();
    }

    private void FillRandomArray(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = NextUlong();
        }
    }

    private ulong NextUlong()
    {
        var buffer = new byte[8];
        _random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    private ulong[] GetArrayFromChar(char c)
    {
        return c switch
        {
            'P' => whitePawn,
            'N' => whiteKnight,
            'B' => whiteBishop,
            'R' => whiteRook,
            'Q' => whiteQueen,
            'K' => whiteKing,
            'p' => blackPawn,
            'n' => blackKnight,
            'b' => blackBishop,
            'r' => blackRook,
            'q' => blackQueen,
            'k' => blackKing,
            _ => whitePawn
        };
    }
}