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
        if (piece.Type != PieceType.King)
            return;

        if (board.CastlingRights == 0)
            return;

        var result = 0ul;
        bool kingSideEmpty;
        bool queenSideEmpty;
        switch (piece.Colour)
        {
            case Colour.White:
                if (square.SquareIndex != BoardConstants.E1) return;
                kingSideEmpty = (BoardConstants.WhiteKingSideCastlingSquares & board.Bitboards.Occupancy()) == 0;
                queenSideEmpty = (BoardConstants.WhiteKingSideCastlingSquares & board.Bitboards.Occupancy()) == 0;
                if (kingSideEmpty && ((board.CastlingRights & BoardConstants.WhiteKingsideCastlingFlag) > 0)) 
                    result |= 1ul << BoardConstants.G1;
                
                if (queenSideEmpty && ((board.CastlingRights & BoardConstants.WhiteQueensideCastlingFlag) > 0)) 
                    result |= 1ul << BoardConstants.C1;
                
                break;

            case Colour.Black:
                if (square.SquareIndex != BoardConstants.E8) return;
                kingSideEmpty = (BoardConstants.BlackKingSideCastlingSquares & board.Bitboards.Occupancy()) == 0;
                queenSideEmpty = (BoardConstants.BlackKingSideCastlingSquares & board.Bitboards.Occupancy()) == 0;
                
                if (kingSideEmpty && ((board.CastlingRights & BoardConstants.BlackKingsideCastlingFlag) > 0)) 
                    result |= 1ul << BoardConstants.G8;
                
                if (queenSideEmpty && ((board.CastlingRights & BoardConstants.BlackQueensideCastlingFlag) > 0)) 
                    result |= 1ul << BoardConstants.C8;
                break;
        }
        
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