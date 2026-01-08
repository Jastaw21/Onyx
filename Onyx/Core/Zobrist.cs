namespace Onyx.Core;

public static class Zobrist
{
    private static readonly int _seed = 123111;
    private static Random _random;

    private static readonly ulong[] _whitePawn = new ulong[64];
    private static readonly ulong[] _whiteRook = new ulong[64];
    private static readonly ulong[] _whiteBishop = new ulong[64];
    private static readonly ulong[] _whiteKnight = new ulong[64];
    private static readonly ulong[] _whiteQueen = new ulong[64];
    private static readonly ulong[] _whiteKing = new ulong[64];

    private static readonly ulong[] _blackPawn = new ulong[64];
    private static readonly ulong[] _blackRook = new ulong[64];
    private static readonly ulong[] _blackBishop = new ulong[64];
    private static readonly ulong[] _blackKnight = new ulong[64];
    private static readonly ulong[] _blackQueen = new ulong[64];
    private static readonly ulong[] _blackKing = new ulong[64];

    private static ulong _whiteToMove;

    static Zobrist()
    {
        _random = new Random(_seed);
        InitZobrist();
    }

    public static ulong MakeNullMove(ulong HashValue)
    {
        var newValue = HashValue;
        newValue^= _whiteToMove;
        return newValue;
    }
    public static ulong FromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);
        var HashValue = 0ul;
        if (fenDetails.WhiteToMove)
            HashValue ^= _whiteToMove;

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
                HashValue ^= valueHere;
                file++;
            }

            i++;
        }
        
        return HashValue;
    }

    public static ulong ApplyMove(Move move, ulong hashIn, sbyte? capturedPiece = null, int? capturedOnSquare = null)
    {
        var HashValue = hashIn;
        var movedPieceChar = Fen.GetCharFromPiece(move.PieceMoved);
        var movedPieceArray = GetArrayFromChar(movedPieceChar);
        var movedPieceToRand = movedPieceArray[move.To];
        var movedPieceFromRand = movedPieceArray[move.From];

        // move the moving piece, turning its from square off, and to square on
        HashValue ^= movedPieceFromRand;
        HashValue ^= movedPieceToRand;

        // test capture
        if (capturedPiece.HasValue && capturedOnSquare.HasValue)
        {
            var capturedPieceChar = Fen.GetCharFromPiece(capturedPiece.Value);
            var capturedPieceArray = GetArrayFromChar(capturedPieceChar);
            var capturedPieceRand = capturedPieceArray[capturedOnSquare.Value];
            HashValue ^= capturedPieceRand;
        }

        if (move is { IsPromotion: true, PromotedPiece: not null })
        {
            // We need to undo the move to of the piece, thats covered in MovePiece,
            // as obviously for promotion this is overridden by the promoted piece. Explicitly set it off here
            HashValue ^= movedPieceToRand;

            var promotedPieceChar = Fen.GetCharFromPiece(move.PromotedPiece.Value);
            var promotedPieceArray = GetArrayFromChar(promotedPieceChar);
            var promotedPieceRand = promotedPieceArray[move.To];
            HashValue ^= promotedPieceRand;
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

            HashValue ^= rookFromRand;
            HashValue ^= rookToRand;
        }

        HashValue ^= _whiteToMove;
        
        return HashValue;
    }

    private static void InitZobrist()
    {
        FillRandomArray(_whitePawn);
        FillRandomArray(_whiteRook);
        FillRandomArray(_whiteBishop);
        FillRandomArray(_whiteQueen);
        FillRandomArray(_whiteKing);
        FillRandomArray(_whitePawn);
        FillRandomArray(_whiteKnight);

        FillRandomArray(_blackPawn);
        FillRandomArray(_blackRook);
        FillRandomArray(_blackBishop);
        FillRandomArray(_blackQueen);
        FillRandomArray(_blackKing);
        FillRandomArray(_blackPawn);
        FillRandomArray(_blackKnight);

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
            'P' => _whitePawn,
            'N' => _whiteKnight,
            'B' => _whiteBishop,
            'R' => _whiteRook,
            'Q' => _whiteQueen,
            'K' => _whiteKing,
            'p' => _blackPawn,
            'n' => _blackKnight,
            'b' => _blackBishop,
            'r' => _blackRook,
            'q' => _blackQueen,
            'k' => _blackKing,
            _ => _whitePawn
        };
    }
}