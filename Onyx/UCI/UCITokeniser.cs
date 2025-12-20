namespace Onyx.UCI;

public class Tokeniser
{
    private string str;
    private int currentIndex;
    public List<Token> tokens;


    public Tokeniser(string stringIn)
    {
        str = stringIn;
        var builtToken = "";
        tokens = [];
        while (currentIndex < str.Length)
        {
            if (str[currentIndex] == ' ')
            {
                HandleToken(builtToken);
                builtToken = "";
            }
            else
            {
                builtToken += str[currentIndex];
            }

            currentIndex++;
        }

        if (!(builtToken.Length == 0))
            HandleToken(builtToken);
    }

    private void HandleToken(string builtToken)
    {
        TokenType type;
        if (builtToken == "uci")
            type = (TokenType.UCI);
        else if (builtToken == "go")
            type = (TokenType.GO);
        else if (builtToken == "depth")
            type = (TokenType.Depth);
        else if (builtToken == "perft")
            type = TokenType.Perft;
        else if (int.TryParse(builtToken, out var result))
            type = (TokenType.IntLiteral);
        else if (builtToken == "fen")
            type = TokenType.Fen;
        else if (builtToken == "startpos")
            type = TokenType.Startpos;
        else if (builtToken == "moves")
            type = TokenType.Moves;
        else if (builtToken == "position")
            type = TokenType.Position;

        else
        {
            var optionalType = ParseUnknownToken(builtToken);
            if (!optionalType.HasValue)
                throw new ArgumentException($"Unknown token type {builtToken}");
            type = optionalType.Value;
        }

        var token = new Token
        {
            Type = type,
            Value = builtToken
        };
        tokens.Add(token);
    }

    private TokenType? ParseUnknownToken(string builtToken)
    {
        // fen string
        if (builtToken.Length >= 14 && builtToken.Contains('/') && builtToken.Contains('k'))
            return TokenType.StringLiteral;

        if ((builtToken.Length == 4 || builtToken.Length == 5)
            && char.IsLetter(builtToken[0])
            && char.IsNumber(builtToken[1])
            && char.IsLetter(builtToken[2])
            && char.IsNumber(builtToken[3])
           )
            return TokenType.MoveString;
        return null;
    }
}