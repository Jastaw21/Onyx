namespace Onyx.UCI;

public enum TokenType
{
    UCI,GO,Depth,Perft,IntLiteral
}

public struct Token
{
    public TokenType Type;
    public string value;
}