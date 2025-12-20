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
    Moves,
    MoveString
}

public struct Token
{
    public TokenType Type;
    public string Value;
}