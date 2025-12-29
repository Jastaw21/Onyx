namespace Onyx.Core;

public static class PreMoveFlags
{
    public static readonly int NoFlag = 0;
    public static readonly int EnPassant = 1 << 0;
    public static readonly int Promotion = 1 << 1;
    public static readonly int Castle = 1 << 2;
}

public static class PostMoveFlags
{
    public static readonly int NoFlag = 0;
    public static readonly int Capture = 0;
}

public struct Move
{
    public Move(Piece pieceMoved, Square from, Square to)
    {
        PieceMoved = pieceMoved;
        From = from;
        To = to;
    }

    public Move(Piece pieceMoved, string notation)
    {
        PieceMoved = pieceMoved;
        var fromSquare = notation[..2];
        var toSquare = notation.Length == 4 ? notation[^2..] : notation[2..5];

        if (notation.Length == 5)
        {
            PromotedPiece = notation[4] switch
            {
                'q' => Piece.MakePiece(PieceType.Queen, pieceMoved.Colour),
                'b' => Piece.MakePiece(PieceType.Bishop, pieceMoved.Colour),
                'n' => Piece.MakePiece(PieceType.Knight, pieceMoved.Colour),
                'r' => Piece.MakePiece(PieceType.Rook, pieceMoved.Colour),
                _ => PromotedPiece
            };
        }

        From = new Square(fromSquare);
        To = new Square(toSquare);
    }

    public Piece PieceMoved { get; }
    public Square From { get; }
    public Square To { get; }

    public Piece? PromotedPiece = null;
    public int PreMoveFlag = PreMoveFlags.NoFlag;
    public int PostMoveFlag = PostMoveFlags.NoFlag;

    public string Notation => From.Notation + To.Notation +
                              (PromotedPiece.HasValue ? Fen.GetCharFromPiece(PromotedPiece.Value) : "");

    public bool IsPromotion => (PreMoveFlag & PreMoveFlags.Promotion) > 0;
    public bool IsCastling => (PreMoveFlag & PreMoveFlags.Castle) > 0;
    public bool IsEnPassant => (PreMoveFlag & PreMoveFlags.EnPassant) > 0;

    public override string ToString()
    {
        return Notation;
    }
}