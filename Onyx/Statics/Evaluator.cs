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
    public static void SortMoves(Span<Move> moves, Move? transpositionTableMove)
    {
        moves.Sort((move, move1) =>
        {
            var aScore = move.IsPromotion ? 1 : 0;
            var bScore = move.IsPromotion ? 1 : 0;

            if (transpositionTableMove.HasValue && transpositionTableMove.Value.Data > 0)
            {
                if (move.Data == transpositionTableMove.Value.Data)
                    return -1;
                if (move1.Data == transpositionTableMove.Value.Data)
                    return 1;
            }

            return bScore.CompareTo(aScore);
        });
    }

    public static int Evaluate(Board board)
    {
        if (Referee.IsThreeFoldRepetition(board)) return 0;

        var whiteMaterial = EvaluateMaterial(board, true);
        var blackMaterial = EvaluateMaterial(board, false);

        // value of material
        var score = whiteMaterial.MaterialScore - blackMaterial.MaterialScore;

        // small boost for having both bishops on the board
        score += whiteMaterial.BishopPairScore - blackMaterial.BishopPairScore;

        // number of moves available to you
        score += MobilityScore(board);
        
        var whitePSS = PieceSquareScore(board, blackMaterial.EndGameRatio(), true);
        var blackPSS = PieceSquareScore(board, whiteMaterial.EndGameRatio(), false);
        score += whitePSS - blackPSS;

        return board.WhiteToMove ? score : -score;
    }

    private static int MobilityScore(Board board)
    {
        // cache turn
        var boardTurnToMove = board.WhiteToMove;
        Span<Move> moveBuffer = stackalloc Move[256];

        var boardInCheck = Referee.IsInCheck(board.WhiteToMove, board);
        // get moves for both sides. Psuedo legal fine, but reward positions where board is in check
        board.WhiteToMove = true;
        var whiteMoves = boardInCheck
            ? MoveGenerator.GetLegalMoves(board, moveBuffer)
            : MoveGenerator.GetMoves(board, moveBuffer);
        board.WhiteToMove = false;
        var blackMoves = boardInCheck
            ? MoveGenerator.GetLegalMoves(board, moveBuffer)
            : MoveGenerator.GetMoves(board, moveBuffer);

        // restore turn
        board.WhiteToMove = boardTurnToMove;

        // 10 points per move
        return (whiteMoves - blackMoves) * 10;
    }

    private static MaterialEvaluation EvaluateMaterial(Board board, bool forWhite)
    {
        MaterialEvaluation materialEvaluation = new MaterialEvaluation();
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            var occupancyByPiece = board.Bitboards.OccupancyByPiece(piece);
            var pieceCount = ulong.PopCount(occupancyByPiece);
            materialEvaluation.MaterialScore += (int)pieceCount * PieceValues[Piece.PieceTypeIndex(piece)];

            switch (Piece.PieceType(piece))
            {
                case Piece.Pawn:
                    materialEvaluation.Pawns++;
                    //materialEvaluation.PawnBoard = occupancyByPiece;
                    break;
                case Piece.Knight:
                    materialEvaluation.Knights++;
                    //materialEvaluation.KnightBoard = occupancyByPiece;
                    break;
                case Piece.Rook:
                    materialEvaluation.Rooks++;
                    //materialEvaluation.RookBoard = occupancyByPiece;
                    break;
                case Piece.Queen:
                    materialEvaluation.Queens++;
                    //materialEvaluation.QueenBoard = occupancyByPiece;
                    break;
                case Piece.Bishop:
                    materialEvaluation.Bishops++;
                    //materialEvaluation.BishopBoard = occupancyByPiece;
                    break;
            }
        }

        return materialEvaluation;
    }

    private static int PieceSquareScore(Board board, float enemyEndGameScale, bool forWhite)
    {
        var score = 0;
        var pieces = forWhite ? Piece._whitePieces : Piece._blackPieces;
        foreach (var piece in pieces)
        {
            score += PieceSquareScoreByPiece(board, piece, enemyEndGameScale);
        }

        return score;
    }

    private static int PieceSquareScoreByPiece(Board board, sbyte piece, float enemyEndGameScale)
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
        return getArray(piece, endGame)[index];
    }

    // tables are laid out like looking at a board from white's perspective'
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
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        80, 80, 80, 80, 80, 80, 80, 80,
        50, 50, 50, 50, 50, 50, 50, 50,
        30, 30, 30, 30, 30, 30, 30, 30,
        20, 20, 20, 20, 20, 20, 20, 20,
        10, 10, 10, 10, 10, 10, 10, 10,
        10, 10, 10, 10, 10, 10, 10, 10,
        0, 0, 0, 0, 0, 0, 0, 0
    };
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

    private static int[] getArray(sbyte piece, bool endGame = false)
    {
        var type = Piece.PieceType(piece);

        return type switch
        {
            Piece.Pawn => endGame ? PawnEnd : PawnStart,
            Piece.Knight => KnightScores,
            Piece.Bishop => BishopStart,
            Piece.Queen => QueenScores,
            Piece.Rook => RookScores,
            _ => ZeroScores
        };
    }
}