using Onyx.UCI;

namespace Onyx.Core;

public class UCIInterface
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
            case UCICommand:
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
        _player.board = new Board(positionCommand.FenString);
    }

    private void HandleGo(GoCommand command)
    {
        if (command.isPerft)
        {
            var depth = command.depth;
            for (int i = 1; i <= depth; i++)
            {
                var perftResult = _player.Perft(i);
                Console.WriteLine($"Depth {i} :  {perftResult}");
            }
        }
        else
        {
            var move = _player.Search(command.depth);
            Console.WriteLine($"bestmove {move.bestMove}");
        }
    }

    private UciParser _parser = new UciParser();
}