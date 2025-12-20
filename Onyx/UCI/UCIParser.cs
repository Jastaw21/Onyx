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

    public string? FenString => IsStartpos ? Fen.DefaultFen : Fenstring;

    public string? Fenstring { get; set; }
    public List<string>? Moves;

    public PositionCommand(bool isStartpos, string? fen = null, List<string>? moves = null)
    {
        IsStartpos = isStartpos;
        Fenstring = fen;
        Moves = moves;
    }
}

public class UCIParser(string uciString)
{
    private Tokeniser _tokeniser = new(uciString);
    private int currentToken;

    private Token Peek()
    {
        return currentToken < _tokeniser.Tokens.Count
            ? _tokeniser.Tokens[currentToken]
            : new Token { Type = TokenType.EOF, Value = "" };
    }

    private Token Consume()
    {
        return _tokeniser.Tokens[currentToken++];
    }


    public Command? Parse()
    {
        while (currentToken < _tokeniser.Tokens.Count)
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

        // if we haven't recieved position fen, the next word must be startpos
        if (positionType.Type != TokenType.Startpos && positionType.Type != TokenType.Fen)
            return null;

        if (positionType.Type == TokenType.Startpos)
        {
            isStartpos = true;
        }

        var positionCommand = new PositionCommand(isStartpos);

        if (positionType.Type == TokenType.Fen)
        {
            var fenPositionString = Consume().Value;
            if (Peek().Type == TokenType.Colour)
                fenPositionString += " " + Consume().Value;
            else return null;

            if (Peek().Type is TokenType.CastlingString or TokenType.Dash)
                fenPositionString += " " + Consume().Value;
            else return null;

            if (Peek().Type is TokenType.EnPassantString or TokenType.Dash)
                fenPositionString += " " + Consume().Value;
            else return null;

            // two ints for the turn/move clocks
            if (Peek().Type is TokenType.IntLiteral)
                fenPositionString += " " + Consume().Value;
            else return null;
            if (Peek().Type is TokenType.IntLiteral)
                fenPositionString += " " + Consume().Value;
            else return null;

            positionCommand.Fenstring = fenPositionString;
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

        positionCommand.Moves = moves;

        return positionCommand;
    }
}