namespace Onyx.Core;

public static class BoardConstants
{
    public const int WhiteKingsideCastlingFlag = 1 << 0;
    public const int WhiteQueensideCastlingFlag = 1 << 1;
    public const int BlackKingsideCastlingFlag = 1 << 2;
    public const int BlackQueensideCastlingFlag = 1 << 3;

    public const ulong WhiteKingSideCastlingSquares = 0x60;
    public const ulong BlackKingSideCastlingSquares = 0x6000000000000000;

    public const ulong WhiteQueenSideCastlingSquares = 0xe;
    public const ulong BlackQueenSideCastlingSquares = 0xe00000000000000;

    public const int A1 = 0;
    public const int H1 = 7;
    public const int A8 = 56;
    public const int H8 = 63;
    public const int E1 = 4;
    public const int E8 = 60;

    public const int G1 = 6;
    public const int G8 = 62;

    public const int C1 = 2;
    public const int C8 = 58;

    public const int B8 = 57;
    public const int B1 = 1;

    public static readonly int[][] KnightMoves =
        [[2, -1], [2, 1], [1, -2], [1, 2], [-1, -2], [-1, 2], [-2, -1], [-2, 1]];

    public static readonly int[][] KingMoves =
        [[1, 1], [1, 0], [1, -1], [0, 1], [0, -1], [-1, 1], [-1, 0], [-1, -1]];
}

internal class BoardState
{
    public Piece? CapturedPiece;
    public int? EnPassantSquare;
    public int CastlingRights;
    public uint LastMoveFlags;
    public int HalfMove;
    public int FullMove;
}

public class Board
{
    public Board Clone()
    {
        return new Board(this.GetFen());
    }

    public readonly Bitboards Bitboards;
    public Colour TurnToMove;
    public Zobrist Zobrist { get; private set; }

    // bit field - from the lowest bit in this order White : K, Q, Black K,Q
    public int CastlingRights { get; private set; }
    public int? EnPassantSquare { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }
    private readonly Stack<BoardState> _boardStateHistory;

    public Board(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        Bitboards = new Bitboards(fen);
        ApplyBoardStateFromFen(fen);
        Zobrist = new Zobrist(fen);
        _boardStateHistory = new Stack<BoardState>();
    }

    public string GetFen()
    {
        var builtFen = "";

        // apply position string
        builtFen += Bitboards.GetFen();

        // apply turn to move
        var moveChar = TurnToMove == Colour.White ? 'w' : 'b';
        builtFen += ' ';
        builtFen += moveChar;
        builtFen += ' ';


        var castlingRightsString = "";
        if ((CastlingRights & BoardConstants.WhiteKingsideCastlingFlag) > 0) castlingRightsString += 'K';
        if ((CastlingRights & BoardConstants.WhiteQueensideCastlingFlag) > 0) castlingRightsString += 'Q';
        if ((CastlingRights & BoardConstants.BlackKingsideCastlingFlag) > 0) castlingRightsString += 'k';
        if ((CastlingRights & BoardConstants.BlackQueensideCastlingFlag) > 0) castlingRightsString += 'q';

        if (castlingRightsString.Length == 0)
            castlingRightsString = "- ";
        else
        {
            castlingRightsString += " ";
        }

        builtFen += castlingRightsString;

        var enPassantString = EnPassantSquare.HasValue ? RankAndFileHelpers.Notation(EnPassantSquare.Value) : "-";
        builtFen += enPassantString;
        builtFen += $" {HalfMoves}";
        builtFen += $" {FullMoves}";

        return builtFen;
    }

    public void ApplyMove(Move move, bool fullApplyMove = true)
    {
        ApplyMoveFlags(ref move);

        var opponentColour = move.PieceMoved.Colour == Colour.White ? Colour.Black : Colour.White;
        Piece? capturedPiece;
        int? capturedSquare;

        if (move.IsEnPassant)
        {
            capturedPiece = opponentColour == Colour.Black ? Piece.BP : Piece.WP;
            var captureRank = opponentColour == Colour.White ? 3 : 4;
            capturedSquare = RankAndFileHelpers.SquareIndex(captureRank, RankAndFileHelpers.FileIndex(move.To));
        }
        else
        {
            capturedPiece = Bitboards.PieceAtSquare(move.To);
            capturedSquare = move.To;
        }

        if (fullApplyMove)
            Zobrist.ApplyMove(move, capturedPiece, capturedSquare);

        var state = new BoardState
        {
            CapturedPiece = capturedPiece,
            EnPassantSquare = EnPassantSquare,
            CastlingRights = CastlingRights,
            LastMoveFlags = move.Data,
            FullMove = FullMoves,
            HalfMove = HalfMoves
        };
        _boardStateHistory.Push(state);

        // action the required change for the moving piece
        MovePiece(move.PieceMoved, move.From, move.To);

        // get rid of the captured piece
        if (capturedPiece.HasValue)
        {
            Bitboards.SetOff(capturedPiece.Value, capturedSquare.Value);
        }

        // action the promotion
        if (move is { IsPromotion: true, PromotedPiece: not null })
        {
            // We need to undo the move to of the piece, thats covered in MovePiece,
            // as obviously for promotion this is overridden by the promoted piece. Explicitly set it off here
            Bitboards.SetOff(move.PieceMoved, move.To);
            Bitboards.SetOn(move.PromotedPiece.Value, move.To);
        }

        var toRankIndex = RankAndFileHelpers.RankIndex(move.To);
        var toFileIndex = RankAndFileHelpers.FileIndex(move.To);
        var fromRankIndex = RankAndFileHelpers.RankIndex(move.From);
        var fromFileIndex = RankAndFileHelpers.FileIndex(move.From);

        // handle castling
        if (move.IsCastling)
        {
            var affectedRook = move.PieceMoved.Colour == Colour.White
                ? Piece.WR
                : Piece.BR;


            var rookNewFile = toFileIndex == 2 ? 3 : 5;
            var rookOldFile = toFileIndex == 2 ? 0 : 7;

            MovePiece(affectedRook,
                RankAndFileHelpers.SquareIndex(toRankIndex, rookOldFile),
                RankAndFileHelpers.SquareIndex(toRankIndex, rookNewFile));
        }

        // king moving always sacrifices the castling rights
        UpdateCastlingRights(move);

        // set en passant target square
        if (move.PieceMoved.Type == PieceType.Pawn && Math.Abs(fromRankIndex - toRankIndex) == 2)
        {
            var targetRank = move.PieceMoved.Colour == Colour.White ? 2 : 5;
            EnPassantSquare = RankAndFileHelpers.SquareIndex(targetRank, fromFileIndex);
        }
        else
        {
            EnPassantSquare = null;
        }

        SwapTurns();

        if (move.PieceMoved.Colour == Colour.Black)
            FullMoves++;
        if (move.PieceMoved.Type != PieceType.Pawn && !capturedPiece.HasValue)
            HalfMoves++;
        else
        {
            HalfMoves = 0;
        }
    }

    public void UndoMove(Move move, bool fullUndoMove = true)
    {
        //ApplyMoveFlags(move: ref move);

        int? capturedOn = null;

        var previousState = _boardStateHistory.Pop();
        EnPassantSquare = previousState.EnPassantSquare;
        CastlingRights = previousState.CastlingRights;
        HalfMoves = previousState.HalfMove;
        FullMoves = previousState.FullMove;
        move.Data = previousState.LastMoveFlags;
        if (previousState.CapturedPiece.HasValue)
        {
            if (!move.IsEnPassant)
            {
                Bitboards.SetOn(previousState.CapturedPiece.Value, move.To);
                capturedOn = move.To;
            }
        }


        MovePiece(move.PieceMoved, move.To, move.From);

        if (move.IsPromotion && move.PromotedPiece.HasValue)
        {
            Bitboards.SetOff(move.PromotedPiece.Value, move.To);
        }

        if (move.IsCastling)
        {
            var rank = RankAndFileHelpers.RankIndex(move.To);
            var file = RankAndFileHelpers.FileIndex(move.To);
            var rookHomeFile = file > 4 ? 7 : 0;
            var rookNewFile = file == 2 ? 3 : 5;
            Bitboards.SetOn(Piece.MakePiece(PieceType.Rook, move.PieceMoved.Colour),
                RankAndFileHelpers.SquareIndex(rank, rookHomeFile));
            Bitboards.SetOff(Piece.MakePiece(PieceType.Rook, move.PieceMoved.Colour),
                RankAndFileHelpers.SquareIndex(rank, rookNewFile));
        }


        if (move.IsEnPassant)
        {
            var pawnHomeRank = move.PieceMoved.Colour == Colour.Black ? 3 : 4;
            var capturedColour = move.PieceMoved.Colour == Colour.White ? Colour.Black : Colour.White;
            capturedOn = RankAndFileHelpers.SquareIndex(pawnHomeRank, RankAndFileHelpers.FileIndex(move.To));
            Bitboards.SetOn(Piece.MakePiece(PieceType.Pawn, capturedColour),
                capturedOn.Value);
        }

        if (fullUndoMove)
            Zobrist.ApplyMove(move, previousState.CapturedPiece, capturedOn);


        SwapTurns();
    }

    public void ApplyMoveFlags(ref Move move)
    {
        var toRankIndex = RankAndFileHelpers.RankIndex(move.To);
        var toFileIndex = RankAndFileHelpers.FileIndex(move.To);
        var fromFileIndex = RankAndFileHelpers.FileIndex(move.From);

        if (move.PieceMoved.Type == PieceType.Pawn &&
            ((move.PieceMoved.Colour == Colour.White && toRankIndex == 7) ||
             (move.PieceMoved.Colour == Colour.Black && toRankIndex == 0)))
        {
            move.IsPromotion = true;
        }

        if (move.PieceMoved.Type == PieceType.King && Math.Abs(fromFileIndex - toFileIndex) > 1)
        {
            
            move.IsCastling = true;
        }

        if (move.PieceMoved.Type == PieceType.Pawn && fromFileIndex - toFileIndex != 0 &&
            !Bitboards.PieceAtSquare(move.To).HasValue)
        {
           
            move.IsEnPassant = true;
        }
    }

    private void UpdateCastlingRights(Move move)
    {
        if (move.PieceMoved.Type == PieceType.King)
        {
            if (move.PieceMoved.Colour == Colour.White)
            {
                CastlingRights &= ~BoardConstants.WhiteKingsideCastlingFlag;
                CastlingRights &= ~BoardConstants.WhiteQueensideCastlingFlag;
            }
            else
            {
                CastlingRights &= ~BoardConstants.BlackKingsideCastlingFlag;
                CastlingRights &= ~BoardConstants.BlackQueensideCastlingFlag;
            }
        }

        if (move.PieceMoved.Type != PieceType.Rook) return;

        if (move.PieceMoved.Colour == Colour.White)
        {
            if (move.From == BoardConstants.A1)
                CastlingRights &= ~BoardConstants.WhiteQueensideCastlingFlag;
            if (move.From == BoardConstants.H1)
                CastlingRights &= ~BoardConstants.WhiteKingsideCastlingFlag;
        }
        else
        {
            if (move.From == BoardConstants.A8)
                CastlingRights &= ~BoardConstants.BlackQueensideCastlingFlag;
            if (move.From == BoardConstants.H8)
                CastlingRights &= ~BoardConstants.BlackKingsideCastlingFlag;
        }
    }

    private void MovePiece(Piece piece, int from, int to)
    {
        Bitboards.SetOff(piece, from);
        Bitboards.SetOn(piece, to);
    }

    private void SwapTurns()
    {
        if (TurnToMove == Colour.White)
            TurnToMove = Colour.Black;
        else
        {
            TurnToMove = Colour.White;
        }
    }

    private void ApplyBoardStateFromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);

        TurnToMove = fenDetails.ColourToMove;

        var castlingString = fenDetails.CastlingString;
        if (castlingString.Contains('K')) CastlingRights |= BoardConstants.WhiteKingsideCastlingFlag;
        if (castlingString.Contains('Q')) CastlingRights |= BoardConstants.WhiteQueensideCastlingFlag;
        if (castlingString.Contains('k')) CastlingRights |= BoardConstants.BlackKingsideCastlingFlag;
        if (castlingString.Contains('q')) CastlingRights |= BoardConstants.BlackQueensideCastlingFlag;

        EnPassantSquare = fenDetails.EnPassantSquare;


        HalfMoves = fenDetails.HalfMove;
        FullMoves = fenDetails.FullMove;
    }

    public void ApplyMoves(List<string>? positionCommandMoves)
    {
        if (positionCommandMoves == null) return;
        foreach (var move in positionCommandMoves)
        {
            var from = move[..2];
            var squareFrom = RankAndFileHelpers.SquareIndex(from);

            var pieceMoved = Bitboards.PieceAtSquare(squareFrom);
            if (!pieceMoved.HasValue)
                throw new InvalidOperationException("No piece at moved from square");

            var moveToApply = new Move(pieceMoved.Value, move);
            ApplyMove(moveToApply);
        }
    }
}