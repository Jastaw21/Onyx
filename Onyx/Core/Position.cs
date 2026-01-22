using Onyx.Statics;

namespace Onyx.Core;


public class PositionState
{
    public sbyte? CapturedPiece;
    public int? EnPassantSquare;
    public int CastlingRights;
    public uint LastMoveFlags;
    public int HalfMove;
    public int FullMove;
    public ulong Hash;
}

public class Position
{
    public Position Clone()
    {
        return new Position(GetFen());
    }

    public readonly Bitboards Bitboards;
    public bool WhiteToMove;
    public ulong ZobristState { get; private set; }
    
    public int CastlingRights { get; private set; }// bit field - from the lowest bit in this order White : K, Q, Black K,Q
    public int? EnPassantSquare { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }
    public ReadOnlySpan<PositionState> History => _historyBuffer.AsSpan(0, _historyStackPointer + 1);

    private PositionState[] _historyBuffer = new PositionState[1024];
    private int _historyStackPointer;


    public Position(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        Bitboards = new Bitboards(fen);
        ApplyBoardStateFromFen(fen);
        ZobristState = Zobrist.FromFen(fen);
        for (int i = 0; i < _historyBuffer.Length; i++) _historyBuffer[i] = new PositionState();

        var startingState = new PositionState
        {
            Hash = ZobristState, CastlingRights = CastlingRights, EnPassantSquare = EnPassantSquare,
            HalfMove = HalfMoves, FullMove = FullMoves
        };

        _historyStackPointer = 0;
        _historyBuffer[_historyStackPointer] = startingState;
    }

    public void SetFen(string fen)
    {
        Bitboards.LoadFen(fen);
        ApplyBoardStateFromFen(fen);
        ZobristState = Zobrist.FromFen(fen);

        for (int i = 0; i < _historyBuffer.Length; i++) _historyBuffer[i] = new PositionState();

        var startingState = new PositionState
        {
            Hash = ZobristState,
            CastlingRights = CastlingRights,
            EnPassantSquare = EnPassantSquare,
            HalfMove = HalfMoves,
            FullMove = FullMoves
        };

        _historyStackPointer = 0;
        _historyBuffer[_historyStackPointer] = startingState;
    }

    private void UpdateHistoryState()
    {
        PositionState state = _historyBuffer[_historyStackPointer];
        state.Hash = ZobristState;
        state.CastlingRights = CastlingRights;
        state.EnPassantSquare = EnPassantSquare;
        state.HalfMove = HalfMoves;
        state.FullMove = FullMoves;
        // CapturedPiece and LastMoveFlags are set during ApplyMove
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
        if ((CastlingRights & BoardHelpers.WhiteKingsideCastlingFlag) > 0) castlingRightsString += 'K';
        if ((CastlingRights & BoardHelpers.WhiteQueensideCastlingFlag) > 0) castlingRightsString += 'Q';
        if ((CastlingRights & BoardHelpers.BlackKingsideCastlingFlag) > 0) castlingRightsString += 'k';
        if ((CastlingRights & BoardHelpers.BlackQueensideCastlingFlag) > 0) castlingRightsString += 'q';

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

    public void MakeNullMove()
    {
        _historyStackPointer++;
        _historyBuffer[_historyStackPointer].LastMoveFlags = 0;
        _historyBuffer[_historyStackPointer].CapturedPiece = null;
        _historyBuffer[_historyStackPointer].EnPassantSquare = EnPassantSquare;
        _historyBuffer[_historyStackPointer].CastlingRights = CastlingRights;
        _historyBuffer[_historyStackPointer].HalfMove = HalfMoves;
        _historyBuffer[_historyStackPointer].FullMove = FullMoves;
        _historyBuffer[_historyStackPointer].Hash = ZobristState;

        if (!WhiteToMove)
            FullMoves++;
        HalfMoves++;

        SwapTurns();
        ZobristState ^= Zobrist.WhiteToMove;

        if (EnPassantSquare.HasValue)
        {
            ZobristState ^= Zobrist.EnPassantSquare[EnPassantSquare.Value];
            EnPassantSquare = null;
        }
    }

    public void UndoNullMove()
    {
        var state = _historyBuffer[_historyStackPointer];

        // Restore everything from history
        ZobristState = state.Hash;
        EnPassantSquare = state.EnPassantSquare;
        CastlingRights = state.CastlingRights;
        HalfMoves = state.HalfMove;
        FullMoves = state.FullMove;

        SwapTurns(); // Flip side to move back

        _historyStackPointer--;
    }

    public void ApplyMove(Move move, bool fullApplyMove = true)
    {
        ApplyMoveFlags(ref move);

        // these will be used to update the zobrist hash
        var previousCastlingRights = CastlingRights;
        var previousEnPassantSquare = EnPassantSquare;

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
            var moveCapturedPiece = move.CapturedPiece;
            if (moveCapturedPiece > 0)
                capturedPiece = Piece.MakePiece(moveCapturedPiece, !isWhite);
            else capturedPiece = null;
            capturedSquare = move.To;
        }

        _historyBuffer[_historyStackPointer].LastMoveFlags = move.Data;
        _historyBuffer[_historyStackPointer].CapturedPiece = capturedPiece;
        _historyStackPointer++;

        // get rid of the captured piece
        if (capturedPiece.HasValue)
        {
            Bitboards.SetOff(capturedPiece.Value, capturedSquare.Value);
        }

        // action the required change for the moving piece
        if (move.IsPromotion && move.PromotedPiece.HasValue)
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
        var pieceType = Piece.PieceType(move.PieceMoved);
        UpdateCastlingRights(move, pieceType, isWhite);

        // set en passant target square
        
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
        // irreversible move
        else
        {
            HalfMoves = 0;
        }

        if (fullApplyMove)
        {
            ZobristState = Zobrist.ApplyMove(move, ZobristState, capturedPiece, capturedSquare, previousEnPassantSquare,
                EnPassantSquare, previousCastlingRights, CastlingRights);
        }

        UpdateHistoryState();
    }

    public void UndoMove(Move move, bool fullUndoMove = true)
    {
        _historyStackPointer--;
        var state = _historyBuffer[_historyStackPointer];

        // for zobrist hash update
        var epAfterThisMove = EnPassantSquare;
        var castlingRightsAfterThisMove = CastlingRights;

        EnPassantSquare = state.EnPassantSquare;
        CastlingRights = state.CastlingRights;
        HalfMoves = state.HalfMove;
        FullMoves = state.FullMove;
        move.Data = state.LastMoveFlags;
       
        var movePieceMoved = move.PieceMoved;


        if (move.IsPromotion)
        {
            Bitboards.SetOff(move.PromotedPiece!.Value, move.To);
            Bitboards.SetOn(movePieceMoved, move.From);
        }
        else
        {
            MovePiece(movePieceMoved, move.To, move.From);
        }
        
        int? capturedOn = null;
        if (state.CapturedPiece.HasValue)
        {
            if (!move.IsEnPassant)
            {
                Bitboards.SetOn(state.CapturedPiece.Value, move.To);
                capturedOn = move.To;
            }
        }
      
        var isWhite = !Piece.IsBlack(movePieceMoved);
        var fileIndex = RankAndFile.FileIndex(move.To);
        if (move.IsCastling)
        {
            
            var rank = RankAndFile.RankIndex(move.To);
            var file = fileIndex;
            var rookHomeFile = file > 4 ? 7 : 0;
            var rookNewFile = file == 2 ? 3 : 5;
            var rook = Piece.MakePiece(Piece.Rook, isWhite);

            Bitboards.SetOn(rook, RankAndFile.SquareIndex(rank, rookHomeFile));
            Bitboards.SetOff(rook, RankAndFile.SquareIndex(rank, rookNewFile));
        }


        if (move.IsEnPassant)
        {
            var pawnHomeRank = isWhite ? 4 : 3;
            capturedOn = RankAndFile.SquareIndex(pawnHomeRank, fileIndex);
            Bitboards.SetOn(Piece.MakePiece(Piece.Pawn, !isWhite), capturedOn.Value);
        }

        if (fullUndoMove)
        {
            ZobristState = Zobrist.ApplyMove(move, ZobristState, state.CapturedPiece, capturedOn, EnPassantSquare,
                epAfterThisMove, CastlingRights, castlingRightsAfterThisMove);
        }


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

        // this move isn't from move gen, so need to check for capture
        if (!move.HasCaptureBeenChecked)
        {
            // normal capture
            if ((Bitboards.AllPieces & (1ul << move.To)) != 0)
            {
                var pieceAtSquare = Bitboards.PieceAtSquare(move.To);
                move.CapturedPiece = pieceAtSquare!.Value;
            }
            // en passant capture
            else if (move.IsEnPassant)
            {
                move.CapturedPiece = Piece.MakePiece(Piece.Pawn, !Piece.IsWhite(move.PieceMoved));
            }
        }
    }

    private void UpdateCastlingRights(Move move, int pieceType, bool isWhite)
    {
        
        if (pieceType == Piece.King)
        {
            if (isWhite)
            {
                CastlingRights &= ~BoardHelpers.WhiteKingsideCastlingFlag;
                CastlingRights &= ~BoardHelpers.WhiteQueensideCastlingFlag;
            }
            else
            {
                CastlingRights &= ~BoardHelpers.BlackKingsideCastlingFlag;
                CastlingRights &= ~BoardHelpers.BlackQueensideCastlingFlag;
            }
        }

        if (pieceType != Piece.Rook) return;

        if (isWhite)
        {
            if (move.From == BoardHelpers.A1)
                CastlingRights &= ~BoardHelpers.WhiteQueensideCastlingFlag;
            if (move.From == BoardHelpers.H1)
                CastlingRights &= ~BoardHelpers.WhiteKingsideCastlingFlag;
        }
        else
        {
            if (move.From == BoardHelpers.A8)
                CastlingRights &= ~BoardHelpers.BlackQueensideCastlingFlag;
            if (move.From == BoardHelpers.H8)
                CastlingRights &= ~BoardHelpers.BlackKingsideCastlingFlag;
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
        CastlingRights = 0;
        EnPassantSquare = null;
        var fenDetails = Fen.FromString(fen);

        WhiteToMove = fenDetails.WhiteToMove;

        var castlingString = fenDetails.CastlingString;
        if (castlingString.Contains('K')) CastlingRights |= BoardHelpers.WhiteKingsideCastlingFlag;
        if (castlingString.Contains('Q')) CastlingRights |= BoardHelpers.WhiteQueensideCastlingFlag;
        if (castlingString.Contains('k')) CastlingRights |= BoardHelpers.BlackKingsideCastlingFlag;
        if (castlingString.Contains('q')) CastlingRights |= BoardHelpers.BlackQueensideCastlingFlag;

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