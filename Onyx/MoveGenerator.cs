namespace Onyx;

public static class MoveGenerator
{
    public static List<Move> GetMoves(Piece piece, Square square, ref Board board)
    {
        var moveList = new List<Move>();
        var moves = GetMovesUlong(piece, square, ref board);
        while (moves > 0)
        {
            var lowest = ulong.TrailingZeroCount(moves);
            moveList.Add(new Move(piece, square, new Square((int)lowest)));
            moves &= moves - 1;
        }

        return moveList;
    }

    private static ulong GetMovesUlong(Piece piece, Square square, ref Board board)
    {
        var result = 0ul;
        result |= MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        return result;
    }
}