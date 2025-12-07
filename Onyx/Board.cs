

namespace Onyx;

public static class BoardConstants
{
    public static readonly int WhiteKingsideCastling = 1 << 0;
    public static readonly int WhiteQueensideCastling = 1 << 1;
    public static readonly int BlackKingsideCastling = 1 <<2;
    public static readonly int BlackQueensideCastling = 1 <<3;
}
public class Board
{
    public Bitboards bitboards;
    public Colour TurnToMove;

    // bit field - from lowest bit in this order White : K, Q, Black K,Q
    public int CastlingRights = 0;
    public Square? enPassantSquare = null;

    public Board(Bitboards bitboards, Colour turnToMove = Colour.White)
    {
        this.bitboards = bitboards;
        TurnToMove = turnToMove;
    }

    public Board(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        bitboards = new Bitboards(fen);

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
            enPassantSquare = new Square(enPassantString);
        }
    }

    public void MakeMove(Move move)
    {
        
    }

    private void MovePiece(Piece piece, Square from, Square to)
    {
        bitboards.SetOff(piece,from);
        bitboards.SetOn(piece,to);
    }
}