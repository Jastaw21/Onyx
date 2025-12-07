namespace Onyx;

public static class BoardConstants
{
    public static readonly int WhiteKingsideCastling = 1 << 0;
    public static readonly int WhiteQueensideCastling = 1 << 1;
    public static readonly int BlackKingsideCastling = 1 << 2;
    public static readonly int BlackQueensideCastling = 1 << 3;

    public static readonly int a1 = 0;
    public static readonly int h1 = 7;
    public static readonly int a8 = 56;
    public static readonly int h8 = 63;
}

public class Board
{
    public Bitboards Bitboards;
    public Colour TurnToMove;

    public int
        CastlingRights { get; private set; } // bit field - from the lowest bit in this order White : K, Q, Black K,Q

    public Square? EnPassantSquare = null;

    public Board(Bitboards bitboards, Colour turnToMove = Colour.White)
    {
        this.Bitboards = bitboards;
        TurnToMove = turnToMove;
    }

    public Board(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        Bitboards = new Bitboards(fen);

        ApplyBoardStateFromFen(fen);
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
        if ((CastlingRights & BoardConstants.WhiteKingsideCastling) > 0) castlingRightsString += 'K';
        if ((CastlingRights & BoardConstants.WhiteQueensideCastling) > 0) castlingRightsString += 'Q';
        if ((CastlingRights & BoardConstants.BlackKingsideCastling) > 0) castlingRightsString += 'k';
        if ((CastlingRights & BoardConstants.BlackQueensideCastling) > 0) castlingRightsString += 'q';

        if (castlingRightsString.Length == 0)
            castlingRightsString = "- ";
        else
        {
            castlingRightsString += " ";
        }

        builtFen += castlingRightsString;
        builtFen += "- 0 1";

        return builtFen;
    }

    /// <summary>
    /// Applies a move, which should have been checked for legality first. However, it will scan the move for special flags like castling/promotion etc
    /// </summary>
    /// <param name="move">The move to apply. </param>
    public void ApplyMove(Move move)
    {
        ApplyMoveFlags(ref move);
        var opponentColour = move.PieceMoved.Colour == Colour.White ? Colour.Black : Colour.White;

        Piece? capturedPiece;
        Square? capturedSquare;

        if ((move.MoveFlag & MoveFlags.EnPassant) > 0)
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


        // action the required change for the moving piece
        MovePiece(move.PieceMoved, move.From, move.To);

        // get rid of the captured piece
        if (capturedPiece.HasValue)
            Bitboards.SetOff(capturedPiece.Value, capturedSquare.Value);


        // action the promotion
        if ((move.MoveFlag & MoveFlags.Promotion) > 0)
        {
            Bitboards.SetOff(move.PieceMoved, move.To);
            Bitboards.SetOn(move.PromotedPiece.Value, move.To);
        }


        // handle castling
        if ((move.MoveFlag & MoveFlags.Castle) > 0)
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
    }

    private void UpdateCastlingRights(Move move)
    {
        if (move.PieceMoved.Type == PieceType.King)
        {
            if (move.PieceMoved.Colour == Colour.White)
            {
                CastlingRights &= ~(BoardConstants.WhiteKingsideCastling);
                CastlingRights &= ~(BoardConstants.WhiteQueensideCastling);
            }
            else
            {
                CastlingRights &= ~(BoardConstants.BlackKingsideCastling);
                CastlingRights &= ~(BoardConstants.BlackQueensideCastling);
            }
        }

        if (move.PieceMoved.Type != PieceType.Rook) return;

        if (move.PieceMoved.Colour == Colour.White)
        {
            if (move.From.SquareIndex == BoardConstants.a1)
                CastlingRights &= ~BoardConstants.WhiteQueensideCastling;
            if (move.From.SquareIndex == BoardConstants.h1)
                CastlingRights &= ~BoardConstants.WhiteKingsideCastling;
        }
        else
        {
            if (move.From.SquareIndex == BoardConstants.a8)
                CastlingRights &= ~BoardConstants.BlackQueensideCastling;
            if (move.From.SquareIndex == BoardConstants.h8)
                CastlingRights &= ~BoardConstants.BlackKingsideCastling;
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
        var fullMoveTokenLocation = fen.IndexOf(' ', enPassantSquareTokenLocation) + 1;
        var halfMoveTokenLocation = fen.IndexOf(' ', fullMoveTokenLocation);

        TurnToMove = fen[colourToMoveTokenLocation] == 'w' ? Colour.White : Colour.Black;

        var castlingString = fen[castlingRightsTokenLocation..(enPassantSquareTokenLocation - 1)];
        if (castlingString.Contains('K')) CastlingRights |= BoardConstants.WhiteKingsideCastling;
        if (castlingString.Contains('Q')) CastlingRights |= BoardConstants.WhiteQueensideCastling;
        if (castlingString.Contains('k')) CastlingRights |= BoardConstants.BlackKingsideCastling;
        if (castlingString.Contains('q')) CastlingRights |= BoardConstants.BlackQueensideCastling;

        var enPassantString = fen[enPassantSquareTokenLocation..(fullMoveTokenLocation - 1)];
        if (enPassantString.Length == 2)
        {
            EnPassantSquare = new Square(enPassantString);
        }
    }
}