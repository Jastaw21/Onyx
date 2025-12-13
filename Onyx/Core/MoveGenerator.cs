namespace Onyx.Core;

public static class MoveGenerator
{
    public static List<Move> GetMoves(Piece piece, Square square, ref Board board)
    {
        var moveList = new List<Move>();
        if (piece.Type != PieceType.Pawn)
        {
            GenerateBasicMoves(piece, square, ref board, moveList);
            GenerateCastlingMoves(piece, square, ref board, moveList);
            
        }
        else
        {
            GeneratePawnMoves(piece, square, ref board, moveList);
            GeneratePawnPromotionMoves(piece, square, ref board, moveList);
        }

        return moveList;
    }

    private static void GeneratePawnMoves(Piece piece, Square square, ref Board board, List<Move> moveList)
    {
        // don't do anything if it's promotion eligible - delegate all promotion logic to GeneratePromotionMoves
        if ((piece.Colour == Colour.White && square.RankIndex == 6) ||
            (piece.Colour == Colour.Black && square.RankIndex == 1))
            return;

        var rawMoveOutput = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        
        var opponentColour = piece.Colour == Colour.White ? Colour.Black : Colour.White;
        var opponentOccupancy = board.Bitboards.OccupancyByColour(opponentColour);

        var normalAttacks = opponentOccupancy & rawMoveOutput;
        if (board.EnPassantSquare.HasValue)
            normalAttacks |= board.EnPassantSquare.Value.Bitboard;

        var pushes = MagicBitboards.MagicBitboards.GetPawnPushes(piece.Colour, square, board.Bitboards.Occupancy());

        var result = pushes | normalAttacks;
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        result &= ~movingSideOccupancy;
        // Add moves from result bitboard
        while (result > 0)
        {
            var lowest = ulong.TrailingZeroCount(result);
            moveList.Add(new Move(piece, square, new Square((int)lowest)));
            result &= result - 1;
        }
        
    }

    private static void GenerateCastlingMoves(Piece piece, Square square, ref Board board, List<Move> moveList)
    {
        if (piece.Type != PieceType.King || board.CastlingRights == 0)
            return;

        var isWhite = piece.Colour == Colour.White;
        var opponentColour = isWhite ? Colour.Black : Colour.White;
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
        {
            var squaresToCheck = kingsideCastleSquares;
            var isAttacked = false;
            while (squaresToCheck != 0ul)
            {
                var lowestBit = ulong.TrailingZeroCount(squaresToCheck);
                if (Referee.IsSquareAttacked(new Square((int)lowestBit), board, opponentColour))
                {
                    isAttacked = true;
                    break;
                }

                squaresToCheck &= squaresToCheck - 1;
            }

            if (!isAttacked)
                result |= 1ul << kingsideTarget;
        }

        // Queenside
        var queensideCastleSquares = isWhite
            ? BoardConstants.WhiteQueenSideCastlingSquares
            : BoardConstants.BlackQueenSideCastlingSquares;
        var queensideFlag =
            isWhite ? BoardConstants.WhiteQueensideCastlingFlag : BoardConstants.BlackQueensideCastlingFlag;
        var queensideTarget = isWhite ? BoardConstants.C1 : BoardConstants.C8;

        if ((queensideCastleSquares & occupancy) == 0 && (board.CastlingRights & queensideFlag) > 0)
        {
            var squaresToCheck = queensideCastleSquares;
            var isAttacked = false;
            while (squaresToCheck != 0ul)
            {
                var lowestBit = ulong.TrailingZeroCount(squaresToCheck);
                if (Referee.IsSquareAttacked(new Square((int)lowestBit), board, opponentColour))
                {
                    isAttacked = true;
                    break;
                }

                squaresToCheck &= squaresToCheck - 1;
            }

            if (!isAttacked)
                result |= 1ul << queensideTarget;
        }

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

    private static void GeneratePawnPromotionMoves(Piece piece, Square square, ref Board board, List<Move> moveList)
    {
        if (piece.Type != PieceType.Pawn)
            return;
        if ((piece.Colour == Colour.White && square.RankIndex != 6) ||
            (piece.Colour == Colour.Black && square.RankIndex != 1))
            return;

        var offset = piece.Colour == Colour.White ? 8 : -8;
        foreach (var promotionType in Piece.PromotionTypes(piece.Colour))
        {
            var targetSquare = new Square(square.SquareIndex + offset);
            var move = new Move(piece, square, targetSquare)
            {
                PromotedPiece = promotionType
            };
            moveList.Add(move);
        }
    }

    private static ulong GetMovesUlong(Piece piece, Square square, ref Board board)
    {
        var result = MagicBitboards.MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        result &= ~movingSideOccupancy;
        return result;
    }
}