namespace Onyx.UCI;

public class UciParser
{
    private Tokeniser? _tokeniser;
    private int _currentToken;

    private Token Peek()
    {
        if (_tokeniser is null)
            return new Token();
        return _currentToken < _tokeniser.Tokens.Count
            ? _tokeniser.Tokens[_currentToken]
            : new Token { Type = TokenType.Eof, Value = "" };
    }

    private Token Consume()
    {
        if (_tokeniser is null)
            return new Token();
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
                case TokenType.SetOption:
                    return ParseSetOptionCommand();
                case TokenType.Quit:
                    Environment.Exit(0);
                    break;
                case TokenType.Stop:
                    return new StopCommand();
                case TokenType.DebugBoard:
                    return new DebugCommand();
                case TokenType.SetLoggingOn:
                    return new SetLoggingOn();
                case TokenType.Evaluate:
                    return new EvaluateCommand();
                default:
                    throw new ArgumentException(
                        $"Invalid starting token of type {currentToken.Type} with value {currentToken.Value}");
            }
        }

        return null;
    }

    private GoCommand ParseGoCommand()
    {
        var command = new GoCommand();
        var tc = new TimeControl();
        while (Peek().Type != TokenType.Eof)
        {
            var token = Peek();


            switch (token.Type)
            {
                case TokenType.Depth:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        command.Depth = int.Parse(Consume().Value);
                    break;

                case TokenType.Perft:
                    Consume();
                    command.IsPerft = true;
                    if (Peek().Type == TokenType.Divide)
                    {
                        command.IsPerftDivide = true;
                        Consume();
                    }

                    if (Peek().Type == TokenType.IntLiteral)
                        command.Depth = int.Parse(Consume().Value);
                    break;

                case TokenType.Wtime:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        tc.Wtime = int.Parse(Consume().Value);
                    break;

                case TokenType.Btime:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        tc.Btime = int.Parse(Consume().Value);
                    break;

                case TokenType.Winc:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        tc.Winc = int.Parse(Consume().Value);
                    break;

                case TokenType.Binc:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        tc.Binc = int.Parse(Consume().Value);
                    break;

                case TokenType.Movestogo:
                    Consume();
                    if (Peek().Type == TokenType.IntLiteral)
                        tc.movesToGo = int.Parse(Consume().Value);
                    break;
            }
        }

        if (tc.Wtime > 0 || tc.Btime > 0)
            command.TimeControl = tc;
        else command.TimeControl = null;
        return command;
    }

    private PositionCommand? ParsePositionCommand()
    {
        var isStartpos = false;

        var positionType = Consume();

        if (positionType.Type == TokenType.Fen)
            isStartpos = false;

        // if we haven't received position fen, the next word must be startpos
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

    private SetOptionCommand? ParseSetOptionCommand()
    {
        if (Peek().Type != TokenType.Name)
            return null;
        Consume();

        if (Peek().Type != TokenType.StringLiteral)
            return null;
        var name = Consume().Value;

        if (Peek().Type != TokenType.Value)
            return null;
        Consume();

        if (Peek().Type != TokenType.IntLiteral)
            return null;
        var value = Consume().Value;

        return new SetOptionCommand { Name = name, Value = value };
    }
}