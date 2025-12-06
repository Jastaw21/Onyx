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

    public Bitboards(string Fenstring)
    {
        var pieceTypeCount = Enum.GetValues<PieceType>().Length;
        var colourCount = Enum.GetValues<Colour>().Length;

        boards = new ulong[colourCount * pieceTypeCount];
        
        int rankIndex = 7; // fen starts from the top
        int fileIndex = 0;

        int currentIndex = 0;

        while (currentIndex < Fenstring.Length)
        {
            // next line indicator
            if (Fenstring[currentIndex] == '/')
            {
                rankIndex--; // move to the next rank down
                fileIndex = 0; // and back to the start
            }

            // empty cells indicator
            else if (Char.IsAsciiDigit(Fenstring[currentIndex]))
                fileIndex += Fenstring[currentIndex] - '0';
            
            // break at space, as the rest if=s all castling/en passant stuff, not relevant to us
            else if (Fenstring[currentIndex] == ' ')
                break;

            // thos os a piece, so set it and move the file on
            else
            {
                var piece = PieceHelpers.GetPieceFromChar(Fenstring[currentIndex]);
                SetOn(new Square(rankIndex, fileIndex), piece);
                fileIndex++;
            }
            currentIndex++;
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