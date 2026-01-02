using System.Runtime.CompilerServices;


namespace Onyx.Core;

public class Bitboards
{
    public Bitboards()
    {
        Boards = new ulong[12];
        AllPieces = 0;
        _whitePieces = 0;
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
    }

    public Bitboards(string fenString)
    {
        Boards = new ulong[12];
        AllPieces = 0;
        _whitePieces = 0;
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
        LoadFen(fenString);
    }

    public void LoadFen(string fenString)
    {
        // reset the boards
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
        _whitePieces = 0;
        AllPieces = 0;

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

    public ulong AllPieces { get; private set; }
    private ulong _whitePieces;
    public ulong[] Boards { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong OccupancyByPiece(sbyte piece)
    {
        return Boards[Piece.BitboardIndex(piece)];
    }
    public ulong OccupancyByColour(bool forBlack)
    {
        if (!forBlack)
            return _whitePieces;

        return AllPieces & ~_whitePieces;
        //var pieces = forBlack ? Piece._blackPieces : Piece._whitePieces;
        //return pieces.Aggregate(0ul, (current, piece) => current | OccupancyByPiece(piece));
    }

    public ulong Occupancy()
    {
        return AllPieces;
    }

    public void SetByPiece(sbyte piece, ulong boardByPiece)
    {
        Boards[Piece.BitboardIndex(piece)] = boardByPiece;
        AllPieces = 0;
        _whitePieces = 0;
        for (var i = 0; i < 12; i++)
        {
            if (Piece.IsWhite(Piece.AllPieces[i]))
                _whitePieces |= Boards[i];
            AllPieces |= Boards[i];
        }
    }

    public void SetAllOff(int square)
    {
        var index = 1ul << square;
        AllPieces &= ~index;
        _whitePieces &= ~index;
        for (var i = 0; i < Boards.Length; i++)
        {
            Boards[i] &= ~index;
        }
    }

    public void SetOff(sbyte piece, int square)
    {
        var bit = 1ul << square;
        var index = Piece.BitboardIndex(piece);

        if ((Boards[index] & bit) != 0)
        {
            Boards[index] &= ~bit;

            // Only clear _allPieces if no other piece is on this square
            var stillOccupied = false;
            for (var i = 0; i < 12; i++)
            {
                if ((Boards[i] & bit) != 0)
                {
                    stillOccupied = true;
                    break;
                }
            }
            if (!stillOccupied)
            {
                AllPieces &= ~bit;
                if (Piece.IsWhite(piece))
                    _whitePieces &= ~bit;
            }
        }
    }

    public void SetOn(sbyte piece, int square)
    {
        var index = 1ul << square;
        Boards[Piece.BitboardIndex(piece)] |= index;
        AllPieces |= index;
        if (Piece.IsWhite(piece))
            _whitePieces |= index;
    }

    public bool SquareOccupied(int squareToTest)
    {
        return (AllPieces & (1ul << squareToTest)) > 0;
    }

    public sbyte? PieceAtSquare(int squareToTest)
    {
        var mask = 1UL << squareToTest;
        if ((AllPieces & (1ul << squareToTest)) == 0) return null;
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