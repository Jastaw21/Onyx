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
}