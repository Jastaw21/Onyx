using Onyx.Core;
using System;


namespace Onyx.Statics;

internal struct MaterialEvaluation
{
    public int PieceCount =>
        Bishops + Knights + Rooks + Queens + Pawns + 1; // always a king

    public int MaterialScore = 0;

    public int Pawns = 0;
    public int Bishops = 0;
    public int Knights = 0;
    public int Rooks = 0;
    public int Queens = 0;

    public MaterialEvaluation()
    {
    }

    public int BishopPairScore => Bishops >= 2 ? 40 : 0;

    public float EndGameRatio()
    {
        const int queenEndgameWeight = 45;
        const int rookEndgameWeight = 20;
        const int bishopEndgameWeight = 10;
        const int knightEndgameWeight = 10;

        // start with all these pieces
        var startScore = 2 * rookEndgameWeight + 2 * knightEndgameWeight + 2 * bishopEndgameWeight + queenEndgameWeight;
        var remainingScore =
            rookEndgameWeight * Rooks
            + knightEndgameWeight * Knights
            + bishopEndgameWeight * Bishops
            + queenEndgameWeight * Queens;

        var delta = 1 - MathF.Min(1, remainingScore / (float)startScore);
        return delta;
    }
}

public static class Evaluator
{
    
   
    private static int GetMoveScore(Move move, Move?[,]? killerMoves, int ply)
    {
        var score = 0;
        if (move.IsPromotion) score += 100000;

        if (move.CapturedPiece != 0)
        {
            var victimPiece = PieceValues[PieceTypes.PieceTypeIndex(move.CapturedPiece)];
            var attackerPiece = PieceValues[PieceTypes.PieceTypeIndex(move.PieceMoved)];
            score += 12000 + (victimPiece * 10 - attackerPiece);
        }

        if (killerMoves == null) return score;
        if (killerMoves[ply, 0] == move)
            return 7000;
        if (killerMoves[ply, 1] == move)
            return 2000;

        return score;
    }

    public static void SortMoves(Span<Move> moves, Move transpositionTableMove, Move?[,] killerMoves, int ply)
    {
        var len = moves.Length;
        if (len <= 1)  return;

        Span<int> scores = stackalloc int[len];
        var hasTTMove = transpositionTableMove.Data > 0;

        for (var i = 0; i < len; i++)
        {
            if (hasTTMove && moves[i] == transpositionTableMove)
            {
                scores[i] = int.MaxValue;
                continue;
            }
            scores[i] = GetMoveScore(moves[i], killerMoves, ply);
        }
        
        PerformSort(moves, scores, 0, len - 1);

    }

    static void PerformSort(Span<Move> moves, Span<int> scores, int left, int right)
    {

        if (left < right) QuickSort(moves,scores,left, right);

    }

    static void InsertionSort(Span<Move> moves, Span<int> scores,int l, int r)
    {
        for (var i = l + 1; i <= r; i++)
        {
            var keyMove = moves[i];
            var keyScore = scores[i];
            var j = i - 1;
            while (j >= l && scores[j] < keyScore) // descending
            {
                moves[j + 1] = moves[j];
                scores[j + 1] = scores[j];
                j--;
            }
            moves[j + 1] = keyMove;
            scores[j + 1] = keyScore;
        }
    }


    static void QuickSort(Span<Move> moves, Span<int> scores,int l, int r)
    {
        const int InsertionThreshold = 10;
        if (r - l <= InsertionThreshold)
        {
            InsertionSort( moves, scores,l, r);
            return;
        }

        // median-of-three pivot
        var mid = (l + r) >> 1;
        if (scores[l] < scores[mid]) Swap(l, mid, moves,scores);
        if (scores[l] < scores[r]) Swap(l, r, moves,scores);
        if (scores[mid] < scores[r]) Swap(mid, r, moves,scores);

        var pivot = scores[mid];
        // move pivot to r-1
        Swap(mid, r - 1, moves, scores);
        var i = l;
        var j = r - 1;

        while (true)
        {
            while (scores[++i] > pivot) { }
            while (scores[--j] < pivot) { }
            if (i >= j) break;
            Swap(i, j, moves, scores);
        }

        // restore pivot
        Swap(i, r - 1, moves, scores);

        if (i - 1 - l > 0) QuickSort(moves,scores,l, i - 1);
        if (r - (i + 1) > 0) QuickSort(moves,scores, i + 1, r);
    }

    private static void Swap(int a, int b, Span<Move> moves, Span<int> scores)
    {
        var tmpMove = moves[a];
        moves[a] = moves[b];
        moves[b] = tmpMove;

        var tmpScore = scores[a];
        scores[a] = scores[b];
        scores[b] = tmpScore;
    }

    public static int Evaluate(Position board)
    {

        var whiteMaterial = EvaluateMaterial(board, true);
        var blackMaterial = EvaluateMaterial(board, false);
        var score = 0;

        score += whiteMaterial.MaterialScore - blackMaterial.MaterialScore;
        score += whiteMaterial.BishopPairScore - blackMaterial.BishopPairScore;
        score += PieceSquareScore(board, blackMaterial.EndGameRatio(), true) - PieceSquareScore(board, whiteMaterial.EndGameRatio(), false);
        score += KingSafetyScore(board, true) - KingSafetyScore(board, false);
        //score += PawnStructureScore(board, true) - PawnStructureScore(board, false); ;

        return board.WhiteToMove ? score : -score;
    }

    public static int KingSafetyScore(Position board, bool forWhite)
    {
        var kingPiece = forWhite ? PieceTypes.WK : PieceTypes.BK;
        var pawnPiece = forWhite ? PieceTypes.WP : PieceTypes.BP;
        var kingBoard = board.Bitboards.OccupancyByPiece(kingPiece);

        var kingSquare = (int)ulong.TrailingZeroCount(kingBoard);
        var kingShields =
            MagicBitboards.MagicBitboards.GetKingShields(forWhite, kingSquare);

        var possibleShields = (int)ulong.PopCount(kingShields);
        var pawnPlacement = board.Bitboards.OccupancyByPiece(pawnPiece);
        var actualShields = (int)ulong.PopCount(pawnPlacement & kingShields);

        var pawnShieldScore = (possibleShields - actualShields) * -20;

        var kingFile = RankAndFile.FileIndex((int)ulong.TrailingZeroCount(kingBoard));
        var pawns = board.Bitboards.OccupancyByPiece(PieceTypes.WP) | board.Bitboards.OccupancyByPiece(PieceTypes.BP);
        var openFilePenalty = BoardHelpers.FileIsOpen(kingFile, pawns) ? -30 : 0;

        return pawnShieldScore + openFilePenalty;
    }

    private static MaterialEvaluation EvaluateMaterial(Position board, bool forWhite)
    {
        var materialEvaluation = new MaterialEvaluation();
        var pieces = forWhite ? PieceTypes._whitePieces : PieceTypes._blackPieces;
        foreach (var piece in pieces)
        {
            var occupancyByPiece = board.Bitboards.OccupancyByPiece(piece);
            var pieceCount = (int)ulong.PopCount(occupancyByPiece);
            
            materialEvaluation.MaterialScore += pieceCount * PieceValues[PieceTypes.PieceTypeIndex(piece)];

            switch (PieceTypes.PieceType(piece))
            {
                case PieceTypes.Pawn:
                    materialEvaluation.Pawns += pieceCount;
                    break;
                case PieceTypes.Knight:
                    materialEvaluation.Knights += pieceCount;
                    break;
                case PieceTypes.Rook:
                    materialEvaluation.Rooks += pieceCount;
                    break;
                case PieceTypes.Queen:
                    materialEvaluation.Queens += pieceCount;
                    break;
                case PieceTypes.Bishop:
                    materialEvaluation.Bishops += pieceCount;
                    break;
            }
        }

        return materialEvaluation;
    }

    private static int PieceSquareScore(Position board, float enemyEndGameScale, bool forWhite)
    {
        var score = 0;
        var pieces = forWhite ? PieceTypes._whitePieces : PieceTypes._blackPieces;
        foreach (var piece in pieces)
        {
            score += PieceSquareScoreByPiece(board, piece, enemyEndGameScale);
        }

        return score;
    }

    private static int PieceSquareScoreByPiece(Position board, byte piece, float enemyEndGameScale)
    {
        var bitboardIndex = PieceTypes.BitboardIndex(piece);
        var occupancy = board.Bitboards.Boards[bitboardIndex];

        var score = 0;
        while (occupancy > 0)
        {
            var lowestSetBit = ulong.TrailingZeroCount(occupancy);
            var square = (int)lowestSetBit;
            if (PieceTypes.PieceType(piece) == PieceTypes.Pawn)
            {
                // ReSharper disable once RedundantArgumentDefaultValue
                var earlyGameScore = GetPieceValueOnSquare(square, piece, false);
                var endGameScore = GetPieceValueOnSquare(square, piece, true);
                score += (int)(endGameScore * enemyEndGameScale + earlyGameScore * (1 - enemyEndGameScale));
            }
            else score += GetPieceValueOnSquare(square, piece);

            occupancy &= occupancy - 1;
        }

        return score;
    }

    // pawn, knight, bishop, rook, king, queen
    private static readonly int[] PieceValues = [100, 300, 320, 500, 0, 900];

    public static int GetPieceValueOnSquare(int square, byte piece, bool endGame = false)
    {
        var index = PieceTypes.IsWhite(piece) ? square ^ 56 : square;
        return GetArray(piece, endGame)[index];
    }

    // tables are laid out like looking at a board from white's perspective
    // @formatter:off
        private static readonly int[] PawnStart =
    [
          0,   0,   0,   0,   0,   0,   0,   0,
         50,  50,  50,  50,  50,  50,  50,  50,
         10,  10,  20,  30,  30,  20,  10,  10,
          5,   5,  10,  25,  25,  10,   5,   5,
          0,   0,   0,  20,  20,   0,   0,   0,
          5,  -5, -10,   0,   0, -10,  -5,   5,
          5,  10,  10, -20, -20,  10,  10,   5,
          0,   0,   0,   0,   0,   0,   0,   0
    ];

    private static readonly int[] PawnEnd =
    [
          0,   0,   0,   0,   0,   0,   0,   0,
         80,  80,  80,  80,  80,  80,  80,  80,
         50,  50,  50,  50,  50,  50,  50,  50,
         30,  30,  30,  30,  30,  30,  30,  30,
         20,  20,  20,  20,  20,  20,  20,  20,
         10,  10,  10,  10,  10,  10,  10,  10,
         10,  10,  10,  10,  10,  10,  10,  10,
          0,   0,   0,   0,   0,   0,   0,   0
    ];

    private static readonly int[] BishopStart =
    [
        -10, -10, -10, -10, -10, -10, -10, -10,
        -10,   0,   0,   0,   0,   0,   0, -10,
        -10,   0,  15,  10,  10,  15,   0, -10,
        -10,   5,   5,  10,  10,   5,   5, -10,
        -10,   0,  10,  10,  10,  10,   0, -10,
        -10,  10,  10,  10,  10,  10,  10, -10,
        -10,  15,   0,   0,   0,   0,  15, -10,
        -10, -10, -10, -10, -10, -10, -10, -10
    ];

    private static readonly int[] KnightScores =
    [
        -50, -40, -30, -30, -30, -30, -40, -50,
        -40, -20,   0,   0,   0,   0, -20, -40,
        -30,   0,  10,  15,  15,  10,   0, -30,
        -30,   5,  15,  20,  20,  15,   5, -30,
        -30,   0,  15,  20,  20,  15,   0, -30,
        -30,   5,  10,  15,  15,  10,   5, -30,
        -40, -20,   0,   5,   5,   0, -20, -40,
        -50, -40, -30, -30, -30, -30, -40, -50
    ];

    private static readonly int[] QueenScores =
    [
        -20, -12, -10,  -5,  -5, -10, -12, -20,
        -10,  -5,   0,   2,   2,   0,  -5, -10,
        -10,   0,   5,   6,   6,   5,   0, -10,
         -5,   0,   5,   7,   7,   5,   0,  -5,
         -5,   0,   5,   7,   7,   5,   0,  -5,
        -10,   5,   5,   5,   5,   5,   0, -10,
        -10,   1,   2,   3,   3,   2,   1, -10,
        -20, -12, -10,  -5,  -5, -10, -12, -20
    ];

    private static readonly int[] KingStart =
    [
        -80, -70, -70, -70, -70, -70, -70, -80,
        -60, -60, -60, -60, -60, -60, -60, -60,
        -40, -50, -50, -60, -60, -50, -50, -40,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -20, -30, -30, -40, -40, -30, -30, -20,
        -10, -20, -20, -20, -20, -20, -20, -10,
         20,  20,  -5,  -5,  -5,  -5,  20,  20,
         20,  30,  10,   0,   0,  10,  30,  20
    ];

    private static readonly int[] KingEnd =
    [
        -20, -10, -10, -10, -10, -10, -10, -20,
         -5,   0,   5,   5,   5,   5,   0,  -5,
        -10,  -5,  20,  30,  30,  20,  -5, -10,
        -15, -10,  35,  45,  45,  35, -10, -15,
        -20, -15,  30,  40,  40,  30, -15, -20,
        -25, -20,  20,  25,  25,  20, -20, -25,
        -30, -25,   0,   0,   0,   0, -25, -30,
        -50, -30, -30, -30, -30, -30, -30, -50
    ];

    private static readonly int[] RookStart =
    [
          0,   0,   0,   0,   0,   0,   0,   0,
          5,  10,  10,  10,  10,  10,  10,   5,
         -5,   0,   0,   0,   0,   0,   0,  -5,
         -5,   0,   0,   0,   0,   0,   0,  -5,
         -5,   0,   0,   0,   0,   0,   0,  -5,
         -5,   0,   0,   0,   0,   0,   0,  -5,
         -5,   0,   0,   0,   0,   0,   0,  -5,
          0,   0,   0,   5,   5,   0,   0,   0
    ];

    private static readonly int[] RookEnd =
    [
          5,   5,   5,   5,   5,   5,   5,   5,
          8,  10,  10,  10,  10,  10,  10,   8,
          5,   5,   5,   5,   5,   5,   5,   5,
         -2,   0,   0,   0,   0,   0,   0,  -2,
         -2,   0,   0,   0,   0,   0,   0,  -2,
         -2,   0,   0,   0,   0,   0,   0,  -2,
        -12, -10, -10, -10, -10, -10, -10, -12,
        -12, -10, -10, -10, -10, -10, -10, -12
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
    // @formatter:on

    // Cache lookup tables by piece type and game phase (start/end) to avoid repeated switch
    private static readonly int[][] PieceSquareTablesStart = new[]
    {
        ZeroScores, // 0 - no piece
        PawnStart,  // 1 - Pawn
        KnightScores, // 2 - Knight
        BishopStart,  // 3 - Bishop
        RookStart,    // 4 - Rook
        KingStart,    // 5 - King
        QueenScores,  // 6 - Queen
        ZeroScores    // 7 - sentinel
    };

    private static readonly int[][] PieceSquareTablesEnd = new[]
    {
        ZeroScores, // 0 - no piece
        PawnEnd,    // 1 - Pawn
        KnightScores, // 2 - Knight (no endgame table)
        BishopStart,  // 3 - Bishop
        RookEnd,      // 4 - Rook
        KingEnd,      // 5 - King
        QueenScores,  // 6 - Queen
        ZeroScores    // 7 - sentinel
    };

    private static int[] GetArray(byte piece, bool endGame = false)
    {
        // inline the piece type computation to avoid a method call hotspot
        var type = piece & 0x7; // equivalent to PieceTypes.PieceType(piece)

        // bounds-safe quick lookup
        if (type >= PieceSquareTablesStart.Length) return ZeroScores;
        return endGame ? PieceSquareTablesEnd[type] : PieceSquareTablesStart[type];
    }
}