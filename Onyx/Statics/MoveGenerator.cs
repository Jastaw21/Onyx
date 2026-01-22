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

    public static int GetMoves(sbyte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        if (Piece.PieceType(piece) != Piece.Pawn)
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

    public static int GetMoves(sbyte piece, Position board, Span<Move> moveBuffer, ref int count,
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
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            GetMoves(piece, board, moveBuffer, ref moveCount,capturesOnly);
        }

        return moveCount;
    }

    private static void UnifiedPawnMoves(sbyte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        var isWhite = Piece.IsWhite(piece);
        var rankIndex = RankAndFile.RankIndex(square);

        var opponentOccupancy = board.Bitboards.OccupancyByColour(isWhite);
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(!isWhite);
        var occupancy = opponentOccupancy | movingSideOccupancy;

        var pushes = MagicBitboards.MagicBitboards.GetPawnPushes(isWhite, square, occupancy);
        var attacks = MagicBitboards.MagicBitboards.GetPawnAttacks(isWhite, square);

        var normalAttacks = opponentOccupancy & attacks;

        var enPassantAttacks = 0ul;
        // the board has a viable en passant square, and we're on an appropriate file
        if (board.EnPassantSquare.HasValue &&
            Math.Abs(RankAndFile.FileIndex(board.EnPassantSquare.Value) - RankAndFile.FileIndex(square)) == 1)
        {
            var relevantAttackRank = isWhite ? 5 : 2;
            var pawnHomeRank = isWhite ? 4 : 3;

            // all other conditions for en passant are met
            if (rankIndex == pawnHomeRank && relevantAttackRank == RankAndFile.RankIndex(board.EnPassantSquare.Value))
            {
                var epSquare = 1ul << board.EnPassantSquare.Value;
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
            sbyte? captured = null;
            if ((thisSquare & opponentOccupancy) != 0)
            {
                captured = board.Bitboards.PieceAtSquare((int)lowest);
            }

            // is an en passant capture
            if ((thisSquare & enPassantAttacks) != 0)
            {
                captured = Piece.MakePiece(Piece.Pawn, !isWhite);
            }

            var isPromotion = (thisSquare & promotionMask) != 0;

            if (isPromotion)
            {
                var promotionPieces = isWhite ? Piece._whitePromotionTypes : Piece._blackPromotionTypes;
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

    private static void GenerateCastlingMoves(sbyte piece, int square, Position board, Span<Move> moveBuffer,
        ref int count)
    {
        if (Piece.PieceType(piece) != Piece.King || board.CastlingRights == 0)
            return;

        var isWhite = Piece.IsWhite(piece);
        var expectedSquare = isWhite ? BoardConstants.E1 : BoardConstants.E8;

        if (square != expectedSquare)
            return;

        var occupancy = board.Bitboards.Occupancy();

        var kingSideRookSquare = isWhite ? BoardConstants.H1 : BoardConstants.H8;
        var queenSideRookSquare = isWhite ? BoardConstants.A1 : BoardConstants.A8;

        // Try kingside
        var pieceAtTargetSquare = board.Bitboards.PieceAtSquare(kingSideRookSquare);
        if (pieceAtTargetSquare.HasValue
            && Piece.PieceType(pieceAtTargetSquare.Value) == Piece.Rook
            && Piece.IsWhite(pieceAtTargetSquare.Value) == isWhite)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardConstants.WhiteKingsideCastlingFlag : BoardConstants.BlackKingsideCastlingFlag,
                isWhite ? BoardConstants.WhiteKingSideCastlingSquares : BoardConstants.BlackKingSideCastlingSquares,
                isWhite ? BoardConstants.G1 : BoardConstants.G8,
                occupancy,
                !isWhite,
                moveBuffer,
                ref count
            );

        // Try queenside
        pieceAtTargetSquare = board.Bitboards.PieceAtSquare(queenSideRookSquare);
        if (pieceAtTargetSquare.HasValue
            && Piece.PieceType(pieceAtTargetSquare.Value) == Piece.Rook
            && Piece.IsWhite(pieceAtTargetSquare.Value) == isWhite)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardConstants.WhiteQueensideCastlingFlag : BoardConstants.BlackQueensideCastlingFlag,
                isWhite ? BoardConstants.WhiteQueenSideCastlingSquares : BoardConstants.BlackQueenSideCastlingSquares,
                isWhite ? BoardConstants.C1 : BoardConstants.C8,
                occupancy,
                !isWhite,
                moveBuffer,
                ref count
            );
    }

    private static void TryCastling(
        Position board,
        sbyte piece,
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
            if (squareIndex != BoardConstants.B1 && squareIndex != BoardConstants.B8)
            {
                if (Referee.IsSquareAttacked(squareIndex, board, opponentIsWhite))
                    return;
            }

            squaresToCheck &= squaresToCheck - 1;
        }

        moveBuffer[count++] = new Move(piece, fromSquare, targetSquare);
    }


    private static void GenerateBasicMoves(sbyte piece, int square, Position board, Span<Move> moveBuffer, ref int count,
        bool capturesOnly = false)
    {
        var moves = GetMovesUlong(piece, square, board, capturesOnly);
        var opponentOccupancy = board.Bitboards.OccupancyByColour(Piece.IsWhite(piece));
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


    private static ulong GetMovesUlong(sbyte piece, int square, Position board, bool capturesOnly = false)
    {
        var opponentKing = Piece.MakePiece(Piece.King, !Piece.IsWhite(piece));
        var opponentKingSquare = board.Bitboards.OccupancyByPiece(opponentKing);
        if (!capturesOnly)
        {
            var result = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
            var movingSideOccupancy = board.Bitboards.OccupancyByColour(Piece.IsBlack(piece));
            result &= ~movingSideOccupancy; // cant go to own square
            result &= ~opponentKingSquare; // cant go to own king
            return result;
        }
        
        var movesByPiece = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var opponentOccupancy = board.Bitboards.OccupancyByColour(Piece.IsWhite(piece));
        opponentOccupancy &= ~opponentKingSquare; // cant go to own king
        return movesByPiece & opponentOccupancy;
        
    }
}