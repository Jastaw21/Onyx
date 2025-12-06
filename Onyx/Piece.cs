namespace Onyx;

public enum PieceType
{
    Pawn = 0,
    Knight = 1,
    Bishop = 2,
    Rook = 3,
    King = 4,
    Queen = 5
}

public enum Colour
{
    White = 0,
    Black = 1
}

public readonly struct Piece(PieceType type, Colour colour)
{
    public Colour Colour { get; } = colour;
    public PieceType Type { get; } = type;
}

public static class PieceHelpers
{
    public static Piece GetPieceFromChar(char pieceChar)
    {
        return pieceChar switch
        {
            'K' => new Piece(PieceType.King, Colour.White),
            'B' => new Piece(PieceType.Bishop, Colour.White),
            'R' => new Piece(PieceType.Rook, Colour.White),
            'N' => new Piece(PieceType.Knight, Colour.White),
            'P' => new Piece(PieceType.Pawn, Colour.White),
            'Q' => new Piece(PieceType.Queen, Colour.White),
            'k' => new Piece(PieceType.King, Colour.Black),
            'b' => new Piece(PieceType.Bishop, Colour.Black),
            'r' => new Piece(PieceType.Rook, Colour.Black),
            'n' => new Piece(PieceType.Knight, Colour.Black),
            'p' => new Piece(PieceType.Pawn, Colour.Black),
            'q' => new Piece(PieceType.Queen, Colour.Black),
            _ => throw new ArgumentException()
        };
    }
}