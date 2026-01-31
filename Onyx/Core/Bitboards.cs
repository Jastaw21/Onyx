using System.Runtime.CompilerServices;


namespace Onyx.Core;

public class Bitboards
{
    public Bitboards()
    {
        Boards = new ulong[12];
        Pieces = new byte[64];
        AllPieces = 0;
        WhitePieces = 0;
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
    }

    public Bitboards(string fenString)
    {
        Boards = new ulong[12];
        Pieces = new byte[64];
        AllPieces = 0;
        WhitePieces = 0;
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
        LoadFen(fenString);
    }

    public void LoadFen(string fenString)
    {
        // reset the boards
        for (var i = 0; i < Boards.Length; i++) Boards[i] = 0ul;
        WhitePieces = 0;
        AllPieces = 0;
        for (var i = 0; i < Pieces.Length; i++) Pieces[i] = 0;

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
                var sq = RankAndFile.SquareIndex(rankIndex, fileIndex);
                SetOn(piece, sq);
                Pieces[sq] = piece;
                fileIndex++;
            }

            currentIndex++;
        }
    }

    public ulong AllPieces { get; private set; }
    public ulong WhitePieces { get; private set; }
    public ulong[] Boards { get; }
    public byte[] Pieces { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong OccupancyByPiece(byte piece)
    {
        return Boards[PieceTypes.BitboardIndex(piece)];
    }
    public ulong OccupancyByColour(bool forBlack)
    {
        if (!forBlack)
            return WhitePieces;

        return AllPieces & ~WhitePieces;        
    }

    public ulong Occupancy()
    {
        return AllPieces;
    }

    public void SetByPiece(byte piece, ulong boardByPiece)
    {
        Boards[PieceTypes.BitboardIndex(piece)] = boardByPiece;
        AllPieces = 0;
        WhitePieces = 0;
        for (var i = 0; i < 12; i++)
        {
            if (PieceTypes.IsWhite(PieceTypes.AllPieces[i]))
                WhitePieces |= Boards[i];
            AllPieces |= Boards[i];          
        }
        var localBoard = boardByPiece;
        while (localBoard != 0)
        {
            var square = ulong.TrailingZeroCount(localBoard);
            Pieces[square] = piece;
            localBoard &= localBoard - 1;
        }
    }

    public void SetAllOff(int square)
    {
        var index = 1ul << square;
        AllPieces &= ~index;
        WhitePieces &= ~index;
        Pieces[square] = 0;
        for (var i = 0; i < Boards.Length; i++)
        {
            Boards[i] &= ~index;
        }
    }

    public void SetOff(byte piece, int square)
    {
        var bit = 1ul << square;
        var index = PieceTypes.BitboardIndex(piece);

        if ((Boards[index] & bit) != 0)
        {
            Boards[index] &= ~bit;
            Pieces[square] = 0;

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
                if (PieceTypes.IsWhite(piece))
                    WhitePieces &= ~bit;
            }
        }
    }

    public void SetOn(byte piece, int square)
    {
        var index = 1ul << square;
        Boards[PieceTypes.BitboardIndex(piece)] |= index;
        Pieces[square] = piece;
        AllPieces |= index;
        if (PieceTypes.IsWhite(piece))
            WhitePieces |= index;
    }

    public bool SquareOccupied(int squareToTest)
    {
        return Pieces[squareToTest] != 0;
    }

    public byte PieceAtSquare(int squareToTest)
    {
        return Pieces[squareToTest];
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

                if (pieceHere != 0)
                {
                    var key = Fen.GetCharFromPiece(pieceHere);

                    // we were tracking empty squares, so write them first
                    if (numberEmptySquares > 0)
                    {
                        builtFen += numberEmptySquares;
                        numberEmptySquares = 0; // reset the tracking
                    }

                    builtFen += key;
                }

                if (pieceHere == 0)
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