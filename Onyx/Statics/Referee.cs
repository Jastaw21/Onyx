using Onyx.Core;
using System;

namespace Onyx.Statics;

public static class Referee
{
    public static bool MoveIsLegal(Move move, ref Board position)
    {
        position.ApplyMove(move,false);
        var result = IsInCheck(move.PieceMoved.Colour, position);
        position.UndoMove(move,false);
        return !result;
    }

    public static bool IsInCheck(Colour colour, Board position)
    {
        var kingBitBoard = position.Bitboards.OccupancyByPiece(Piece.MakePiece(PieceType.King, colour));
        var square = ulong.TrailingZeroCount(kingBitBoard);
        if (square == 64) return false; // no king on the board 
        var attackingColour = colour == Colour.White ? Colour.Black : Colour.White;
        return IsSquareAttacked(new Square((int)square), position, attackingColour);
    }

    public static bool IsCheckmate(Colour colourInCheckmate, Board position)
    {
        if (!IsInCheck(colourInCheckmate, position))
            return false;

        // check each of the pieces they have moves with
        var piecesTheyCanMove = Piece.ByColour(colourInCheckmate);
        foreach (var piece in piecesTheyCanMove)
        {
            var moves = MoveGenerator.GetMoves(piece, position);
            // then see if these moves take the board out of check
            foreach (var move in moves)
            {
                position.ApplyMove(move);
                var isInCheck = IsInCheck(colourInCheckmate, position);
                position.UndoMove(move);
                if (isInCheck)
                    return true;
            }
        }

        return false;
    }

    public static bool IsCheckmate(Board position)
    {
        return IsCheckmate(Colour.White, position) || IsCheckmate(Colour.Black, position);
    }

    public static bool IsSquareAttacked(Square square, Board board, Colour byColour)
    {
        var occupancy = board.Bitboards.Occupancy();
        var squareIndex = square.SquareIndex;     

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
        var pawnColour = byColour == Colour.White ? Colour.Black : Colour.White;
        var pawnsBB = board.Bitboards.OccupancyByPiece(attackingPiece);

        if (square.FileIndex > 0)
        {
            int targetIndex = byColour == Colour.White ? squareIndex - 9 : squareIndex + 7;
            if ((uint)targetIndex < 64)
            {
                if (((1UL << targetIndex) & pawnsBB) != 0UL) return true;
            }
        }

        if (square.FileIndex < 7)
        {
            int targetIndex = byColour == Colour.White ? squareIndex - 7 : squareIndex + 9;
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