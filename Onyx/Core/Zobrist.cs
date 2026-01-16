namespace Onyx.Core;

public static class Zobrist
{
    private static readonly int Seed = 123111;
    private static Random _random;

    private static readonly ulong[] WhitePawn = new ulong[64];
    private static readonly ulong[] WhiteRook = new ulong[64];
    private static readonly ulong[] WhiteBishop = new ulong[64];
    private static readonly ulong[] WhiteKnight = new ulong[64];
    private static readonly ulong[] WhiteQueen = new ulong[64];
    private static readonly ulong[] WhiteKing = new ulong[64];

    private static readonly ulong[] BlackPawn = new ulong[64];
    private static readonly ulong[] BlackRook = new ulong[64];
    private static readonly ulong[] BlackBishop = new ulong[64];
    private static readonly ulong[] BlackKnight = new ulong[64];
    private static readonly ulong[] BlackQueen = new ulong[64];
    private static readonly ulong[] BlackKing = new ulong[64];

    private static ulong _whiteToMove;

    static Zobrist()
    {
        _random = new Random(Seed);
        InitZobrist();
    }

    public static ulong MakeNullMove(ulong hashValue)
    {
        var newValue = hashValue;
        newValue^= _whiteToMove;
        return newValue;
    }
    public static ulong FromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);
        var hashValue = 0ul;
        if (fenDetails.WhiteToMove)
            hashValue ^= _whiteToMove;

        var rank = 7;
        var file = 0;
        var i = 0;
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
                hashValue ^= valueHere;
                file++;
            }

            i++;
        }
        
        return hashValue;
    }

    public static ulong ApplyMove(Move move, ulong hashIn, sbyte? capturedPiece = null, int? capturedOnSquare = null)
    {
        var hashValue = hashIn;
        var movedPieceChar = Fen.GetCharFromPiece(move.PieceMoved);
        var movedPieceArray = GetArrayFromChar(movedPieceChar);
        var movedPieceToRand = movedPieceArray[move.To];
        var movedPieceFromRand = movedPieceArray[move.From];

        // move the moving piece, turning its from square off, and to square on
        hashValue ^= movedPieceFromRand;
        hashValue ^= movedPieceToRand;

        // test capture
        if (capturedPiece.HasValue && capturedOnSquare.HasValue)
        {
            var capturedPieceChar = Fen.GetCharFromPiece(capturedPiece.Value);
            var capturedPieceArray = GetArrayFromChar(capturedPieceChar);
            var capturedPieceRand = capturedPieceArray[capturedOnSquare.Value];
            hashValue ^= capturedPieceRand;
        }

        if (move is { IsPromotion: true, PromotedPiece: not null })
        {
            // We need to undo the move to of the piece, thats covered in MovePiece,
            // as obviously for promotion this is overridden by the promoted piece. Explicitly set it off here
            hashValue ^= movedPieceToRand;

            var promotedPieceChar = Fen.GetCharFromPiece(move.PromotedPiece.Value);
            var promotedPieceArray = GetArrayFromChar(promotedPieceChar);
            var promotedPieceRand = promotedPieceArray[move.To];
            hashValue ^= promotedPieceRand;
        }

        if (move.IsCastling)
        {
            var affectedRook = Piece.IsWhite(move.PieceMoved) ? Piece.WR : Piece.BR;

            var rookChar = Fen.GetCharFromPiece(affectedRook);
            var rookArray = GetArrayFromChar(rookChar);

            var toFileIndex = RankAndFile.FileIndex(move.To);
            var toRankIndex = RankAndFile.RankIndex(move.To);

            var rookNewFile = toFileIndex == 2 ? 3 : 5;
            var rookOldFile = toFileIndex == 2 ? 0 : 7;
            var rookFrom = RankAndFile.SquareIndex(toRankIndex, rookOldFile);
            var rookTo = RankAndFile.SquareIndex(toRankIndex, rookNewFile);

            var rookFromRand = rookArray[rookFrom];
            var rookToRand = rookArray[rookTo];

            hashValue ^= rookFromRand;
            hashValue ^= rookToRand;
        }

        hashValue ^= _whiteToMove;
        
        return hashValue;
    }

    private static void InitZobrist()
    {
        FillRandomArray(WhitePawn);
        FillRandomArray(WhiteRook);
        FillRandomArray(WhiteBishop);
        FillRandomArray(WhiteQueen);
        FillRandomArray(WhiteKing);
        FillRandomArray(WhitePawn);
        FillRandomArray(WhiteKnight);

        FillRandomArray(BlackPawn);
        FillRandomArray(BlackRook);
        FillRandomArray(BlackBishop);
        FillRandomArray(BlackQueen);
        FillRandomArray(BlackKing);
        FillRandomArray(BlackPawn);
        FillRandomArray(BlackKnight);

        _whiteToMove = NextUlong();
    }

    private static void FillRandomArray(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = NextUlong();
        }
    }

    private static ulong NextUlong()
    {
        var buffer = new byte[8];
        _random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    private static ulong[] GetArrayFromChar(char c)
    {
        return c switch
        {
            'P' => WhitePawn,
            'N' => WhiteKnight,
            'B' => WhiteBishop,
            'R' => WhiteRook,
            'Q' => WhiteQueen,
            'K' => WhiteKing,
            'p' => BlackPawn,
            'n' => BlackKnight,
            'b' => BlackBishop,
            'r' => BlackRook,
            'q' => BlackQueen,
            'k' => BlackKing,
            _ => WhitePawn
        };
    }
}