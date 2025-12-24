using Onyx.Core;

namespace Onyx.UCI;

public class UciInterface
{

    private Engine _player = new Engine();

    public void HandleCommand(string commandString)
    {
        var command = _parser.Parse(commandString);
        if (command is null)
        {
            Console.WriteLine($"Unknown command {commandString}");
            return;
        }
        else
        {
            DispatchCommand(command);
        }
    }

    private void DispatchCommand(Command command)
    {
        switch (command)
        {
            case UciCommand:
                Console.WriteLine("uciready");
                break;
            case GoCommand goCommand:
                HandleGo(goCommand);
                break;
            case PositionCommand positionCommand:
                HandlePosition(positionCommand);
                break;
        }
    }

    private void HandlePosition(PositionCommand positionCommand)
    {
        _player.Board = new Board(positionCommand.FenString);
    }

    private void HandleGo(GoCommand command)
    {
        if (command.IsPerft)
        {
            var depth = command.Depth;
            for (var i = 1; i <= depth; i++)
            {
                var perftResult = _player.Perft(i);
                Console.WriteLine($"Depth {i} :  {perftResult}");
            }
        }
        else
        {
            var move = _player.Search(command.Depth);
            Console.WriteLine($"bestmove {move.bestMove}");
        }
    }

    private UciParser _parser = new UciParser();
}