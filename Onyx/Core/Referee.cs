namespace Onyx.Core;

public static class Referee
{
    public static bool MoveIsPseudoLegal(Move move, ref Board position)
    {
        var movingSideOccupancy = position.Bitboards.OccupancyByColour(move.PieceMoved.Colour);
        var allMoves = MagicBitboards.MagicBitboards.GetMovesByPiece(move.PieceMoved, move.From, position.Bitboards.Occupancy());
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

        var pawnColour = byColour == Colour.White ? Colour.Black : Colour.White;
        if (square.FileIndex > 0)
        {
            var targetSquare = pawnColour == Colour.Black
                ? new Square(square.SquareIndex - 9)
                : new Square(square.SquareIndex + 7);
            
            if ((targetSquare.Bitboard & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Pawn, byColour))) > 0)
                return true;
        }
        if (square.FileIndex < 7)
        {
            var targetSquare = pawnColour == Colour.Black
                ? new Square(square.SquareIndex - 7)
                : new Square(square.SquareIndex + 9);
            
            if ((targetSquare.Bitboard & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Pawn, byColour))) > 0)
                return true;
        }

        var knightAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WN, square, occupancy);
        if ((knightAttacks & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Knight, byColour))) > 0)
            return true;

        var diagAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WB, square, occupancy);
        var diagAttackers = board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Bishop, byColour)) |
                            board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Queen, byColour));

        if ((diagAttacks & diagAttackers) > 0)
            return true;

        var straightAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WR, square, occupancy);
        var straightAttackers = board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Rook, byColour)) |
                                board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Queen, byColour));
        if ((straightAttacks & straightAttackers) > 0)
            return true;

        var kingAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WK, square, occupancy);
        if ((kingAttacks & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.King, byColour))) > 0)
            return true;
        
        
        return false;
    }
}