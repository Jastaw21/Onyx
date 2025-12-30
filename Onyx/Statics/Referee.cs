using Onyx.Core;

namespace Onyx.Statics;

public static class Referee
{
    public static bool MoveIsLegal(Move move, Board position)
    {
        if (move.PieceMoved.Type == PieceType.King)
            return FullLegalityCheck(move, position);

        // if the board isn't in check, can just check for pinned pieces
        if (!IsInCheck(position.TurnToMove, position))
        {
            var pieceMovedColour = move.PieceMoved.Colour;
            var relevantKing = pieceMovedColour == Colour.White ? Piece.WK : Piece.BK;
            var kingBoard = position.Bitboards.OccupancyByPiece(relevantKing);
            var kingSquare = (int)ulong.TrailingZeroCount(kingBoard);
            
            // Should not happen in a valid game, but for safety in tests
            if (kingSquare == 64) return FullLegalityCheck(move, position);
            
            return !IsPinnedToKing(move.From, kingSquare, pieceMovedColour,
                position, move.To);
        }

        // in check
        return FullLegalityCheck(move, position);
    }

    private static bool FullLegalityCheck(Move move, Board position)
    {
        position.ApplyMove(move, false);
        var result = IsInCheck(move.PieceMoved.Colour, position);
        position.UndoMove(move, false);
        return !result;
    }

    private static bool IsPinnedToKing(int pinnedPieceSquare, int kingSquare, Colour kingColour, Board position,
        int squareMoveTo)
    {
        // 1. Get the ray between the piece and the king.
        var rayBetween = RankAndFileHelpers.GetRayBetween(pinnedPieceSquare, kingSquare);
        if (rayBetween == 0) return false;

        // 2. See if there is an attacker behind the piece on this ray.
        var occupancyWithoutPinnedPiece = position.Bitboards.Occupancy() & ~(1UL << pinnedPieceSquare);

        // Check Diagonals
        var diagAttacks =
            MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WB, kingSquare, occupancyWithoutPinnedPiece);
        var relevantBishop = kingColour == Colour.White ? Piece.BB : Piece.WB;
        var relevantQueen = kingColour == Colour.White ? Piece.BQ : Piece.WQ;
        var diagAttackers =
            (position.Bitboards.OccupancyByPiece(relevantQueen) | position.Bitboards.OccupancyByPiece(relevantBishop)) &
            diagAttacks;

        if (diagAttackers != 0)
        {
            // If the piece is on a diagonal ray and there's a diagonal attacker...
            // it can ONLY move if the destination is also on the ray between the king and that attacker.
            int attackerSquare = (int)ulong.TrailingZeroCount(diagAttackers);
            var pinRay = RankAndFileHelpers.GetRayBetween(kingSquare, attackerSquare);
            return (pinRay & (1ul << squareMoveTo)) == 0;
        }

        // Check Straights
        var straightAttacks =
            MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WR, kingSquare, occupancyWithoutPinnedPiece);
        var relevantRook = kingColour == Colour.White ? Piece.BR : Piece.WR;
        var straightAttackers =
            (position.Bitboards.OccupancyByPiece(relevantQueen) | position.Bitboards.OccupancyByPiece(relevantRook)) &
            straightAttacks;

        if (straightAttackers != 0)
        {
            int attackerSquare = (int)ulong.TrailingZeroCount(straightAttackers);
            var pinRay = RankAndFileHelpers.GetRayBetween(kingSquare, attackerSquare);
            return (pinRay & (1ul << squareMoveTo)) == 0;
        }

        return false;
    }

    public static bool IsInCheck(Colour colour, Board position)
    {
        var kingBitBoard = position.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.King, colour));
        var square = ulong.TrailingZeroCount(kingBitBoard);
        if (square == 64) return false; // no king on the board 
        var attackingColour = colour == Colour.White ? Colour.Black : Colour.White;
        return IsSquareAttacked((int)square, position, attackingColour);
    }

    public static bool IsCheckmate(Colour colourInCheckmate, Board position)
    {
        if (!IsInCheck(colourInCheckmate, position))
            return false;

        // check each of the pieces they have moves with
        Span<Move> moveBuffer = stackalloc Move[256];
        int moveCount = MoveGenerator.GetMoves(colourInCheckmate, position, moveBuffer);

        // then see if these moves take the board out of check
        for (int i = 0; i < moveCount; i++)
        {
            var move = moveBuffer[i];
            position.ApplyMove(move);
            var isInCheck = IsInCheck(colourInCheckmate, position);
            position.UndoMove(move);
            if (!isInCheck)
                return false;
        }

        return true;
    }

    public static bool IsCheckmate(Board position)
    {
        return IsCheckmate(Colour.White, position) || IsCheckmate(Colour.Black, position);
    }

    public static bool IsSquareAttacked(int square, Board board, Colour byColour)
    {
        var occupancy = board.Bitboards.Occupancy();


        // what squares could a bishop attack from where the king currently is?
        var diagAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WB, square, occupancy);

        // look at the piece types (queen and bishop) that could launch these attacks
        // pre calc the attackers to save makePieceCalls
        var relevantBishop = byColour == Colour.White ? Piece.WB : Piece.BB;
        var relevantQueen = byColour == Colour.White ? Piece.WQ : Piece.BQ;

        // check the pieces one by one to save some cycles if one does match
        if ((diagAttacks & board.Bitboards.OccupancyByPiece(relevantBishop)) > 0)
            return true;

        var queenOccupancy = board.Bitboards.OccupancyByPiece(relevantQueen); // precache as used below
        if ((diagAttacks & queenOccupancy) > 0)
            return true;

        // pre calc the attackers to save makePieceCalls
        var relevantRook = byColour == Colour.White ? Piece.WR : Piece.BR;
        var straightAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WR, square, occupancy);
        if ((straightAttacks & queenOccupancy) > 0)
            return true;
        var rookOccupancy = board.Bitboards.OccupancyByPiece(relevantRook);
        if ((straightAttacks & rookOccupancy) > 0)
            return true;

        var knightPiece = byColour == Colour.White ? Piece.WN : Piece.BN;
        var knightAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WN, square, occupancy);
        if ((knightAttacks & board.Bitboards.OccupancyByPiece(knightPiece)) > 0)
            return true;

        // check pawn attacks
        var attackingPiece = byColour == Colour.White ? Piece.WP : Piece.BP;
        var pawnsBB = board.Bitboards.OccupancyByPiece(attackingPiece);

        var fileIndex = RankAndFileHelpers.FileIndex(square);
        if (fileIndex > 0)
        {
            int targetIndex = byColour == Colour.White ? square - 9 : square + 7;
            if ((uint)targetIndex < 64)
            {
                if (((1UL << targetIndex) & pawnsBB) != 0UL) return true;
            }
        }

        if (fileIndex < 7)
        {
            int targetIndex = byColour == Colour.White ? square - 7 : square + 9;
            if ((uint)targetIndex < 64)
            {
                if (((1UL << targetIndex) & pawnsBB) != 0UL) return true;
            }
        }

        var relevantKing = byColour == Colour.White ? Piece.WK : Piece.BK;
        var kingAttacks = MagicBitboards.MagicBitboards.GetMovesByPiece(Piece.WK, square, occupancy);
        if ((kingAttacks & board.Bitboards.OccupancyByPiece(relevantKing)) > 0)
            return true;


        return false;
    }
}