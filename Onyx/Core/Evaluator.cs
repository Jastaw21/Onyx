namespace Onyx.Core;

public static class Evaluator
{
    public static int Evaluate(Board board)
    {
        var materialScore = MaterialScore(board);

        return materialScore;
    }

    private static int MaterialScore(Board board)
    {
        var whiteScore = 0;
        var blackScore = 0;
        foreach (var piece in Piece.ByColour(Colour.White))
        {
            whiteScore += (int)ulong.PopCount(board.Bitboards.OccupancyByPiece(piece)) * PieceValues[piece.Type];
        }

        foreach (var piece in Piece.ByColour(Colour.Black))
        {
            blackScore += (int)ulong.PopCount(board.Bitboards.OccupancyByPiece(piece)) * PieceValues[piece.Type];
        }

        return whiteScore - blackScore;
    }
    
    private static readonly Dictionary<PieceType,int> PieceValues = new()
    {
        { PieceType.Pawn, 100 },
        { PieceType.Knight, 300 },
        { PieceType.Bishop, 300 },
        { PieceType.Rook, 500 },
        { PieceType.Queen, 900 },
        { PieceType.King, 0 }
    };
}