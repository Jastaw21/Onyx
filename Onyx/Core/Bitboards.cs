using System.Runtime.CompilerServices;


namespace Onyx.Core;

public class Bitboards
{
    public Bitboards()
    {
        _pieceTypeCount = Enum.GetValues<PieceType>().Length;
        _colourCount = Enum.GetValues<Colour>().Length;

        _boards = new ulong[_colourCount * _pieceTypeCount];

        for (var colourIndex = 0; colourIndex < _colourCount; colourIndex++)
        for (var pieceTypeIndex = 0; pieceTypeIndex < _pieceTypeCount; pieceTypeIndex++)
        {
            _boards[colourIndex * _pieceTypeCount + pieceTypeIndex] = 0ul;
        }
    }

    public Bitboards(string fenString)
    {
        _pieceTypeCount = Enum.GetValues<PieceType>().Length;
        _colourCount = Enum.GetValues<Colour>().Length;

        _boards = new ulong[_colourCount * _pieceTypeCount];

        for (var colourIndex = 0; colourIndex < _colourCount; colourIndex++)
        for (var pieceTypeIndex = 0; pieceTypeIndex < _pieceTypeCount; pieceTypeIndex++)
        {
            _boards[colourIndex * _pieceTypeCount + pieceTypeIndex] = 0ul;
        }

        LoadFen(fenString);
    }

    public void LoadFen(string fenString)
    {
        _boards = new ulong[_colourCount * _pieceTypeCount];
       
        var rankIndex = 7; // fen starts from the top
        var fileIndex = 0;

        var currentIndex = 0;

        while (currentIndex < fenString.Length)
        {
            // next line indicator
            if (fenString[currentIndex] == '/')
            {
                rankIndex--; // move to the next rank down
                fileIndex = 0; // and back to the start
            }

            // empty cells indicator
            else if (Char.IsAsciiDigit(fenString[currentIndex]))
                fileIndex += fenString[currentIndex] - '0';

            // break at space, as the rest is all castling/en passant stuff, not relevant to us
            else if (fenString[currentIndex] == ' ')
                break;

            // this is a piece, so set it and move the file on
            else
            {
                var piece = Fen.GetPieceFromChar(fenString[currentIndex]);
                SetOn(piece, new Square(rankIndex, fileIndex));
                fileIndex++;
            }

            currentIndex++;
        }
    }

    private ulong[] _boards;   
    private int _pieceTypeCount;
    private int _colourCount;
    public ulong[] Boards => _boards;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong OccupancyByPiece(Piece piece)
    {
        int t = (int)piece.Type;
        int colourOffset = piece.Colour == Colour.White ? 0 : 6;
        return _boards[t + colourOffset];
    }

    public ulong OccupancyByColour(Colour colour)
    {
        return Piece.ByColour(colour).Aggregate(0ul, (current, piece) => current | OccupancyByPiece(piece));
    }

    public ulong Occupancy()
    {
        return Boards.Aggregate(0ul, (current, board) => current | board);
    }

    public void SetByPiece(Piece piece, ulong boardByPiece)
    {
        var col = (int)piece.Colour;
        var type = (int)piece.Type;
        var index = col * _pieceTypeCount + type;        
        _boards[index] = boardByPiece;       
    }

    public void SetAllOff(Square square)
    {
        var index = (1ul << square.SquareIndex);
        for (var i = 0; i < _boards.Length; i++)
        {
            _boards[i] &= ~index;
        }      

    }

    public void SetOff(Piece piece, Square square)
    {
        var index = Index(piece);
        var mask = ~(1ul << square.SquareIndex);
        _boards[index] &= mask;        
    }

    public void SetOn(Piece piece, Square square)
    {
        var index = Index(piece);

        var value = 1ul << square.SquareIndex;

        _boards[index] |= value;        
    }

    private int Index(Piece piece)
    {
        var col = (int)piece.Colour;
        var type = (int)piece.Type;
        var index = col * _pieceTypeCount + type;
        return index;
    }

    public bool SquareOccupied(Square squareToTest)
    {       
        return _boards.Any(pieceBoard => (pieceBoard & (1ul << squareToTest.SquareIndex)) > 0);
    }

    public Piece? PieceAtSquare(int squareToTest)
    {    
        ulong mask = 1UL << squareToTest;
        var pieces = Piece.AllPieces;
        foreach (var piece in pieces)
        {
            var board = OccupancyByPiece(piece);            
            if ((board & mask) != 0)
                return piece;
        }

        return null;
    }

    public string GetFen()
    {
        var builtFen = "";

        for (var rankIndex = 7; rankIndex >= 0; rankIndex--)
        {
            var numberEmptySquares = 0;

            for (var fileIndex = 0; fileIndex <= 7; fileIndex++)
            {
                var pieceHere = PieceAtSquare(RankAndFileHelpers.SquareIndex(rankIndex, fileIndex));

                if (pieceHere.HasValue)
                {
                    var key = Fen.GetCharFromPiece(pieceHere.Value);

                    // we were tracking empty squares, so write them first
                    if (numberEmptySquares > 0)
                    {
                        builtFen += numberEmptySquares;
                        numberEmptySquares = 0; // reset the tracking
                    }

                    builtFen += key;
                }

                if (!pieceHere.HasValue)
                    numberEmptySquares++;
            }

            // exiting the rank with remaining empty squares
            if (numberEmptySquares > 0)
                builtFen += numberEmptySquares;

            if (rankIndex > 0)
                builtFen += '/';
        }

        return builtFen;
    }
}