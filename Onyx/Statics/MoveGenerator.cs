using System.Reflection;
using Onyx.Core;

namespace Onyx.Statics;

public static class MoveGenerator
{
    public static int GetLegalMoves(Board board, Span<Move> moveBuffer, bool alreadyKnowBoardInCheck = false, bool isAlreadyInCheck = false)
    {
        Span<Move> pseudoMovesBuffer = stackalloc Move[256];
        var pseudoMoveCount = GetMoves(board.WhiteToMove , board, pseudoMovesBuffer);
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

    public static int GetMoves(Board board, Span<Move> moveBuffer)
    {
        return GetMoves(board.WhiteToMove , board, moveBuffer);
    }

    public static int GetMoves(sbyte piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        if (Piece.PieceType(piece) != Piece.Pawn)
        {
            GenerateBasicMoves(piece, square, board, moveBuffer, ref count);
            GenerateCastlingMoves(piece, square, board, moveBuffer, ref count);
        }
        else
        {
            GeneratePawnMoves(piece, square, board, moveBuffer, ref count);
            GeneratePawnPromotionMoves(piece, square, board, moveBuffer, ref count);
        }

        return count;
    }

    public static int GetMoves(sbyte piece, Board board, Span<Move> moveBuffer, ref int count)
    {
        var thisPieceStartSquares = board.Bitboards.OccupancyByPiece(piece);
        while (thisPieceStartSquares > 0)
        {
            var lowestSetBit = ulong.TrailingZeroCount(thisPieceStartSquares);
            var thisSquare = (int)lowestSetBit;
            GetMoves(piece, thisSquare, board, moveBuffer, ref count);

            thisPieceStartSquares &= thisPieceStartSquares - 1;
        }

        return count;
    }

    public static int GetMoves(bool forWhite, Board board, Span<Move> moveBuffer)
    {
        var moveCount = 0;
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            GetMoves(piece, board, moveBuffer, ref moveCount);
        }

        return moveCount;
    }

    private static void GeneratePawnMoves(sbyte piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        var rankIndex = RankAndFile.RankIndex(square);
        var isWhite = Piece.IsWhite(piece);
        // don't do anything if it's promotion eligible - delegate all promotion logic to GeneratePromotionMoves
        if ((isWhite && rankIndex == 6) || (!isWhite && rankIndex == 1))
            return;

        var opponentOccupancy = board.Bitboards.OccupancyByColour(isWhite);
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(!isWhite);
        var occupancy = opponentOccupancy | movingSideOccupancy;

        var rawMoveOutput = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, occupancy);
        var pushes = MagicBitboards.MagicBitboards.GetPawnPushes(isWhite, square, occupancy);
        var attacks = rawMoveOutput ^ pushes;

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
                normalAttacks |= 1ul << board.EnPassantSquare.Value;
                enPassantAttacks |= 1ul << board.EnPassantSquare.Value;
            }
        }

        var result = pushes | normalAttacks;
        result &= ~board.Bitboards.OccupancyByColour(!isWhite);
        // Add moves from the result bitboard
        while (result > 0)
        {
            var lowest = ulong.TrailingZeroCount(result);
            var move = new Move(piece, square, (int)lowest);
            
            // is a capture
            if (((1ul << (int)lowest) & opponentOccupancy) != 0)
            {
                var capturedPiece = board.Bitboards.PieceAtSquare((int)lowest);
                if (capturedPiece.HasValue)
                {
                    move.CapturedPiece = capturedPiece.Value;
                }
            }
            
            // is an en passant capture
            if (((1ul << (int)lowest) & enPassantAttacks) != 0)
            {
                var capturedPiece = Piece.MakePiece(Piece.Pawn, !isWhite);
                move.CapturedPiece = capturedPiece;
            }
            move.HasCaptureBeenChecked = true;
            moveBuffer[count++] = move;
            result &= result - 1;
        }
    }

    private static void GeneratePawnPromotionMoves(sbyte piece, int square, Board board, Span<Move> moveBuffer,
        ref int count)
    {
        var isWhite = Piece.IsWhite(piece);
        if (Piece.PieceType(piece) != Piece.Pawn)
            return;

        var rankIndex = RankAndFile.RankIndex(square);

        if ((isWhite && rankIndex != 6) || (!isWhite && rankIndex != 1))
            return;

        var offset = isWhite ? 8 : -8;
        var promotionPieces = isWhite ? Piece._whitePromotionTypes : Piece._blackPromotionTypes;
        
        // non capturing promotion
        foreach (var promotionType in promotionPieces)
        {
            var targetSquare = square + offset;
            if (!board.Bitboards.PieceAtSquare(targetSquare).HasValue)
            {
                var move = new Move(piece, square, targetSquare)
                {
                    PromotedPiece = promotionType
                };
                moveBuffer[count++] = move;
            }
        }

        var fileIndex = RankAndFile.FileIndex(square);
        // can capture to right (board wise, not piece wise, it's left as far as a black pawn is concerned)
        if (fileIndex < 7)
        {
            var targetSquare = square + offset + 1;
            var pieceAtTarget = board.Bitboards.PieceAtSquare(targetSquare);
            if (pieceAtTarget.HasValue // needs to be a piece there 
                && Piece.IsWhite(pieceAtTarget.Value) != isWhite // of the other colour
                && Piece.PieceType(pieceAtTarget.Value) != Piece.King) // and not a king
            {
                foreach (var promotionType in promotionPieces)
                {
                    var move = new Move(piece, square, targetSquare) { PromotedPiece = promotionType };
                    move.CapturedPiece = pieceAtTarget.Value;
                    moveBuffer[count++] = move;
                }
            }
        }

        // can capture to left (board wise, not piece wise, it's right as far as a black pawn is concerned)
        if (fileIndex > 0)
        {
            var targetSquare = square + offset - 1;
            var pieceAtTarget = board.Bitboards.PieceAtSquare(targetSquare);
            if (pieceAtTarget.HasValue // needs to be a piece there 
                && Piece.IsWhite(pieceAtTarget.Value) != isWhite // of the other colour
                && Piece.PieceType(pieceAtTarget.Value) != Piece.King) // and not a king
            {
                foreach (var promotionType in promotionPieces)
                {
                    var move = new Move(piece, square, targetSquare) { PromotedPiece = promotionType };
                    move.CapturedPiece = pieceAtTarget.Value;
                    move.HasCaptureBeenChecked = true;
                    moveBuffer[count++] = move;
                }
            }
        }
    }

    private static void GenerateCastlingMoves(sbyte piece, int square, Board board, Span<Move> moveBuffer,
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
        Board board,
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


    private static void GenerateBasicMoves(sbyte piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        var moves = GetMovesUlong(piece, square, board);
        var opponentOccupancy = board.Bitboards.OccupancyByColour(Piece.IsWhite(piece));
        while (moves > 0)
        {
            var lowest = ulong.TrailingZeroCount(moves);
            var move = new Move(piece, square, (int)lowest);
            if (((1ul << (int)lowest) & opponentOccupancy) != 0)
            {
                var capturedPiece = board.Bitboards.PieceAtSquare((int)lowest);
                if (capturedPiece.HasValue)
                    move.CapturedPiece = capturedPiece.Value;
            }
            move.HasCaptureBeenChecked = true;
            moveBuffer[count++] = move;
            moves &= moves - 1;
        }
    }


    private static ulong GetMovesUlong(sbyte piece, int square, Board board)
    {
        var result = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(Piece.IsBlack(piece));
        result &= ~movingSideOccupancy;
        return result;
    }
}