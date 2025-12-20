using Onyx.UCI;

namespace Onyx.Core;

public class Engine
{
    public Engine()
    {
        board = new Board();
    }

    private Board board;

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
        board = new Board(positionCommand.FenString);
    }

    private void HandleGo(GoCommand command)
    {
        if (command.isPerft)
        {
            var depth = command.depth;
            for (int i = 1; i <= depth; i++)
            {
                var perftResult = PerftSearcher.GetPerftResults(board, i);
                Console.WriteLine($"Depth : {i} {perftResult}");
            }
        }
    }

    private UciParser _parser = new UciParser();
}