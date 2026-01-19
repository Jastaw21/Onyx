using Onyx.Core;


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
    public static bool LoggingEnabled = false;

    public static void SortMoves(Span<Move> moves, Move? transpositionTableMove, Move?[,] killerMoves, int ply)
    {
        try
        {
            moves.Sort((a, b) =>
            {
                if (a == b)
                {
                    return 0;
                }

                if (transpositionTableMove is { Data: > 0 })
                {
                    if (a == transpositionTableMove.Value)
                        return -99;
                    if (b == transpositionTableMove.Value)
                        return 99;
                }

                var aScore = GetMoveScore(a, killerMoves, ply);
                var bScore = GetMoveScore(b, killerMoves, ply);

                return bScore.CompareTo(aScore);
            });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private static int GetMoveScore(Move move, Move?[,]? killerMoves, int ply)
    {
        var score = 0;
        if (move.IsPromotion) score += 100000;

        if (move.CapturedPiece != 0)
        {
            var victimPiece = PieceValues[Piece.PieceTypeIndex(move.CapturedPiece)];
            var attackerPiece = PieceValues[Piece.PieceTypeIndex(move.PieceMoved)];
            score += 12000 + (victimPiece * 10 - attackerPiece);
        }

        if (killerMoves == null) return score;
        if (killerMoves[ply, 0] == move)
            return 7000;
        if (killerMoves[ply, 1] == move)
            return 2000;

        return score;
    }

    public static int Evaluate(Position board)
    {
        if (Referee.IsThreeFoldRepetition(board)) return 0;

        var whiteMaterial = EvaluateMaterial(board, true);
        var blackMaterial = EvaluateMaterial(board, false);

        // value of material
        var materialScore = whiteMaterial.MaterialScore - blackMaterial.MaterialScore;

        // a small boost for having both bishops on the board
        var bishopPairScore = whiteMaterial.BishopPairScore - blackMaterial.BishopPairScore;

        // piece square score
        var whitePss = PieceSquareScore(board, blackMaterial.EndGameRatio(), true);
        var blackPss = PieceSquareScore(board, whiteMaterial.EndGameRatio(), false);
        var pieceSquareScore = whitePss - blackPss;

        if (LoggingEnabled)
            Logger.Log(LogType.Evaluator,
                $"{board.GetFen()} MS: {materialScore} BS: {bishopPairScore} PSS: {pieceSquareScore}");

        var score = materialScore + bishopPairScore + pieceSquareScore;
        return board.WhiteToMove ? score : -score;
    }

    private static int MobilityScore(Position board)
    {
        // cache turn
        var boardTurnToMove = board.WhiteToMove;
        Span<Move> moveBuffer = stackalloc Move[256];

        // get moves for both sides. Pseudo legal fine, but reward positions where board is in check
        board.WhiteToMove = true;
        var whiteMoves = MoveGenerator.GetMoves(board, moveBuffer);
        board.WhiteToMove = false;
        var blackMoves = MoveGenerator.GetMoves(board, moveBuffer);

        // restore turn
        board.WhiteToMove = boardTurnToMove;

        // 10 points per move
        return (whiteMoves - blackMoves) * 10;
    }

    private static MaterialEvaluation EvaluateMaterial(Position board, bool forWhite)
    {
        var materialEvaluation = new MaterialEvaluation();
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            var occupancyByPiece = board.Bitboards.OccupancyByPiece(piece);
            var pieceCount = ulong.PopCount(occupancyByPiece);
            materialEvaluation.MaterialScore += (int)pieceCount * PieceValues[Piece.PieceTypeIndex(piece)];

            switch (Piece.PieceType(piece))
            {
                case Piece.Pawn:
                    materialEvaluation.Pawns += (int)pieceCount;
                    break;
                case Piece.Knight:
                    materialEvaluation.Knights += (int)pieceCount;
                    break;
                case Piece.Rook:
                    materialEvaluation.Rooks += (int)pieceCount;
                    break;
                case Piece.Queen:
                    materialEvaluation.Queens += (int)pieceCount;
                    break;
                case Piece.Bishop:
                    materialEvaluation.Bishops += (int)pieceCount;
                    break;
            }
        }

        return materialEvaluation;
    }

    private static int PieceSquareScore(Position board, float enemyEndGameScale, bool forWhite)
    {
        var score = 0;
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            score += PieceSquareScoreByPiece(board, piece, enemyEndGameScale);
        }

        return score;
    }

    private static int PieceSquareScoreByPiece(Position board, sbyte piece, float enemyEndGameScale)
    {
        var bitboardIndex = Piece.BitboardIndex(piece);
        var occupancy = board.Bitboards.Boards[bitboardIndex];

        var score = 0;
        while (occupancy > 0)
        {
            var lowestSetBit = ulong.TrailingZeroCount(occupancy);
            var square = (int)lowestSetBit;
            if (Piece.PieceType(piece) == Piece.Pawn)
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

    public static int GetPieceValueOnSquare(int square, sbyte piece, bool endGame = false)
    {
        var index = Piece.IsWhite(piece) ? square ^ 56 : square;
        return GetArray(piece, endGame)[index];
    }

    // tables are laid out like looking at a board from white's perspective
    private static readonly int[] PawnStart =
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
    private static readonly int[] PawnEnd =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        80, 80, 80, 80, 80, 80, 80, 80,
        50, 50, 50, 50, 50, 50, 50, 50,
        30, 30, 30, 30, 30, 30, 30, 30,
        20, 20, 20, 20, 20, 20, 20, 20,
        10, 10, 10, 10, 10, 10, 10, 10,
        10, 10, 10, 10, 10, 10, 10, 10,
        0, 0, 0, 0, 0, 0, 0, 0
    ];
    private static readonly int[] BishopStart =
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
    private static readonly int[] KingStart =
    [
        -80, -70, -70, -70, -70, -70, -70, -80,
        -60, -60, -60, -60, -60, -60, -60, -60,
        -40, -50, -50, -60, -60, -50, -50, -40,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -20, -30, -30, -40, -40, -30, -30, -20,
        -10, -20, -20, -20, -20, -20, -20, -10,
        20, 20, -5, -5, -5, -5, 20, 20,
        20, 30, 10, 0, 0, 10, 30, 20
    ];
    private static readonly int[] KingEnd =
    [
        -20, -10, -10, -10, -10, -10, -10, -20,
        -5,   0,   5,   5,   5,   5,   0,  -5,
        -10, -5,   20,  30,  30,  20,  -5, -10,
        -15, -10,  35,  45,  45,  35, -10, -15,
        -20, -15,  30,  40,  40,  30, -15, -20,
        -25, -20,  20,  25,  25,  20, -20, -25,
        -30, -25,   0,   0,   0,   0, -25, -30,
        -50, -30, -30, -30, -30, -30, -30, -50
    ];

    private static readonly int[] RookStart =
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
    private static readonly int[] RookEnd =
    [
        5, 5, 5, 5, 5, 5, 5, 5,
        8, 10, 10, 10, 10, 10, 10, 8,
        5, 5, 5, 5, 5, 5, 5, 5,
        -2, 0, 0, 0, 0, 0, 0, -2,
        -2, 0, 0, 0, 0, 0, 0, -2,
        -2, 0, 0, 0, 0, 0, 0, -2,
        -12, -10, -10, -10, -10, -10, -10, -12,
        -12, -10, -10, -10, -10, -10, -10, -12,
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

    private static int[] GetArray(sbyte piece, bool endGame = false)
    {
        var type = Piece.PieceType(piece);

        return type switch
        {
            Piece.Pawn =>  endGame ? PawnEnd:PawnStart,
            Piece.Knight => KnightScores,
            Piece.Bishop => BishopStart,
            Piece.Queen => QueenScores,
            Piece.Rook =>endGame ? RookEnd:RookStart,
            Piece.King =>endGame ?  KingEnd:KingStart,
            _ => ZeroScores
        };
    }
}