namespace Onyx;

public struct Move(Piece piece, Square from, Square to)
{
    public Square From { get; } = from;
    public Square To { get; } = to;
    public Piece Piece1 { get; } = piece;

    public Piece? CapturedPiece = null;
    public Piece? PromotedPiece = null;

    public string Notation
    {
        get
        {
            return From.Notation + To.Notation;
        }
    }
}