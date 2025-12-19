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
        else if (int.TryParse(builtToken,out var result))
            type = (TokenType.IntLiteral);

        else
        {
            throw new ArgumentException();
        }
        var token = new Token
        {
            Type = type,
            value = builtToken
        };
        tokens.Add(token);
        
    }
    }