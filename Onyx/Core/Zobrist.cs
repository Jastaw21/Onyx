namespace Onyx.Core;

public static class Zobrist
{
    private const int Seed = 123111;
    private static readonly Random Random;

    private static readonly ulong[,] PieceSquare = new ulong[32, 64]; // Use sbyte piece value directly as index. 32 to be safe for 1<<4 + 6

    public static ulong WhiteToMove { get; private set; }

    public static ulong[] EnPassantSquare { get; private set; } = new ulong[64];
    private static readonly ulong[,] CastlingRights = new ulong[2, 2]; // [w/b, kingside/queenside]

    static Zobrist()
    {
        Random = new Random(Seed);
        InitZobrist();
    }

    public static ulong MakeNullMove(ulong hashValue)
    {
        var newValue = hashValue;
        newValue ^= WhiteToMove;
        return newValue;
    }

    public static ulong FromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);
        var hashValue = 0ul;
        if (fenDetails.WhiteToMove)
            hashValue ^= WhiteToMove;

        // handle the special board state bits
        if (fenDetails.EnPassantSquare.HasValue)
            hashValue ^= EnPassantSquare[fenDetails.EnPassantSquare.Value];

        if ((fenDetails.CastlingRights & BoardConstants.WhiteKingsideCastlingFlag) > 0)
            hashValue ^= CastlingRights[0, 0];
        if ((fenDetails.CastlingRights & BoardConstants.WhiteQueensideCastlingFlag) > 0)
            hashValue ^= CastlingRights[0, 1];
        if ((fenDetails.CastlingRights & BoardConstants.BlackKingsideCastlingFlag) > 0)
            hashValue ^= CastlingRights[1, 0];
        if ((fenDetails.CastlingRights & BoardConstants.BlackQueensideCastlingFlag) > 0)
            hashValue ^= CastlingRights[1, 1];

        // now handle all the piece placements
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
                var piece = Fen.GetPieceFromChar(fenDetails.PositionFen[i]);
                hashValue ^= PieceSquare[piece, square];
                file++;
            }

            i++;
        }

        return hashValue;
    }

    public static ulong ApplyMove(Move move, ulong hashIn, sbyte? capturedPiece = null, int? capturedOnSquare = null,
        int? epBefore = null, int? epAfter = null, int? castlingRights = null, int? newCastlingRights = null)
    {
        var hashValue = hashIn;
        var movedPieceToRand = PieceSquare[move.PieceMoved, move.To];
        var movedPieceFromRand = PieceSquare[move.PieceMoved, move.From];

        // move the moving piece, turning its from square off, and to square on
        hashValue ^= movedPieceFromRand;
        hashValue ^= movedPieceToRand;

        // test capture
        if (capturedPiece.HasValue && capturedOnSquare.HasValue)
        {
            hashValue ^= PieceSquare[capturedPiece.Value, capturedOnSquare.Value];
        }

        if (move.IsPromotion)
        {
            // We need to undo the move to of the piece, thats covered in MovePiece,
            // as obviously for promotion this is overridden by the promoted piece. Explicitly set it off here
            hashValue ^= movedPieceToRand;
            var promotedPieceRand = PieceSquare[move.PromotedPiece!.Value, move.To];
            hashValue ^= promotedPieceRand;
        }

        else if (move.IsCastling)
        {
            var affectedRook = Piece.IsWhite(move.PieceMoved) ? Piece.WR : Piece.BR;

            var toFileIndex = RankAndFile.FileIndex(move.To);
            var toRankIndex = RankAndFile.RankIndex(move.To);

            var rookNewFile = toFileIndex == 2 ? 3 : 5;
            var rookOldFile = toFileIndex == 2 ? 0 : 7;
            var rookFromRand = PieceSquare[affectedRook, RankAndFile.SquareIndex(toRankIndex, rookOldFile)];
            var rookToRand = PieceSquare[affectedRook, RankAndFile.SquareIndex(toRankIndex, rookNewFile)];

            hashValue ^= rookFromRand;
            hashValue ^= rookToRand;
        }

        if (epAfter.HasValue)
            hashValue ^= EnPassantSquare[epAfter.Value];
        if (epBefore.HasValue)
            hashValue ^= EnPassantSquare[epBefore.Value];


        if (castlingRights.HasValue && newCastlingRights.HasValue && castlingRights.Value != newCastlingRights.Value)
        {
            if ((castlingRights.Value & BoardConstants.WhiteKingsideCastlingFlag) !=
                (newCastlingRights.Value & BoardConstants.WhiteKingsideCastlingFlag))
                hashValue ^= CastlingRights[0, 0];
            if ((castlingRights.Value & BoardConstants.WhiteQueensideCastlingFlag) !=
                (newCastlingRights.Value & BoardConstants.WhiteQueensideCastlingFlag))
                hashValue ^= CastlingRights[0, 1];
            if ((castlingRights.Value & BoardConstants.BlackKingsideCastlingFlag) !=
                (newCastlingRights.Value & BoardConstants.BlackKingsideCastlingFlag))
                hashValue ^= CastlingRights[1, 0];
            if ((castlingRights.Value & BoardConstants.BlackQueensideCastlingFlag) !=
                (newCastlingRights.Value & BoardConstants.BlackQueensideCastlingFlag))
                hashValue ^= CastlingRights[1, 1];
        }

        hashValue ^= WhiteToMove;

        return hashValue;
    }

    private static void InitZobrist()
    {
        foreach (var piece in Piece.AllPieces)
        {
            for (var square = 0; square < 64; square++)
            {
                PieceSquare[piece, square] = NextUlong();
            }
        }

        FillRandomArray(EnPassantSquare);
        FillRandomArray(CastlingRights);

        WhiteToMove = NextUlong();
    }

    private static void FillRandomArray(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = NextUlong();
        }
    }

    private static void FillRandomArray(ulong[,] array)
    {
        for (var i = 0; i < array.GetLength(0); i++)
        for (var j = 0; j < array.GetLength(1); j++)
            array[i, j] = NextUlong();
    }

    private static ulong NextUlong()
    {
        var buffer = new byte[8];
        Random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
}