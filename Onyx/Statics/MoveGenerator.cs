using Onyx.Core;

namespace Onyx.Statics;

public static class MoveGenerator
{
    public static int GetLegalMoves(Position board, Span<Move> moveBuffer, bool alreadyKnowBoardInCheck = false,
        bool isAlreadyInCheck = false, bool capturesOnly = false)
    {
        Span<Move> pseudoMovesBuffer = stackalloc Move[256];
        var pseudoMoveCount = GetMoves(board.WhiteToMove, board, pseudoMovesBuffer,capturesOnly);
        var legalMoveCount = 0;

        for (var i = 0; i < pseudoMoveCount; i++)
        {
            var move = pseudoMovesBuffer[i];
            if (Referee.MoveIsLegal(move, board, alreadyKnowBoardInCheck, isAlreadyInCheck))
            {
                moveBuffer[legalMoveCount++] = move;
            }
        }

        return legalMoveCount;
    }

    public static int GetMoves(Position board, Span<Move> moveBuffer, bool capturesOnly = false)
    {
        return GetMoves(board.WhiteToMove, board, moveBuffer, capturesOnly);
    }

    public static int GetMoves(byte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        if (PieceTypes.PieceType(piece) != PieceTypes.Pawn)
        {
            GenerateBasicMoves(piece, square, board, moveBuffer, ref count, capturesOnly);
            if (!capturesOnly)
                GenerateCastlingMoves(piece, square, board, moveBuffer, ref count);
        }
        else
        {
            UnifiedPawnMoves(piece, square, board, moveBuffer, ref count, capturesOnly);
        }

        return count;
    }

    public static int GetMoves(byte piece, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        var thisPieceStartSquares = board.Bitboards.OccupancyByPiece(piece);
        while (thisPieceStartSquares > 0)
        {
            var lowestSetBit = ulong.TrailingZeroCount(thisPieceStartSquares);
            var thisSquare = (int)lowestSetBit;
            GetMoves(piece, thisSquare, board, moveBuffer, ref count, capturesOnly);

            thisPieceStartSquares &= thisPieceStartSquares - 1;
        }

        return count;
    }

    public static int GetMoves(bool forWhite, Position board, Span<Move> moveBuffer, bool capturesOnly = false)
    {
        var moveCount = 0;
        var pieces = forWhite ? PieceTypes._whitePieces : PieceTypes._blackPieces;
        foreach (var piece in pieces)
        {
            GetMoves(piece, board, moveBuffer, ref moveCount,capturesOnly);
        }

        return moveCount;
    }

    private static void UnifiedPawnMoves(byte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        var isWhite = PieceTypes.IsWhite(piece);
        var rankIndex = RankAndFile.RankIndex(square);

        var opponentOccupancy = board.Bitboards.OccupancyByColour(isWhite);
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(!isWhite);
        var occupancy = opponentOccupancy | movingSideOccupancy;

        var pushes = MagicBitboards.MagicBitboards.GetPawnPushes(isWhite, square, occupancy);
        var attacks = MagicBitboards.MagicBitboards.GetPawnAttacks(isWhite, square);

        var normalAttacks = opponentOccupancy & attacks;

        var enPassantAttacks = 0ul;
        // the board has a viable en passant square, and we're on an appropriate file
        if (board.EnPassantSquare != -1 &&
            Math.Abs(RankAndFile.FileIndex(board.EnPassantSquare) - RankAndFile.FileIndex(square)) == 1)
        {
            var relevantAttackRank = isWhite ? 5 : 2;
            var pawnHomeRank = isWhite ? 4 : 3;

            // all other conditions for en passant are met
            if (rankIndex == pawnHomeRank && relevantAttackRank == RankAndFile.RankIndex(board.EnPassantSquare))
            {
                var epSquare = 1ul << board.EnPassantSquare;
                normalAttacks |= epSquare;
                enPassantAttacks |= epSquare;
            }
        }

        var result = capturesOnly ? normalAttacks : pushes | normalAttacks;
        result &= ~movingSideOccupancy;

        var promotionMask = isWhite ? 0xff00000000000000 : 0xff;
        while (result > 0)
        {
            var lowest = ulong.TrailingZeroCount(result);
            var move = new Move(piece, square, (int)lowest);

            // is a capture
            var thisSquare = 1ul << (int)lowest;
            byte? captured = null;
            if ((thisSquare & opponentOccupancy) != 0)
            {
                captured = board.Bitboards.PieceAtSquare((int)lowest);
            }

            // is an en passant capture
            if ((thisSquare & enPassantAttacks) != 0)
            {
                captured = PieceTypes.MakePiece(PieceTypes.Pawn, !isWhite);
            }

            var isPromotion = (thisSquare & promotionMask) != 0;

            if (isPromotion)
            {
                var promotionPieces = isWhite ? PieceTypes._whitePromotionTypes : PieceTypes._blackPromotionTypes;
                foreach (var promotionType in promotionPieces)
                {
                    var promotionMove = new Move(piece, square, (int)lowest)
                    {
                        PromotedPiece = promotionType,
                        CapturedPiece = captured.GetValueOrDefault(0)
                    };
                    moveBuffer[count++] = promotionMove;
                }
            }

            if (!isPromotion)
            {
                move.CapturedPiece = captured.GetValueOrDefault(0);
                move.HasCaptureBeenChecked = true;
                moveBuffer[count++] = move;
            }

            result &= result - 1;
        }
    }

    private static void GenerateCastlingMoves(byte piece, int square, Position board, Span<Move> moveBuffer,
        ref int count)
    {
        if (PieceTypes.PieceType(piece) != PieceTypes.King || board.CastlingRights == 0)
            return;

        var isWhite = PieceTypes.IsWhite(piece);
        var expectedSquare = isWhite ? BoardHelpers.E1 : BoardHelpers.E8;

        if (square != expectedSquare)
            return;

        var occupancy = board.Bitboards.Occupancy();

        var kingSideRookSquare = isWhite ? BoardHelpers.H1 : BoardHelpers.H8;
        var queenSideRookSquare = isWhite ? BoardHelpers.A1 : BoardHelpers.A8;

        // Try kingside
        var pieceAtTargetSquare = board.Bitboards.PieceAtSquare(kingSideRookSquare);
        if (pieceAtTargetSquare.HasValue
            && PieceTypes.PieceType(pieceAtTargetSquare.Value) == PieceTypes.Rook
            && PieceTypes.IsWhite(pieceAtTargetSquare.Value) == isWhite)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardHelpers.WhiteKingsideCastlingFlag : BoardHelpers.BlackKingsideCastlingFlag,
                isWhite ? BoardHelpers.WhiteKingSideCastlingSquares : BoardHelpers.BlackKingSideCastlingSquares,
                isWhite ? BoardHelpers.G1 : BoardHelpers.G8,
                occupancy,
                !isWhite,
                moveBuffer,
                ref count
            );

        // Try queenside
        pieceAtTargetSquare = board.Bitboards.PieceAtSquare(queenSideRookSquare);
        if (pieceAtTargetSquare.HasValue
            && PieceTypes.PieceType(pieceAtTargetSquare.Value) == PieceTypes.Rook
            && PieceTypes.IsWhite(pieceAtTargetSquare.Value) == isWhite)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardHelpers.WhiteQueensideCastlingFlag : BoardHelpers.BlackQueensideCastlingFlag,
                isWhite ? BoardHelpers.WhiteQueenSideCastlingSquares : BoardHelpers.BlackQueenSideCastlingSquares,
                isWhite ? BoardHelpers.C1 : BoardHelpers.C8,
                occupancy,
                !isWhite,
                moveBuffer,
                ref count
            );
    }

    private static void TryCastling(
        Position board,
        byte piece,
        int fromSquare,
        int castlingFlag,
        ulong requiredEmptySquares,
        int targetSquare,
        ulong occupancy,
        bool opponentIsWhite,
        Span<Move> moveBuffer,
        ref int count)
    {
        // check board castling state
        if ((board.CastlingRights & castlingFlag) == 0)
            return;

        // is the path clear
        if ((requiredEmptySquares & occupancy) != 0)
            return;

        // Check if any square the king passes through is attacked (including where it starts)
        var squaresToCheck = requiredEmptySquares | (1ul << fromSquare);
        while (squaresToCheck != 0)
        {
            var squareIndex = (int)ulong.TrailingZeroCount(squaresToCheck);

            // Don't check b1/b8 for attack (queenside rook square)
            if (squareIndex != BoardHelpers.B1 && squareIndex != BoardHelpers.B8)
            {
                if (Referee.IsSquareAttacked(squareIndex, board, opponentIsWhite))
                    return;
            }

            squaresToCheck &= squaresToCheck - 1;
        }

        moveBuffer[count++] = new Move(piece, fromSquare, targetSquare);
    }


    private static void GenerateBasicMoves(byte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        var moves = GetMovesUlong(piece, square, board, capturesOnly);
        var opponentOccupancy = board.Bitboards.OccupancyByColour(PieceTypes.IsWhite(piece));
        while (moves > 0)
        {
            var thisSquare = (int)ulong.TrailingZeroCount(moves);
            var move = new Move(piece, square, thisSquare);
            if (((1ul << thisSquare) & opponentOccupancy) != 0)
            {
                var capturedPiece = board.Bitboards.PieceAtSquare(thisSquare);
                if (capturedPiece.HasValue)
                    move.CapturedPiece = capturedPiece.Value;
            }

            move.HasCaptureBeenChecked = true;
            moveBuffer[count++] = move;
            moves &= moves - 1;
        }
    }


    private static ulong GetMovesUlong(byte piece, int square, Position board, bool capturesOnly = false)
    {
        var opponentKing = PieceTypes.MakePiece(PieceTypes.King, !PieceTypes.IsWhite(piece));
        var opponentKingSquare = board.Bitboards.OccupancyByPiece(opponentKing);
        if (!capturesOnly)
        {
            var result = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
            var movingSideOccupancy = board.Bitboards.OccupancyByColour(PieceTypes.IsBlack(piece));
            result &= ~movingSideOccupancy; // cant go to own square
            result &= ~opponentKingSquare; // cant go to own king
            return result;
        }
        
        var movesByPiece = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var opponentOccupancy = board.Bitboards.OccupancyByColour(PieceTypes.IsWhite(piece));
        opponentOccupancy &= ~opponentKingSquare; // cant go to own king
        return movesByPiece & opponentOccupancy;
        
    }
}