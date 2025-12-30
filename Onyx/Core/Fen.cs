namespace Onyx.Core;

using FenString = string;

public struct FenDetails
{
    public string PositionFen;
    public Colour ColourToMove;
    public string CastlingString;
    public int? EnPassantSquare;
    public int HalfMove;
    public int FullMove;
}

public static class Fen
{
    public static FenDetails FromString(string fen)
    {
        var details = new FenDetails();

        var colourToMoveTokenLocation = fen.IndexOf(' ') + 1;
        details.PositionFen = fen[0..(colourToMoveTokenLocation - 1)];
        var castlingRightsTokenLocation = fen.IndexOf(' ', colourToMoveTokenLocation) + 1;
        var enPassantSquareTokenLocation = fen.IndexOf(' ', castlingRightsTokenLocation) + 1;
        var halfMoveTokenLocation = fen.IndexOf(' ', enPassantSquareTokenLocation) + 1;
        var fullMoveTokenLocation = fen.IndexOf(' ', halfMoveTokenLocation) + 1;

        details.ColourToMove = fen[colourToMoveTokenLocation] == 'w' ? Colour.White : Colour.Black;

        var castlingString = fen[castlingRightsTokenLocation..(enPassantSquareTokenLocation - 1)];
        details.CastlingString = castlingString;

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

    public const FenString DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const FenString KiwiPeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    public const FenString Pos3Fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    public const FenString Pos4Fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    public const FenString Pos5Fen = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

    public static sbyte GetPieceFromChar(char pieceChar)
    {
        return pieceChar switch
        {
            'K' => Pc.WK,
            'B' => Pc.WB,
            'R' => Pc.WR,
            'N' => Pc.WN,
            'P' => Pc.WP,
            'Q' => Pc.WQ,
            'k' => Pc.BK,
            'b' => Pc.BB,
            'r' => Pc.BR,
            'n' => Pc.BN,
            'p' => Pc.BP,
            'q' => Pc.BQ,
            _ => throw new ArgumentException()
        };
    }

    public static char GetCharFromPiece(sbyte piece)
    {
        var lowerVersion = Pc.PieceType(piece) switch
        {
            Pc.King => 'k',
            Pc.Queen => 'q',
            Pc.Bishop => 'b',
            Pc.Rook => 'r',
            Pc.Knight => 'n',
            Pc.Pawn => 'p',
            _ => throw new ArgumentOutOfRangeException()
        };

        return Pc.IsWhite(piece) ? char.ToUpper(lowerVersion) : lowerVersion;
    }
}