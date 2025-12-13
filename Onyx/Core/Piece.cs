namespace Onyx.Core;

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

    public static List<Piece> PromotionTypes(Colour colour)
    {
        return
        [
            new Piece(PieceType.Bishop, colour), new Piece(PieceType.Queen, colour), new Piece(PieceType.Rook, colour),
            new Piece(PieceType.Knight, colour)
        ];
    }

    public static Piece MakePiece(PieceType piece, Colour colour)
    {
        return new Piece(piece, colour);
    }

    public static Piece WP => new(PieceType.Pawn, Colour.White);
    public static Piece BP => new(PieceType.Pawn, Colour.Black);
    public static Piece WR => new(PieceType.Rook, Colour.White);
    public static Piece BR => new(PieceType.Rook, Colour.Black);
    public static Piece WN => new(PieceType.Knight, Colour.White);
    public static Piece BN => new(PieceType.Knight, Colour.Black);
    public static Piece WQ => new(PieceType.Queen, Colour.White);
    public static Piece BQ => new(PieceType.Queen, Colour.Black);
    public static Piece WK => new(PieceType.King, Colour.White);
    public static Piece BK => new(PieceType.King, Colour.Black);
    public static Piece WB => new(PieceType.Bishop, Colour.White);
    public static Piece BB => new(PieceType.Bishop, Colour.Black);

    public static Piece[] ByColour(Colour colour)
    {
        var pieces = Enum.GetValues<PieceType>();
        var builtPieces = new Piece[pieces.Length];

        for (var i = 0; i < pieces.Length; i++)
        {
            builtPieces[i] = new Piece(pieces[i], colour);
        }

        return builtPieces;
    }
}