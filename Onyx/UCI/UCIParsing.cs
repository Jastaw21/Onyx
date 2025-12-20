using Onyx.Core;

namespace Onyx.UCI;

public abstract record Command;

public record UCICommand : Command;

public record GoCommand : Command
{
    public bool isPerft;
    public int depth;
    public int wtime;
    public int btime;
}

public record PositionCommand : Command
{
    public bool IsStartpos { get; }

    public string? FenString => IsStartpos ? Fen.DefaultFen : _fenstring;

    private readonly string? _fenstring;
    public List<string>? Moves;

    public PositionCommand(bool isStartpos, string? fen = null, List<string>? moves = null)
    {
        IsStartpos = isStartpos;
        _fenstring = fen;
        Moves = moves;
    }
}

public class UCIParser(string uciString)
{
    private Tokeniser _tokeniser = new(uciString);
    private int currentToken;

    private Token Peek()
    {
        return currentToken < _tokeniser.tokens.Count
            ? _tokeniser.tokens[currentToken]
            : new Token { Type = TokenType.EOF, Value = "" };
    }

    private Token Consume()
    {
        return _tokeniser.tokens[currentToken++];
    }


    public Command? Parse()
    {
        while (currentToken < _tokeniser.tokens.Count)
        {
            var currentToken = Consume();

            if (currentToken.Type == TokenType.UCI)
                return new UCICommand();
            if (currentToken.Type == TokenType.Position)
                return ParsePositionCommand();
            if (currentToken.Type == TokenType.GO)
                return ParseGoCommand();
        }

        return null;
    }

    private Command? ParseGoCommand()
    {
        var command = new GoCommand();
        var goType = Peek();
        if (goType.Type == TokenType.Depth)
        {
            Consume(); // pop off the go token
            command.isPerft = false;
            if (Peek().Type == TokenType.IntLiteral)
            {
                var depthValue = int.Parse(Consume().Value);
                command.depth = depthValue;
            }
            else
            {
                command.depth = 5; // default?
            }

            return command;
        }
        else if (goType.Type == TokenType.Perft)
        {
            Consume();
            command.isPerft = true;
            if (Peek().Type == TokenType.IntLiteral)
            {
                var value = int.Parse(Consume().Value);
                command.depth = value;
            }
            else
            {
                command.depth = 5;
            }


            return command;
        }

        return null;
    }

    private Command? ParsePositionCommand()
    {
        var isStartpos = false;

        var positionType = Consume();

        if (positionType.Type == TokenType.Fen)
            isStartpos = false;
        // not a valid position string
        if (positionType.Type != TokenType.Startpos)
            return null;

        else if (positionType.Type == TokenType.Startpos)
        {
            isStartpos = true;
        }

        var fenString = "";
        if (positionType.Type == TokenType.Fen)
        {
            fenString = Consume().Value;
        }

        List<string>? moves = null;
        if (Peek().Type == TokenType.Moves)
        {
            Consume();
            moves = [];
            while (Peek().Type != TokenType.EOF)
            {
                var token = Consume();
                if (token.Type == TokenType.MoveString)
                    moves.Add(token.Value);
            }
        }

        var positionCommand = new PositionCommand(isStartpos);
        positionCommand.Moves = moves;

        return positionCommand;
    }
}