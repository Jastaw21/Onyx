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
    public Square? EnPassantSquare;
    public int CastlingRights;
    public int LastMoveFlags;
    public int HalfMove;
    public int FullMove;
}

public class Board
{
    public readonly Bitboards Bitboards;
    public Colour TurnToMove;

    // bit field - from the lowest bit in this order White : K, Q, Black K,Q
    public int CastlingRights { get; private set; }
    public Square? EnPassantSquare { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }
    private readonly Stack<BoardState> _boardStateHistory;

    public Board(Bitboards bitboards, Colour turnToMove = Colour.White)
    {
        Bitboards = bitboards;
        TurnToMove = turnToMove;
        _boardStateHistory = new Stack<BoardState>();
    }

    public Board(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        Bitboards = new Bitboards(fen);
        ApplyBoardStateFromFen(fen);
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

        var enPassantString = EnPassantSquare.HasValue ? EnPassantSquare.Value.Notation : "-";
        builtFen += enPassantString;
        builtFen += $" {HalfMoves}";
        builtFen += $" {FullMoves}";

        return builtFen;
    }


    public void ApplyMove(Move move)
    {
        ApplyMoveFlags(ref move);

        var opponentColour = move.PieceMoved.Colour == Colour.White ? Colour.Black : Colour.White;
        Piece? capturedPiece;
        Square? capturedSquare;

        if (move.IsEnPassant)
        {
            capturedPiece = Piece.MakePiece(PieceType.Pawn, opponentColour);
            var captureRank = opponentColour == Colour.White ? 3 : 4;
            capturedSquare = new Square(captureRank, move.To.FileIndex);
        }
        else
        {
            capturedPiece = Bitboards.PieceAtSquare(move.To);
            capturedSquare = move.To;
        }

        var state = new BoardState
        {
            CapturedPiece = capturedPiece,
            EnPassantSquare = EnPassantSquare,
            CastlingRights = CastlingRights,
            LastMoveFlags = move.MoveFlag,
            FullMove = FullMoves,
            HalfMove = HalfMoves
        };
        _boardStateHistory.Push(state);

        // action the required change for the moving piece
        MovePiece(move.PieceMoved, move.From, move.To);

        // get rid of the captured piece
        if (capturedPiece.HasValue)
            Bitboards.SetOff(capturedPiece.Value, capturedSquare.Value);


        // action the promotion
        if (move.IsPromotion && move.PromotedPiece.HasValue)
        {
            Bitboards.SetOff(move.PieceMoved, move.To);
            Bitboards.SetOn(move.PromotedPiece.Value, move.To);
        }


        // handle castling
        if (move.IsCastling)
        {
            var affectedRook = move.PieceMoved.Colour == Colour.White
                ? new Piece(PieceType.Rook, Colour.White)
                : new Piece(PieceType.Rook, Colour.Black);

            var rookNewFile = move.To.FileIndex == 2 ? 3 : 5;
            var rookOldFile = move.To.FileIndex == 2 ? 0 : 7;

            MovePiece(affectedRook,
                new Square(move.To.RankIndex, rookOldFile),
                new Square(move.To.RankIndex, rookNewFile));
        }

        // king moving always sacrifices the castling rights
        UpdateCastlingRights(move);

        // set en passant target square
        if (move.PieceMoved.Type == PieceType.Pawn && Math.Abs(move.From.RankIndex - move.To.RankIndex) == 2)
        {
            var targetRank = move.PieceMoved.Colour == Colour.White ? 2 : 5;
            EnPassantSquare = new Square(targetRank, move.From.FileIndex);
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

    public void UndoMove(Move move)
    {
        //ApplyMoveFlags(move: ref move);


        var previousState = _boardStateHistory.Pop();
        EnPassantSquare = previousState.EnPassantSquare;
        CastlingRights = previousState.CastlingRights;
        HalfMoves = previousState.HalfMove;
        FullMoves = previousState.FullMove;
        move.MoveFlag = previousState.LastMoveFlags;
        if (previousState.CapturedPiece.HasValue)
        {
            if (!move.IsEnPassant)
                Bitboards.SetOn(previousState.CapturedPiece.Value, move.To);
        }


        MovePiece(move.PieceMoved, move.To, move.From);

        if (move.IsPromotion && move.PromotedPiece.HasValue)
        {
            Bitboards.SetOff(move.PromotedPiece.Value, move.To);
        }

        if (move.IsCastling)
        {
            var rank = move.To.RankIndex;
            var rookHomeFile = move.To.FileIndex > 4 ? 7 : 0;
            var rookNewFile = move.To.FileIndex == 2 ? 3 : 5;
            Bitboards.SetOn(Piece.MakePiece(PieceType.Rook, move.PieceMoved.Colour), new Square(rank, rookHomeFile));
            Bitboards.SetOff(Piece.MakePiece(PieceType.Rook, move.PieceMoved.Colour), new Square(rank, rookNewFile));
        }

        if (move.IsEnPassant)
        {
            var pawnHomeRank = move.PieceMoved.Colour == Colour.Black ? 3 : 4;
            var capturedColour = move.PieceMoved.Colour == Colour.White ? Colour.Black : Colour.White;
            Bitboards.SetOn(Piece.MakePiece(PieceType.Pawn, capturedColour),
                new Square(pawnHomeRank, move.To.FileIndex));
        }


        SwapTurns();
    }

    private void ApplyMoveFlags(ref Move move)
    {
        if (move.PieceMoved.Type == PieceType.Pawn &&
            ((move.PieceMoved.Colour == Colour.White && move.To.RankIndex == 7) ||
             (move.PieceMoved.Colour == Colour.Black && move.To.RankIndex == 0)))
        {
            move.MoveFlag |= MoveFlags.Promotion;
        }

        if (move.PieceMoved.Type == PieceType.King && Math.Abs(move.From.FileIndex - move.To.FileIndex) > 1)
        {
            move.MoveFlag |= MoveFlags.Castle;
        }

        if (move.PieceMoved.Type == PieceType.Pawn && (move.From.FileIndex - move.To.FileIndex) != 0 &&
            !Bitboards.PieceAtSquare(move.To).HasValue)
            move.MoveFlag |= MoveFlags.EnPassant;
    }

    private void UpdateCastlingRights(Move move)
    {
        if (move.PieceMoved.Type == PieceType.King)
        {
            if (move.PieceMoved.Colour == Colour.White)
            {
                CastlingRights &= ~(BoardConstants.WhiteKingsideCastlingFlag);
                CastlingRights &= ~(BoardConstants.WhiteQueensideCastlingFlag);
            }
            else
            {
                CastlingRights &= ~(BoardConstants.BlackKingsideCastlingFlag);
                CastlingRights &= ~(BoardConstants.BlackQueensideCastlingFlag);
            }
        }

        if (move.PieceMoved.Type != PieceType.Rook) return;

        if (move.PieceMoved.Colour == Colour.White)
        {
            if (move.From.SquareIndex == BoardConstants.A1)
                CastlingRights &= ~BoardConstants.WhiteQueensideCastlingFlag;
            if (move.From.SquareIndex == BoardConstants.H1)
                CastlingRights &= ~BoardConstants.WhiteKingsideCastlingFlag;
        }
        else
        {
            if (move.From.SquareIndex == BoardConstants.A8)
                CastlingRights &= ~BoardConstants.BlackQueensideCastlingFlag;
            if (move.From.SquareIndex == BoardConstants.H8)
                CastlingRights &= ~BoardConstants.BlackKingsideCastlingFlag;
        }
    }

    private void MovePiece(Piece piece, Square from, Square to)
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
        var colourToMoveTokenLocation = fen.IndexOf(' ') + 1;
        var castlingRightsTokenLocation = fen.IndexOf(' ', colourToMoveTokenLocation) + 1;
        var enPassantSquareTokenLocation = fen.IndexOf(' ', castlingRightsTokenLocation) + 1;
        var halfMoveTokenLocation = fen.IndexOf(' ', enPassantSquareTokenLocation) + 1;
        var fullMoveTokenLocation = fen.IndexOf(' ', halfMoveTokenLocation) + 1;

        TurnToMove = fen[colourToMoveTokenLocation] == 'w' ? Colour.White : Colour.Black;

        var castlingString = fen[castlingRightsTokenLocation..(enPassantSquareTokenLocation - 1)];
        if (castlingString.Contains('K')) CastlingRights |= BoardConstants.WhiteKingsideCastlingFlag;
        if (castlingString.Contains('Q')) CastlingRights |= BoardConstants.WhiteQueensideCastlingFlag;
        if (castlingString.Contains('k')) CastlingRights |= BoardConstants.BlackKingsideCastlingFlag;
        if (castlingString.Contains('q')) CastlingRights |= BoardConstants.BlackQueensideCastlingFlag;

        var enPassantString = fen[enPassantSquareTokenLocation..(fullMoveTokenLocation - 1)];
        if (enPassantString.Length == 2)
        {
            EnPassantSquare = new Square(enPassantString);
        }

        var halfMoveTokenValue = int.Parse(fen[halfMoveTokenLocation..(fullMoveTokenLocation - 1)]);
        var fullMoveTokenValue = int.Parse(fen[fullMoveTokenLocation..]);

        HalfMoves = halfMoveTokenValue;
        FullMoves = fullMoveTokenValue;
    }
}