namespace Onyx.UCI;

public enum TokenType
{
    UCI,
    GO,
    Depth,
    Perft,
    IntLiteral,
    Position,
    EOF,
    Fen,
    Startpos,
    StringLiteral,
    FenString,
    Moves,
    MoveString,
    Colour,
    CastlingString,
    EnPassantString,
    Dash
}

public struct Token
{
    public TokenType Type;
    public string Value;
}