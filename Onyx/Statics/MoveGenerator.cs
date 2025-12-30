using Onyx.Core;

namespace Onyx.Statics;

public static class MoveGenerator
{
    public static int GetLegalMoves(Board board, Span<Move> moveBuffer)
    {
        Span<Move> pseudoMovesBuffer = stackalloc Move[256];
        int pseudoMoveCount = GetMoves(board.TurnToMove, board, pseudoMovesBuffer);
        int legalMoveCount = 0;

        for (int i = 0; i < pseudoMoveCount; i++)
        {
            var move = pseudoMovesBuffer[i];
            if (Referee.MoveIsLegal(move, board))
            {
                moveBuffer[legalMoveCount++] = move;
            }
        }

        return legalMoveCount;
    }

    public static int GetMoves(Board board, Span<Move> moveBuffer)
    {
        return GetMoves(board.TurnToMove, board, moveBuffer);
    }

    public static int GetMoves(Piece piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        if (piece.Type != PieceType.Pawn)
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

    public static int GetMoves(Piece piece, Board board, Span<Move> moveBuffer, ref int count)
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

    public static int GetMoves(Colour colour, Board board, Span<Move> moveBuffer)
    {
        int moveCount = 0;
        foreach (var piece in Piece.ByColour(colour))
        {
            GetMoves(piece, board, moveBuffer, ref moveCount);
        }

        return moveCount;
    }

    private static void GeneratePawnMoves(Piece piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        var rankIndex = RankAndFile.RankIndex(square);
        // don't do anything if it's promotion eligible - delegate all promotion logic to GeneratePromotionMoves
        if ((piece.Colour == Colour.White && rankIndex == 6) ||
            (piece.Colour == Colour.Black && rankIndex == 1))
            return;

        var opponentColour = piece.Colour == Colour.White ? Colour.Black : Colour.White;
        var opponentOccupancy = board.Bitboards.OccupancyByColour(opponentColour);
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        var occupancy = opponentOccupancy | movingSideOccupancy;

        var rawMoveOutput = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, occupancy);
        var pushes = MagicBitboards.MagicBitboards.GetPawnPushes(piece.Colour, square, occupancy);
        var attacks = rawMoveOutput ^ pushes;

        var normalAttacks = opponentOccupancy & attacks;

        // the board has a viable en passant square, and we're on an appropriate file
        if (board.EnPassantSquare.HasValue &&
            Math.Abs(RankAndFile.FileIndex(board.EnPassantSquare.Value) - RankAndFile.FileIndex(square)) == 1)
        {
            var relevantAttackRank = piece.Colour == Colour.Black ? 2 : 5;
            var pawnHomeRank = piece.Colour == Colour.Black ? 3 : 4;
            if (rankIndex == pawnHomeRank && relevantAttackRank == RankAndFile.RankIndex(board.EnPassantSquare.Value))
                normalAttacks |= 1ul << board.EnPassantSquare.Value;
        }

        var result = pushes | normalAttacks;
        result &= ~board.Bitboards.OccupancyByColour(piece.Colour);
        // Add moves from result bitboard
        while (result > 0)
        {
            var lowest = ulong.TrailingZeroCount(result);
            moveBuffer[count++] = new Move(piece, square, (int)lowest);
            result &= result - 1;
        }
    }

    private static void GeneratePawnPromotionMoves(Piece piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        if (piece.Type != PieceType.Pawn)
            return;
        var rankIndex = RankAndFile.RankIndex(square);
        if ((piece.Colour == Colour.White && rankIndex != 6) ||
            (piece.Colour == Colour.Black && rankIndex != 1))
            return;

        var offset = piece.Colour == Colour.White ? 8 : -8;
        foreach (var promotionType in Piece.PromotionTypes(piece.Colour))
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
        // can go right (board wise, not piece wise, it's left as far as a black pawn is concerned)
        if (fileIndex < 7)
        {
            var targetSquare = square + offset + 1;
            if (board.Bitboards.PieceAtSquare(targetSquare).HasValue // needs to be a piece there 
                && board.Bitboards.PieceAtSquare(targetSquare)!.Value.Colour != piece.Colour // of the other colour
                && board.Bitboards.PieceAtSquare(targetSquare) is not { Type: PieceType.King }) // and not a king
            {
                foreach (var promotionType in Piece.PromotionTypes(piece.Colour))
                {
                    moveBuffer[count++] = new Move(piece, square, targetSquare) { PromotedPiece = promotionType };
                }
            }
        }

        // can go left (board wise, not piece wise, it's right as far as a black pawn is concerned)
        if (fileIndex > 0)
        {
            var targetSquare = square + offset - 1;
            if (board.Bitboards.PieceAtSquare(targetSquare).HasValue // needs to be a piece there 
                && board.Bitboards.PieceAtSquare(targetSquare)!.Value.Colour != piece.Colour // of the other colour
                && board.Bitboards.PieceAtSquare(targetSquare) is not { Type: PieceType.King }) // and not a king
            {
                foreach (var promotionType in Piece.PromotionTypes(piece.Colour))
                {
                    moveBuffer[count++] = new Move(piece, square, targetSquare) { PromotedPiece = promotionType };
                }
            }
        }
    }

    private static void GenerateCastlingMoves(Piece piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        if (piece.Type != PieceType.King || board.CastlingRights == 0)
            return;

        var isWhite = piece.Colour == Colour.White;
        var expectedSquare = isWhite ? BoardConstants.E1 : BoardConstants.E8;

        if (square != expectedSquare)
            return;

        var opponentColour = isWhite ? Colour.Black : Colour.White;
        var occupancy = board.Bitboards.Occupancy();


        var kingSideRookSquare = piece.Colour == Colour.White ? BoardConstants.H1 : BoardConstants.H8;
        var queenSideRookSquare = piece.Colour == Colour.White ? BoardConstants.A1 : BoardConstants.A8;

        // Try kingside
        if (board.Bitboards.PieceAtSquare(kingSideRookSquare).HasValue
            && board.Bitboards.PieceAtSquare(kingSideRookSquare) is { Type: PieceType.Rook }
            && board.Bitboards.PieceAtSquare(kingSideRookSquare)!.Value.Colour == piece.Colour)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardConstants.WhiteKingsideCastlingFlag : BoardConstants.BlackKingsideCastlingFlag,
                isWhite ? BoardConstants.WhiteKingSideCastlingSquares : BoardConstants.BlackKingSideCastlingSquares,
                isWhite ? BoardConstants.G1 : BoardConstants.G8,
                occupancy,
                opponentColour,
                moveBuffer,
                ref count
            );

        // Try queenside
        if (board.Bitboards.PieceAtSquare(queenSideRookSquare).HasValue
            && board.Bitboards.PieceAtSquare(queenSideRookSquare) is { Type: PieceType.Rook }
            && board.Bitboards.PieceAtSquare(queenSideRookSquare)!.Value.Colour == piece.Colour)
            TryCastling(
                board,
                piece,
                square,
                isWhite ? BoardConstants.WhiteQueensideCastlingFlag : BoardConstants.BlackQueensideCastlingFlag,
                isWhite ? BoardConstants.WhiteQueenSideCastlingSquares : BoardConstants.BlackQueenSideCastlingSquares,
                isWhite ? BoardConstants.C1 : BoardConstants.C8,
                occupancy,
                opponentColour,
                moveBuffer,
                ref count
            );
    }

    private static void TryCastling(
        Board board,
        Piece piece,
        int fromSquare,
        int castlingFlag,
        ulong requiredEmptySquares,
        int targetSquare,
        ulong occupancy,
        Colour opponentColour,
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
                if (Referee.IsSquareAttacked(squareIndex, board, opponentColour))
                    return;
            }

            squaresToCheck &= squaresToCheck - 1;
        }

        moveBuffer[count++] = new Move(piece, fromSquare, targetSquare);
    }


    private static void GenerateBasicMoves(Piece piece, int square, Board board, Span<Move> moveBuffer, ref int count)
    {
        var moves = GetMovesUlong(piece, square, board);
        while (moves > 0)
        {
            var lowest = ulong.TrailingZeroCount(moves);
            moveBuffer[count++] = new Move(piece, square, (int)lowest);
            moves &= moves - 1;
        }
    }


    private static ulong GetMovesUlong(Piece piece, int square, Board board)
    {
        var result = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        result &= ~movingSideOccupancy;
        return result;
    }
}