namespace Onyx;

public class Bitboards
{
    public Bitboards()
    {
        var pieceTypeCount = Enum.GetValues<PieceType>().Length;
        var colourCount = Enum.GetValues<Colour>().Length;

        boards = new ulong[colourCount * pieceTypeCount];

        for (int colourIndex = 0; colourIndex < colourCount; colourIndex++)
        for (int pieceTypeIndex = 0; pieceTypeIndex < pieceTypeCount; pieceTypeIndex++)
        {
            boards[colourIndex * pieceTypeCount + pieceTypeIndex] = 0ul;
        }
    }

    private ulong[] boards;
    public ulong[] Boards => boards;

    public ulong GetByPiece(Piece piece)
    {
        var col = (int)piece.Colour;
        var type = (int)piece.Type;
        var index = col * Enum.GetValues<PieceType>().Length + type;

        return boards[index];
    }

    public void SetByPiece(Piece piece, ulong boardByPiece)
    {
        var col = (int)piece.Colour;
        var type = (int)piece.Type;
        var index = col * Enum.GetValues<PieceType>().Length + type;

        boards[index] = boardByPiece;
    }

    public void SetZero(Square square)
    {
        for (var i = 0; i < boards.Length; i++)
        {
            boards[i] &= ~(1ul << square.SquareIndex);
        }
    }

    public void SetOn(Square square, Piece piece)
    {
        var col = (int)piece.Colour;
        var type = (int)piece.Type;
        var index = col * Enum.GetValues<PieceType>().Length + type;

        var value = 1ul << square.SquareIndex;

        boards[index] |= value;
    }

    public bool SquareOccupied(Square squareToTest)
    {
        return boards.Any(pieceBoard => (pieceBoard & (1ul << squareToTest.SquareIndex)) > 0);
    }
}