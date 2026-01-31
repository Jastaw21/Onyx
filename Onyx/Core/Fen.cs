using Onyx.Statics;

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
            details.CastlingRights |= BoardHelpers.WhiteKingsideCastlingFlag;
        if (castlingString.Contains('Q'))
            details.CastlingRights |= BoardHelpers.WhiteQueensideCastlingFlag;
        if (castlingString.Contains('k'))
            details.CastlingRights |= BoardHelpers.BlackKingsideCastlingFlag;
        if (castlingString.Contains('q'))
            details.CastlingRights |= BoardHelpers.BlackQueensideCastlingFlag;

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

    public static byte GetPieceFromChar(char pieceChar)
    {
        return pieceChar switch
        {
            'K' => PieceTypes.WK,
            'B' => PieceTypes.WB,
            'R' => PieceTypes.WR,
            'N' => PieceTypes.WN,
            'P' => PieceTypes.WP,
            'Q' => PieceTypes.WQ,
            'k' => PieceTypes.BK,
            'b' => PieceTypes.BB,
            'r' => PieceTypes.BR,
            'n' => PieceTypes.BN,
            'p' => PieceTypes.BP,
            'q' => PieceTypes.BQ
           
        };
    }

    public static char GetCharFromPiece(byte piece)
    {
        var lowerVersion = PieceTypes.PieceType(piece) switch
        {
            PieceTypes.King => 'k',
            PieceTypes.Knight => 'n',
            PieceTypes.Bishop => 'b',
            PieceTypes.Rook => 'r',
            PieceTypes.Queen => 'q',
            PieceTypes.Pawn => 'p',
        };

        return PieceTypes.IsWhite(piece) ? char.ToUpper(lowerVersion) : lowerVersion;
    }
}