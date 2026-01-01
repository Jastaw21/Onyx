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
    public sbyte? CapturedPiece;
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
    public bool WhiteToMove;
    public Zobrist Zobrist { get; private set; }

    // bit field - from the lowest bit in this order White : K, Q, Black K,Q
    public int CastlingRights { get; private set; }
    public int? EnPassantSquare { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }
    private readonly Stack<BoardState> _boardStateHistory;

    public sbyte LastCapture => _boardStateHistory.Count > 0 && _boardStateHistory.Last().CapturedPiece.HasValue
        ? _boardStateHistory.Last().CapturedPiece.Value
        : (sbyte)0;

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
        var moveChar = WhiteToMove ? 'w' : 'b';
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

        var enPassantString = EnPassantSquare.HasValue ? RankAndFile.Notation(EnPassantSquare.Value) : "-";
        builtFen += enPassantString;
        builtFen += $" {HalfMoves}";
        builtFen += $" {FullMoves}";

        return builtFen;
    }

    public void ApplyMove(Move move, bool fullApplyMove = true)
    {
        ApplyMoveFlags(ref move);

        var isWhite = Piece.IsWhite(move.PieceMoved);
        sbyte? capturedPiece;
        int? capturedSquare;

        if (move.IsEnPassant)
        {
            capturedPiece = isWhite ? Piece.BP : Piece.WP;
            var captureRank = isWhite ? 4 : 3;
            capturedSquare = RankAndFile.SquareIndex(captureRank, RankAndFile.FileIndex(move.To));
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

       

        // get rid of the captured piece
        if (capturedPiece.HasValue)
        {
            Bitboards.SetOff(capturedPiece.Value, capturedSquare.Value);
        }

        // action the required change for the moving piece
        if (move is { IsPromotion: true, PromotedPiece: not null })
        {
            Bitboards.SetOff(move.PieceMoved, move.From);
            Bitboards.SetOn(move.PromotedPiece.Value, move.To);
        }
        else
        {
            MovePiece(move.PieceMoved, move.From, move.To);
        }

        var toRankIndex = RankAndFile.RankIndex(move.To);
        var toFileIndex = RankAndFile.FileIndex(move.To);
        var fromRankIndex = RankAndFile.RankIndex(move.From);
        var fromFileIndex = RankAndFile.FileIndex(move.From);

        // handle castling
        if (move.IsCastling)
        {
            var affectedRook = isWhite ? Piece.WR : Piece.BR;


            var rookNewFile = toFileIndex == 2 ? 3 : 5;
            var rookOldFile = toFileIndex == 2 ? 0 : 7;

            MovePiece(affectedRook,
                RankAndFile.SquareIndex(toRankIndex, rookOldFile),
                RankAndFile.SquareIndex(toRankIndex, rookNewFile));
        }

        // king moving always sacrifices the castling rights
        UpdateCastlingRights(move);

        // set en passant target square
        var pieceType = Piece.PieceType(move.PieceMoved);
        if (pieceType == Piece.Pawn && Math.Abs(fromRankIndex - toRankIndex) == 2)
        {
            var targetRank = isWhite ? 2 : 5;
            EnPassantSquare = RankAndFile.SquareIndex(targetRank, fromFileIndex);
        }
        else
        {
            EnPassantSquare = null;
        }

        SwapTurns();

        if (Piece.IsBlack(move.PieceMoved))
            FullMoves++;
        if (pieceType != Piece.Pawn && !capturedPiece.HasValue)
            HalfMoves++;
        else
        {
            HalfMoves = 0;
        }
    }

    public void UndoMove(Move move, bool fullUndoMove = true)
    {
        int? capturedOn = null;
        var movePieceMoved = move.PieceMoved;

        var previousState = _boardStateHistory.Pop();
        EnPassantSquare = previousState.EnPassantSquare;
        CastlingRights = previousState.CastlingRights;
        HalfMoves = previousState.HalfMove;
        FullMoves = previousState.FullMove;
        move.Data = previousState.LastMoveFlags;
        
        if (move.IsPromotion && move.PromotedPiece.HasValue)
        {
            Bitboards.SetOff(move.PromotedPiece.Value, move.To);
            Bitboards.SetOn(movePieceMoved, move.From);
        }
        else
        {
            MovePiece(movePieceMoved, move.To, move.From);
        }

        var capturedPieceHasValue = previousState.CapturedPiece.HasValue;
        if (capturedPieceHasValue)
        {
            if (!move.IsEnPassant)
            {
                Bitboards.SetOn(previousState.CapturedPiece.Value, move.To);
                capturedOn = move.To;
            }
        }

        var isBlack = Piece.IsBlack(movePieceMoved);
        var isWhite = !isBlack;
        if (move.IsCastling)
        {
            var rank = RankAndFile.RankIndex(move.To);
            var file = RankAndFile.FileIndex(move.To);
            var rookHomeFile = file > 4 ? 7 : 0;
            var rookNewFile = file == 2 ? 3 : 5;
            var rook = Piece.MakePiece(Piece.Rook, isWhite);

            Bitboards.SetOn(rook, RankAndFile.SquareIndex(rank, rookHomeFile));
            Bitboards.SetOff(rook, RankAndFile.SquareIndex(rank, rookNewFile));
        }


        if (move.IsEnPassant)
        {
            var pawnHomeRank = isBlack ? 3 : 4;
            capturedOn = RankAndFile.SquareIndex(pawnHomeRank, RankAndFile.FileIndex(move.To));
            Bitboards.SetOn(Piece.MakePiece(Piece.Pawn, !isWhite), capturedOn.Value);
        }

        if (fullUndoMove)
            Zobrist.ApplyMove(move, previousState.CapturedPiece, capturedOn);


        SwapTurns();
    }

    private void ApplyMoveFlags(ref Move move)
    {
        var toRankIndex = RankAndFile.RankIndex(move.To);
        var toFileIndex = RankAndFile.FileIndex(move.To);
        var fromFileIndex = RankAndFile.FileIndex(move.From);

        var pieceType = Piece.PieceType(move.PieceMoved);
        if (pieceType == Piece.Pawn &&
            ((Piece.IsWhite(move.PieceMoved) && toRankIndex == 7) ||
             (Piece.IsBlack(move.PieceMoved) && toRankIndex == 0)))
        {
            move.IsPromotion = true;
        }

        if (pieceType == Piece.King && Math.Abs(fromFileIndex - toFileIndex) > 1)
        {
            move.IsCastling = true;
        }

        if (pieceType == Piece.Pawn && fromFileIndex - toFileIndex != 0 &&
            !Bitboards.PieceAtSquare(move.To).HasValue)
        {
            move.IsEnPassant = true;
        }
    }

    private void UpdateCastlingRights(Move move)
    {
        var type = Piece.PieceType(move.PieceMoved);
        var isWhite = Piece.IsWhite(move.PieceMoved);
        if (type == Piece.King)
        {
            if (isWhite)
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

        if (type != Piece.Rook) return;

        if (isWhite)
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

    private void MovePiece(sbyte piece, int from, int to)
    {
        Bitboards.SetOff(piece, from);
        Bitboards.SetOn(piece, to);
    }

    private void SwapTurns()
    {
        WhiteToMove = !WhiteToMove;
    }

    private void ApplyBoardStateFromFen(string fen)
    {
        var fenDetails = Fen.FromString(fen);

        WhiteToMove = fenDetails.WhiteToMove;

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
            var squareFrom = RankAndFile.SquareIndex(from);

            var pieceMoved = Bitboards.PieceAtSquare(squareFrom);
            if (!pieceMoved.HasValue)
                throw new InvalidOperationException("No piece at moved from square");

            var moveToApply = new Move(pieceMoved.Value, move);
            ApplyMove(moveToApply);
        }
    }
}