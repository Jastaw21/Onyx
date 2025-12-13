namespace Onyx;

public static class Referee
{
    public static bool MoveIsPseudoLegal(Move move, ref Board position)
    {
        var movingSideOccupancy = position.Bitboards.OccupancyByColour(move.PieceMoved.Colour);
        var allMoves = MagicBitboards.GetMovesByPiece(move.PieceMoved, move.From, position.Bitboards.Occupancy());
        var moveTo = move.To.Bitboard;

        // can't go to any of the places
        if (!((moveTo & allMoves) > 0))
            return false;

        // can't go to own square
        if ((moveTo & movingSideOccupancy) > 0)
            return false;

        return true;
    }

    public static bool IsSquareAttacked(Square square, Board board, Colour byColour)
    {
        var occupancy = board.Bitboards.Occupancy();

        var knightAttacks = MagicBitboards.GetMovesByPiece(Piece.WN, square, occupancy);
        if ((knightAttacks & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.King, byColour))) > 0)
            return true;

        var diagAttacks = MagicBitboards.GetMovesByPiece(Piece.WB, square, occupancy);
        if ((diagAttacks & board.Bitboards.OccupancyByColour(byColour)) > 0)
            return true;

        var straightAttacks = MagicBitboards.GetMovesByPiece(Piece.WR, square, occupancy);
        if ((straightAttacks & board.Bitboards.OccupancyByColour(byColour)) > 0)
            return true;

        var kingAttacks = MagicBitboards.GetMovesByPiece(Piece.WK, square, occupancy);
        return true;
    }
}