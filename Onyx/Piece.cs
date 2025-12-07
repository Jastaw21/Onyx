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

    public override string ToString()
    {
        return $"{Colour} {Type}";
    }
    
    public static List<Piece> All()
    {
        List<Piece> pieces = [];
        pieces.AddRange(from type in Enum.GetValues<PieceType>()
            from colour in Enum.GetValues<Colour>()
            select new Piece(type, colour));

        return pieces;
    }
}
