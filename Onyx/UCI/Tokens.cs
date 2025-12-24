namespace Onyx.UCI;

public enum TokenType
{
    Uci,
    Go,
    Depth,
    Perft,
    IntLiteral,
    Position,
    Eof,
    Fen,
    Startpos,
    StringLiteral,
    FenString,
    Moves,
    MoveString,
    Colour,
    CastlingString,
    EnPassantString,
    Dash,
    Btime,Wtime,
    Binc,
    Winc
}

public struct Token
{
    public TokenType Type;
    public string Value;
}