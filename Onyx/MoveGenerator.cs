namespace Onyx;

public static class MoveGenerator
{
    public static List<Move> GetMoves(Piece piece, Square square, ref Board board)
    {
        var moveList = new List<Move>();
        GenerateBasicMoves(piece, square, ref board, moveList);
        GenerateCastlingMoves(piece, square, ref board, moveList);

        return moveList;
    }

    private static void GenerateCastlingMoves(Piece piece, Square square, ref Board board, List<Move> moveList)
    {
        if (piece.Type != PieceType.King || board.CastlingRights == 0)
            return;

        var isWhite = piece.Colour == Colour.White;
        var expectedSquare = isWhite ? BoardConstants.E1 : BoardConstants.E8;

        if (square.SquareIndex != expectedSquare)
            return;

        var occupancy = board.Bitboards.Occupancy();
        var result = 0ul;

        // Kingside
        var kingsideCastleSquares =
            isWhite ? BoardConstants.WhiteKingSideCastlingSquares : BoardConstants.BlackKingSideCastlingSquares;
        var kingsideFlag =
            isWhite ? BoardConstants.WhiteKingsideCastlingFlag : BoardConstants.BlackKingsideCastlingFlag;
        var kingsideTarget = isWhite ? BoardConstants.G1 : BoardConstants.G8;

        if ((kingsideCastleSquares & occupancy) == 0 && (board.CastlingRights & kingsideFlag) > 0)
            result |= 1ul << kingsideTarget;

        // Queenside
        var queensideCastleSquares = isWhite
            ? BoardConstants.WhiteQueenSideCastlingSquares
            : BoardConstants.BlackQueenSideCastlingSquares;
        var queensideFlag =
            isWhite ? BoardConstants.WhiteQueensideCastlingFlag : BoardConstants.BlackQueensideCastlingFlag;
        var queensideTarget = isWhite ? BoardConstants.C1 : BoardConstants.C8;

        if ((queensideCastleSquares & occupancy) == 0 && (board.CastlingRights & queensideFlag) > 0)
            result |= 1ul << queensideTarget;

        // Add moves from result bitboard
        while (result > 0)
        {
            var lowest = ulong.TrailingZeroCount(result);
            moveList.Add(new Move(piece, square, new Square((int)lowest)));
            result &= result - 1;
        }
    }


    private static void GenerateBasicMoves(Piece piece, Square square, ref Board board, List<Move> moveList)
    {
        var moves = GetMovesUlong(piece, square, ref board);
        while (moves > 0)
        {
            var lowest = ulong.TrailingZeroCount(moves);
            moveList.Add(new Move(piece, square, new Square((int)lowest)));
            moves &= moves - 1;
        }
    }

    private static ulong GetMovesUlong(Piece piece, Square square, ref Board board)
    {
        var result = 0ul;
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        result |= MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        result &= ~movingSideOccupancy;
        return result;
    }
}