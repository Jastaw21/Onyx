using System.Runtime.CompilerServices;


namespace Onyx.Core;

public class Bitboards
{
    public Bitboards()
    {
        _boards = new ulong[12];
        for (int i = 0; i < _boards.Length; i++) _boards[i] = 0ul;
    }

    public Bitboards(string fenString)
    {
        _boards = new ulong[12];
        for (int i = 0; i < _boards.Length; i++) _boards[i] = 0ul;
        LoadFen(fenString);
    }

    public void LoadFen(string fenString)
    {
        // reset the boards
        for (int i = 0; i < _boards.Length; i++) _boards[i] = 0ul;
        
        var rankIndex = 7; // fen starts from the top
        var fileIndex = 0;

        var currentIndex = 0;

        while (currentIndex < fenString.Length)
        {
            // next line indicator
            var pieceChar = fenString[currentIndex];
            if (pieceChar == '/')
            {
                rankIndex--; // move to the next rank down
                fileIndex = 0; // and back to the start
            }

            // empty cells indicator
            else if (Char.IsAsciiDigit(pieceChar))
                fileIndex += pieceChar - '0';

            // break at space, as the rest is all castling/en passant stuff, not relevant to us
            else if (pieceChar == ' ')
                break;

            // this is a piece, so set it and move the file on
            else
            {
                var piece = Fen.GetPieceFromChar(pieceChar);
                SetOn(piece, RankAndFile.SquareIndex(rankIndex, fileIndex));
                fileIndex++;
            }

            currentIndex++;
        }
    }

    private ulong[] _boards;
    public ulong[] Boards => _boards;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong OccupancyByPiece(sbyte piece)
    {
        return _boards[Pc.BitboardIndex(piece)];
    }

    public ulong OccupancyByColour(bool forBlack)
    {
        var pieces = forBlack ? Pc._blackPieces : Pc._whitePieces;
        return pieces.Aggregate(0ul, (current, piece) => current | OccupancyByPiece(piece));
    }

    public ulong Occupancy()
    {
        return Boards.Aggregate(0ul, (current, board) => current | board);
    }

    public void SetByPiece(sbyte piece, ulong boardByPiece)
    {
        _boards[Pc.BitboardIndex(piece)] = boardByPiece;       
    }

    public void SetAllOff(int square)
    {
        var index = 1ul << square;
        for (var i = 0; i < _boards.Length; i++)
        {
            _boards[i] &= ~index;
        }      

    }

    public void SetOff(sbyte piece, int square)
    {
        _boards[Pc.BitboardIndex(piece)] &= ~(1ul << square);        
    }

    public void SetOn(sbyte piece, int square)
    {
        _boards[Pc.BitboardIndex(piece)] |= 1ul << square;        
    }

    public bool SquareOccupied(int squareToTest)
    {       
        return _boards.Any(pieceBoard => (pieceBoard & (1ul << squareToTest)) > 0);
    }

    public sbyte? PieceAtSquare(int squareToTest)
    {    
        ulong mask = 1UL << squareToTest;
        var pieces = Pc.AllPieces;
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
                var pieceHere = PieceAtSquare(RankAndFile.SquareIndex(rankIndex, fileIndex));

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