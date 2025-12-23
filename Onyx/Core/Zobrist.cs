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

    public void ApplyMove(Move move, Piece? capturedPiece = null, Square? capturedOnSquare = null)
    {
        var movedPieceChar = Fen.GetCharFromPiece(move.PieceMoved);
        var movedPieceArray = GetArrayFromChar(movedPieceChar);
        var movedPieceToRand = movedPieceArray[move.To.SquareIndex];
        var movedPieceFromRand = movedPieceArray[move.From.SquareIndex];

        // move the moving piece, turning tits from square off, and to square on
        HashValue ^= movedPieceFromRand;
        HashValue ^= movedPieceToRand;

        // test capture
        if (capturedPiece.HasValue && capturedOnSquare.HasValue)
        {
            var capturedPieceChar = Fen.GetCharFromPiece(capturedPiece.Value);
            var capturedPieceArray = GetArrayFromChar(capturedPieceChar);
            var capturedPieceRand = capturedPieceArray[capturedOnSquare.Value.SquareIndex];
            HashValue ^= capturedPieceRand;
        }

        if (move is { IsPromotion: true, PromotedPiece: not null })
        {
            // We need to undo the move to of the piece, thats covered in MovePiece,
            // as obviously for promotion this is overridden by the promoted piece. Explicitly set it off here
            HashValue ^= movedPieceToRand;

            var promotedPieceChar = Fen.GetCharFromPiece(move.PromotedPiece.Value);
            var promotedPieceArray = GetArrayFromChar(promotedPieceChar);
            var promotedPieceRand = promotedPieceArray[move.To.SquareIndex];
            HashValue ^= promotedPieceRand;
        }

        if (move.IsCastling)
        {
            var affectedRook = move.PieceMoved.Colour == Colour.White
                ? Piece.WR
                : Piece.BR;

            var rookChar = Fen.GetCharFromPiece(affectedRook);
            var rookArray = GetArrayFromChar(rookChar);

            var rookNewFile = move.To.FileIndex == 2 ? 3 : 5;
            var rookOldFile = move.To.FileIndex == 2 ? 0 : 7;
            var rookFrom = new Square(move.To.RankIndex, rookOldFile);
            var rookTo = new Square(move.To.RankIndex, rookNewFile);

            var rookFromRand = rookArray[rookFrom.SquareIndex];
            var rookToRand = rookArray[rookTo.SquareIndex];

            HashValue ^= rookFromRand;
            HashValue ^= rookToRand;
        }

        HashValue ^= whiteToMove;
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