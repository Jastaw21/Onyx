namespace Onyx.Core;

using FenString = string;

public static class Fen
{
    public const FenString DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const FenString KiwiPeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    public const FenString Pos3Fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";

    public static Piece GetPieceFromChar(char pieceChar)
    {
        return pieceChar switch
        {
            'K' => new Piece(PieceType.King, Colour.White),
            'B' => new Piece(PieceType.Bishop, Colour.White),
            'R' => new Piece(PieceType.Rook, Colour.White),
            'N' => new Piece(PieceType.Knight, Colour.White),
            'P' => new Piece(PieceType.Pawn, Colour.White),
            'Q' => new Piece(PieceType.Queen, Colour.White),
            'k' => new Piece(PieceType.King, Colour.Black),
            'b' => new Piece(PieceType.Bishop, Colour.Black),
            'r' => new Piece(PieceType.Rook, Colour.Black),
            'n' => new Piece(PieceType.Knight, Colour.Black),
            'p' => new Piece(PieceType.Pawn, Colour.Black),
            'q' => new Piece(PieceType.Queen, Colour.Black),
            _ => throw new ArgumentException()
        };
    }

    public static char GetCharFromPiece(Piece piece)
    {
        var lowerVersion = piece.Type switch
        {
            PieceType.King => 'k',
            PieceType.Queen => 'q',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Knight => 'n',
            PieceType.Pawn => 'p',
            _ => throw new ArgumentOutOfRangeException()
        };

        return piece.Colour == Colour.Black ? lowerVersion : char.ToUpper(lowerVersion);
    }
}