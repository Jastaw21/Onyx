namespace Onyx.Core;

public static class Referee
{
    public static bool MoveIsLegal(Move move, ref Board position)
    {
        position.ApplyMove(move);
        return !IsInCheck(move.PieceMoved.Colour, ref position);
    }

    public static bool IsInCheck(Colour colour, ref Board position)
    {
        var kingBitBoard = position.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.King, colour));
        var square = ulong.TrailingZeroCount(kingBitBoard);
        var attackingColour = colour == Colour.White ? Colour.Black : Colour.White;
        return IsSquareAttacked(new Square((int)square), position, attackingColour);
    }

    public static bool IsCheckmate(Colour colourInCheckmate, ref Board position)
    {
        if (!IsInCheck(colourInCheckmate, ref position))
            return false;

        // check each of the pieces they have moves with
        var piecesTheyCanMove = Piece.ByColour(colourInCheckmate);
        foreach (var piece in piecesTheyCanMove)
        {
            var moves = MoveGenerator.GetMoves(piece, ref position);
            // then see if these moves take the board out of check
            foreach (var move in moves)
            {
                position.ApplyMove(move);
                if (IsInCheck(colourInCheckmate, ref position))
                    return true;
            }
        }

        return false;
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

            if ((targetSquare.Bitboard & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Pawn, byColour))) >
                0)
                return true;
        }

        if (square.FileIndex < 7)
        {
            var targetSquare = pawnColour == Colour.Black
                ? new Square(square.SquareIndex - 7)
                : new Square(square.SquareIndex + 9);

            if ((targetSquare.Bitboard & board.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.Pawn, byColour))) >
                0)
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