namespace Onyx.Core;

using FenString = string;

public struct FenDetails
{
    public string PositionFen;
    public bool WhiteToMove;
    public string CastlingString;
    public int? EnPassantSquare;
    public int HalfMove;
    public int FullMove;
    public int CastlingRights;
}

public static class Fen
{
    public static void BuildPvString(Move[,] pvTable, int[] pvLengthTable, out List<Move> moveList)
    {
        moveList = new List<Move>(pvLengthTable[0]);
        for (int i = 0; i < pvLengthTable[0]; i++)
        {
            var move = pvTable[0, i];
            if (move.Data == 0) break; // No more moves
            moveList.Add(move);
        } 
    } 
    public static FenDetails FromString(string fen)
    {
        var details = new FenDetails();

        var colourToMoveTokenLocation = fen.IndexOf(' ') + 1;
        details.PositionFen = fen[0..(colourToMoveTokenLocation - 1)];
        var castlingRightsTokenLocation = fen.IndexOf(' ', colourToMoveTokenLocation) + 1;
        var enPassantSquareTokenLocation = fen.IndexOf(' ', castlingRightsTokenLocation) + 1;
        var halfMoveTokenLocation = fen.IndexOf(' ', enPassantSquareTokenLocation) + 1;
        var fullMoveTokenLocation = fen.IndexOf(' ', halfMoveTokenLocation) + 1;

        details.WhiteToMove = fen[colourToMoveTokenLocation] == 'w';

        var castlingString = fen[castlingRightsTokenLocation..(enPassantSquareTokenLocation - 1)];
        details.CastlingString = castlingString;
        
        if (castlingString.Contains('K'))
            details.CastlingRights |= BoardConstants.WhiteKingsideCastlingFlag;
        if (castlingString.Contains('Q'))
            details.CastlingRights |= BoardConstants.WhiteQueensideCastlingFlag;
        if (castlingString.Contains('k'))
            details.CastlingRights |= BoardConstants.BlackKingsideCastlingFlag;
        if (castlingString.Contains('q'))
            details.CastlingRights |= BoardConstants.BlackQueensideCastlingFlag;

        var enPassantString = fen[enPassantSquareTokenLocation..(halfMoveTokenLocation - 1)];
        if (enPassantString.Length == 2)
        {
            details.EnPassantSquare = RankAndFile.SquareIndex(enPassantString);
        }
        else
        {
            details.EnPassantSquare = null;
        }

        var halfMoveTokenValue = int.Parse(fen[halfMoveTokenLocation..(fullMoveTokenLocation - 1)]);
        var fullMoveTokenValue = int.Parse(fen[fullMoveTokenLocation..]);

        details.HalfMove = halfMoveTokenValue;
        details.FullMove = fullMoveTokenValue;

        return details;
    }

    public const FenString DefaultFen   = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const FenString KiwiPeteFen  = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    public const FenString Pos3Fen      = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    public const FenString Pos4Fen      = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    public const FenString Pos5Fen      = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

    public static sbyte GetPieceFromChar(char pieceChar)
    {
        return pieceChar switch
        {
            'K' => Piece.WK,
            'B' => Piece.WB,
            'R' => Piece.WR,
            'N' => Piece.WN,
            'P' => Piece.WP,
            'Q' => Piece.WQ,
            'k' => Piece.BK,
            'b' => Piece.BB,
            'r' => Piece.BR,
            'n' => Piece.BN,
            'p' => Piece.BP,
            'q' => Piece.BQ,
            _ => throw new ArgumentException()
        };
    }

    public static char GetCharFromPiece(sbyte piece)
    {
        var lowerVersion = Piece.PieceType(piece) switch
        {
            Piece.King => 'k',
            Piece.Queen => 'q',
            Piece.Bishop => 'b',
            Piece.Rook => 'r',
            Piece.Knight => 'n',
            Piece.Pawn => 'p',
            _ => throw new ArgumentOutOfRangeException()
        };

        return Piece.IsWhite(piece) ? char.ToUpper(lowerVersion) : lowerVersion;
    }
}