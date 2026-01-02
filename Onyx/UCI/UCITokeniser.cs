namespace Onyx.UCI;

public class Tokeniser
{
    private readonly int _currentIndex;
    public readonly List<Token> Tokens;


    public Tokeniser(string stringIn)
    {
        var builtToken = "";
        Tokens = [];
        while (_currentIndex < stringIn.Length)
        {
            if (stringIn[_currentIndex] == ' ')
            {
                HandleToken(builtToken);
                builtToken = "";
            }
            else
            {
                builtToken += stringIn[_currentIndex];
            }

            _currentIndex++;
        }

        if (!(builtToken.Length == 0))
            HandleToken(builtToken);
    }

    private void HandleToken(string builtToken)
    {
        TokenType type;
        if (builtToken == "uci")
            type = TokenType.Uci;
        else if (builtToken == "go")
            type = TokenType.Go;
        else if (builtToken == "depth")
            type = TokenType.Depth;
        else if (builtToken == "perft")
            type = TokenType.Perft;
        else if (builtToken == "fen")
            type = TokenType.Fen;
        else if (builtToken == "startpos")
            type = TokenType.Startpos;
        else if (builtToken == "moves")
            type = TokenType.Moves;
        else if (builtToken == "position")
            type = TokenType.Position;
        else if (builtToken == "-")
            type = TokenType.Dash;
        else if (builtToken == "btime")
            type = TokenType.Btime;
        else if (builtToken == "wtime")
            type = TokenType.Wtime;
        else if (builtToken == "winc")
            type = TokenType.Winc;
        else if (builtToken == "binc")
            type = TokenType.Binc;
        else if (builtToken == "ucinewgame")
            type = TokenType.UciNewGame;
        else if (builtToken == "isready")
            type = TokenType.IsReady;
        else if (builtToken == "movestogo")
            type = TokenType.Movestogo;
        else if (builtToken == "quit")
            type = TokenType.Quit;
        else if (builtToken == "divide")
            type = TokenType.Divide;
        else if (builtToken == "d")
            type = TokenType.DebugBoard;
        else if (builtToken == "stop")
            type = TokenType.Stop;
        else if (builtToken == "-logging")
            type = TokenType.SetLoggingOn;
        
        else if (int.TryParse(builtToken, out _))
            type = TokenType.IntLiteral;

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
        Tokens.Add(token);
    }

    private TokenType? ParseUnknownToken(string builtToken)
    {
        // fen string
        if (builtToken.Length >= 14 && builtToken.Contains('/') && builtToken.Contains('k'))
            return TokenType.FenString;

        // move string
        if (builtToken.Length is 4 or 5
            && char.IsLetter(builtToken[0])
            && char.IsNumber(builtToken[1])
            && char.IsLetter(builtToken[2])
            && char.IsNumber(builtToken[3])
           )
            return TokenType.MoveString;

        if (builtToken is "w" or "b")
            return TokenType.Colour;

        // could be a castling token
        if (builtToken.Length >= 1 && builtToken.Length <= 4)
        {
            var charsInString = new Dictionary<char, int>();
            foreach (var character in builtToken)
            {
                if (charsInString.ContainsKey(character))
                    charsInString[character]++;
                else
                {
                    charsInString[character] = 1;
                }
            }

            var validCastlingString = false;
            foreach (var pair in charsInString)
            {
                if (pair.Value is not ('k' or 'q' or 'K' or 'Q'))
                    validCastlingString = true;
                else
                {
                    validCastlingString = false;
                    break;
                }
            }

            if (validCastlingString)
                return TokenType.CastlingString;
        }

        if (builtToken.Length is 2 && char.IsLetter(builtToken[0]) && char.IsNumber(builtToken[1]))
            return TokenType.EnPassantString;

        return null;
    }
}