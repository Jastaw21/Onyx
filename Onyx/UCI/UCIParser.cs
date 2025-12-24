namespace Onyx.UCI;

public class UciParser
{
    private Tokeniser? _tokeniser;
    private int _currentToken;

    private Token Peek()
    {
        return _currentToken < _tokeniser.Tokens.Count
            ? _tokeniser.Tokens[_currentToken]
            : new Token { Type = TokenType.Eof, Value = "" };
    }

    private Token Consume()
    {
        return _tokeniser.Tokens[_currentToken++];
    }

    public Command? Parse(string uciString)
    {
        _currentToken = 0;
        _tokeniser = new Tokeniser(uciString);
        while (_currentToken < _tokeniser.Tokens.Count)
        {
            var currentToken = Consume();

            switch (currentToken.Type)
            {
                case TokenType.Uci:
                    return new UciCommand();
                case TokenType.UciNewGame:
                    return new UciNewGameCommand();
                case TokenType.IsReady:
                    return new IsReadyCommand();
                case TokenType.Position:
                    return ParsePositionCommand();
                case TokenType.Go:
                    return ParseGoCommand();
                case TokenType.Quit:
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException(
                        $"Invalid starting token of type {currentToken.Type} with value {currentToken.Value}");
            }
        }

        return null;
    }

    private Command? ParseGoCommand()
    {
        var command = new GoCommand();
        var goType = Peek();
        switch (goType.Type)
        {
            case TokenType.Depth:
                HandleGoDepth(command);
                break;
            case TokenType.Perft:
                HandlePerft(command);
                break;
        }

        // here, we should be after anything like go depth x or go perft y
        while (Peek().Type is TokenType.Btime or TokenType.Wtime or TokenType.Winc or TokenType.Binc)
        {
            var anchorToken = Consume();
            switch (anchorToken.Type)
            {
                case TokenType.Btime:
                    if (Peek().Type == TokenType.IntLiteral) command.TimeControl.Btime = int.Parse(Consume().Value);
                    break;
                case TokenType.Wtime:
                    if (Peek().Type == TokenType.IntLiteral) command.TimeControl.Wtime = int.Parse(Consume().Value);
                    break;
                case TokenType.Binc:
                    if (Peek().Type == TokenType.IntLiteral) command.TimeControl.Binc = int.Parse(Consume().Value);
                    break;
                case TokenType.Winc:
                    if (Peek().Type == TokenType.IntLiteral) command.TimeControl.Winc = int.Parse(Consume().Value);
                    break;
                case TokenType.Movestogo:
                    if (Peek().Type == TokenType.IntLiteral) command.TimeControl.movesToGo = int.Parse(Consume().Value);
                    break;
            }
        }

        return command;
    }

    private void HandleGoDepth(GoCommand command)
    {
        Consume(); // pop off the go token
        command.IsPerft = false;
        if (Peek().Type == TokenType.IntLiteral)
        {
            var depthValue = int.Parse(Consume().Value);
            command.Depth = depthValue;
        }
        else
        {
            command.Depth = 5; // default?
        }
    }

    private void HandlePerft(GoCommand command)
    {
        Consume();
        command.IsPerft = true;
        if (Peek().Type == TokenType.IntLiteral)
        {
            var value = int.Parse(Consume().Value);
            command.Depth = value;
        }
        else
        {
            command.Depth = 5;
        }
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
            while (Peek().Type != TokenType.Eof)
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