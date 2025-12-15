namespace Onyx.Core;

using MagicBitboards;

public static class MoveGenerator
{
    public static List<Move> GetMoves(Piece piece, Square square, Board board)
    {
        var moveList = new List<Move>();
        if (piece.Type != PieceType.Pawn)
        {
            GenerateBasicMoves(piece, square, board, moveList);
            GenerateCastlingMoves(piece, square, board, moveList);
        }
        else
        {
            GeneratePawnMoves(piece, square, board, moveList);
            GeneratePawnPromotionMoves(piece, square, board, moveList);
        }

        return moveList;
    }

    public static List<Move> GetMoves(Piece piece, Board board)
    {
        var thisPieceStartSquares = board.Bitboards.OccupancyByPiece(piece);
        List<Move> moves = [];
        while (thisPieceStartSquares > 0)
        {
            var lowestSetBit = ulong.TrailingZeroCount(thisPieceStartSquares);
            var thisSquare = new Square((int)lowestSetBit);
            moves.AddRange(GetMoves(piece, thisSquare, board));

            thisPieceStartSquares &= thisPieceStartSquares - 1;
        }

        return moves;
    }

    public static List<Move> GetMoves(Colour colour, Board board)
    {
        List<Move> moves = [];
        foreach (var piece in Piece.ByColour(colour))
        {
            moves.AddRange(GetMoves(piece, board));
        }

        return moves;
    }

    private static void GeneratePawnMoves(Piece piece, Square square, Board board, List<Move> moveList)
    {
        // don't do anything if it's promotion eligible - delegate all promotion logic to GeneratePromotionMoves
        if ((piece.Colour == Colour.White && square.RankIndex == 6) ||
            (piece.Colour == Colour.Black && square.RankIndex == 1))
            return;

        var rawMoveOutput = MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var pushes = MagicBitboards.GetPawnPushes(piece.Colour, square, board.Bitboards.Occupancy());
        var attacks = rawMoveOutput ^ pushes;

        var opponentColour = piece.Colour == Colour.White ? Colour.Black : Colour.White;
        var opponentOccupancy = board.Bitboards.OccupancyByColour(opponentColour);


        var normalAttacks = opponentOccupancy & attacks;

        // the board has a viable en passant square, and we're on an appropriate rank
        if (board.EnPassantSquare.HasValue && Math.Abs(board.EnPassantSquare.Value.FileIndex - square.FileIndex) == 1)
        {
            var pawnHomeRank = piece.Colour == Colour.Black ? 3 : 4;
            if (square.RankIndex == pawnHomeRank)
                normalAttacks |= board.EnPassantSquare.Value.Bitboard;
        }

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

    private static void GenerateCastlingMoves(Piece piece, Square square, Board board, List<Move> moveList)
    {
        if (piece.Type != PieceType.King || board.CastlingRights == 0)
            return;

        var isWhite = piece.Colour == Colour.White;
        var expectedSquare = isWhite ? BoardConstants.E1 : BoardConstants.E8;

        if (square.SquareIndex != expectedSquare)
            return;

        var opponentColour = isWhite ? Colour.Black : Colour.White;
        var occupancy = board.Bitboards.Occupancy();

        // Try kingside
        TryCastling(
            board,
            piece,
            square,
            isWhite ? BoardConstants.WhiteKingsideCastlingFlag : BoardConstants.BlackKingsideCastlingFlag,
            isWhite ? BoardConstants.WhiteKingSideCastlingSquares : BoardConstants.BlackKingSideCastlingSquares,
            isWhite ? BoardConstants.G1 : BoardConstants.G8,
            occupancy,
            opponentColour,
            moveList
        );

        // Try queenside
        TryCastling(
            board,
            piece,
            square,
            isWhite ? BoardConstants.WhiteQueensideCastlingFlag : BoardConstants.BlackQueensideCastlingFlag,
            isWhite ? BoardConstants.WhiteQueenSideCastlingSquares : BoardConstants.BlackQueenSideCastlingSquares,
            isWhite ? BoardConstants.C1 : BoardConstants.C8,
            occupancy,
            opponentColour,
            moveList
        );
    }

    private static void TryCastling(
        Board board,
        Piece piece,
        Square fromSquare,
        int castlingFlag,
        ulong requiredEmptySquares,
        int targetSquare,
        ulong occupancy,
        Colour opponentColour,
        List<Move> moveList)
    {
        // Check if we have the right
        if ((board.CastlingRights & castlingFlag) == 0)
            return;

        // Check if path is clear
        if ((requiredEmptySquares & occupancy) != 0)
            return;

        // Check if any square the king passes through is attacked
        var squaresToCheck = requiredEmptySquares;
        while (squaresToCheck != 0)
        {
            var squareIndex = (int)ulong.TrailingZeroCount(squaresToCheck);

            // Don't check b1/b8 for attack (queenside rook square)
            if (squareIndex != BoardConstants.B1 && squareIndex != BoardConstants.B8)
            {
                if (Referee.IsSquareAttacked(new Square(squareIndex), board, opponentColour))
                    return;
            }

            squaresToCheck &= squaresToCheck - 1;
        }

        // All checks passed, add the move
        moveList.Add(new Move(piece, fromSquare, new Square(targetSquare)));
    }

    private static void GenerateBasicMoves(Piece piece, Square square, Board board, List<Move> moveList)
    {
        var moves = GetMovesUlong(piece, square, board);
        while (moves > 0)
        {
            var lowest = ulong.TrailingZeroCount(moves);
            moveList.Add(new Move(piece, square, new Square((int)lowest)));
            moves &= moves - 1;
        }
    }

    private static void GeneratePawnPromotionMoves(Piece piece, Square square, Board board, List<Move> moveList)
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

    private static ulong GetMovesUlong(Piece piece, Square square, Board board)
    {
        var result = MagicBitboards.GetMovesByPiece(piece, square, board.Bitboards.Occupancy());
        var movingSideOccupancy = board.Bitboards.OccupancyByColour(piece.Colour);
        result &= ~movingSideOccupancy;
        return result;
    }
}