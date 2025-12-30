using Onyx.Core;


namespace Onyx.Statics;

public static class Evaluator
{
    public static void SortMoves(Span<Move> moves, Move? transpositionTableMove)
    {
        moves.Sort((move, move1) =>
        {
            var aScore = move.IsPromotion ? 1:0;
            var bScore = move.IsPromotion ? 1:0;

            if (transpositionTableMove.HasValue)
            {
                if (move.Notation == transpositionTableMove.Value.Notation)
                    return int.MaxValue;
                if (move1.Notation == transpositionTableMove.Value.Notation)
                    return int.MinValue;
            }

            return aScore.CompareTo(bScore);
        });
    }
    public static int Evaluate(Board board)
    {
        var materialandPsScore = MaterialAndPsScore(board);
       
        //var mobilityScore = MobilityScore(board);
        var bishopPairScore = BishopPairScore(board);
        return materialandPsScore + bishopPairScore; //+ mobilityScore;
    }

    private static int MaterialAndPsScore(Board board)
    {
        var whiteScore = 0;
        var blackScore = 0;
        foreach (var piece in Pc._whitePieces)
        {
            whiteScore += (int)ulong.PopCount(board.Bitboards.OccupancyByPiece(piece)) * PieceValues[Pc.PieceTypeIndex(piece)];
            
            var placements = board.Bitboards.OccupancyByPiece(piece);
            while (placements > 0)
            {
                var bottomSetBit = ulong.TrailingZeroCount(placements);
                var scores = getArray(piece);
                whiteScore += scores[bottomSetBit];
                placements &= placements - 1;
            }
        }

        foreach (var piece in Pc._blackPieces)
        {
            blackScore += (int)ulong.PopCount(board.Bitboards.OccupancyByPiece(piece)) * PieceValues[Pc.PieceTypeIndex(piece)];
            
            var placements = board.Bitboards.OccupancyByPiece(piece);
            while (placements > 0)
            {
                var bottomSetBit = ulong.TrailingZeroCount(placements);
                var scores = getArray(piece);
                blackScore += scores[bottomSetBit];
                placements &= placements - 1;
            }
        }

        var score = whiteScore - blackScore;
        return board.WhiteToMove ? score : -score;
    }

    private static int BishopPairScore(Board board)
    {
        var whiteBishops = ulong.PopCount(board.Bitboards.OccupancyByPiece(Pc.WB));
        var blackBishops = ulong.PopCount(board.Bitboards.OccupancyByPiece(Pc.BB));

        var whiteScore = whiteBishops >= 2 ? 50 : 0;
        var blackScore = blackBishops >= 2 ? 50 : 0;
        var score = whiteScore - blackScore;
        return board.WhiteToMove  ? score : -score;
    }

    // private static int MobilityScore(Board board)
    // {
    //     var boardTurnToMove = board.TurnToMove;
    //     board.TurnToMove = Colour.White;
    //     var whiteMoves = MoveGenerator.GetLegalMoves(board).Count;
    //     board.TurnToMove = Colour.Black;
    //     var blackMoves = MoveGenerator.GetLegalMoves(board).Count;
    //     board.TurnToMove = boardTurnToMove;
    //     var score = whiteMoves - blackMoves;
    //     return board.TurnToMove == Colour.White ? score : -score;
    // }

    // pawn, knight, bishop, rook, king, queen
    private static readonly int[] PieceValues = [100, 300, 320, 500, 0, 900];

    private static readonly int[] PawnScores =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
        5, 5, 10, 25, 25, 10, 5, 5,
        0, 0, 0, 20, 20, 0, 0, 0,
        5, -5, -10, 0, 0, -10, -5, 5,
        5, 10, 10, -20, -20, 10, 10, 5,
        0, 0, 0, 0, 0, 0, 0, 0
    ];
    
    private static readonly int[] PawnsEnd = {
        0,   0,   0,   0,   0,   0,   0,   0,
        80,  80,  80,  80,  80,  80,  80,  80,
        50,  50,  50,  50,  50,  50,  50,  50,
        30,  30,  30,  30,  30,  30,  30,  30,
        20,  20,  20,  20,  20,  20,  20,  20,
        10,  10,  10,  10,  10,  10,  10,  10,
        10,  10,  10,  10,  10,  10,  10,  10,
        0,   0,   0,   0,   0,   0,   0,   0
    };
    private static readonly int[] BishopScores =
    [
        -10, -10, -10, -10, -10, -10, -10, -10,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -10, 0, 15, 10, 10, 15, 0, -10,
        -10, 5, 5, 10, 10, 5, 5, -10,
        -10, 0, 10, 10, 10, 10, 0, -10,
        -10, 10, 10, 10, 10, 10, 10, -10,
        -10, 15, 0, 0, 0, 0, 15, -10,
        -10, -10, -10, -10, -10, -10, -10, -10,
    ];

    private static readonly int[] KnightScores =
    [
        -50, -40, -30, -30, -30, -30, -40, -50,
        -40, -20, 0, 0, 0, 0, -20, -40,
        -30, 0, 10, 15, 15, 10, 0, -30,
        -30, 5, 15, 20, 20, 15, 5, -30,
        -30, 0, 15, 20, 20, 15, 0, -30,
        -30, 5, 10, 15, 15, 10, 5, -30,
        -40, -20, 0, 5, 5, 0, -20, -40,
        -50, -40, -30, -30, -30, -30, -40, -50,
    ];

    private static readonly int[] QueenScores =
    [
        -20, -10, -10, -5, -5, -10, -10, -20,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -10, 0, 5, 5, 5, 5, 0, -10,
        -5, 0, 5, 5, 5, 5, 0, -5,
        0, 0, 5, 5, 5, 5, 0, -5,
        -10, 5, 5, 5, 5, 5, 0, -10,
        -10, 0, 5, 0, 0, 0, 0, -10,
        -20, -10, -10, -5, -5, -10, -10, -20
    ];

    private static readonly int[] RookScores =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        5, 10, 10, 10, 10, 10, 10, 5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        0, 0, 0, 5, 5, 0, 0, 0
    ];

    private static readonly int[] ZeroScores =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
    ];

    private static int[] getArray(sbyte piece)
    {
        var type = Pc.PieceType(piece);
        return type switch
        {
            Pc.Pawn => PawnScores,
            Pc.Knight => KnightScores,
            Pc.Bishop => BishopScores,
            Pc.Queen => QueenScores,
            Pc.Rook => RookScores,
            _ => ZeroScores
        };
    }
}