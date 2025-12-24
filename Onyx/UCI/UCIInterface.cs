using Onyx.Core;

namespace Onyx.UCI;

public class UciInterface
{
    private Engine _player = new Engine();

    public void HandleCommand(string commandString)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {commandString}";
        File.AppendAllText("uci_commands.log", logEntry + Environment.NewLine);

        var command = _parser.Parse(commandString);
        if (command is null)
        {
            Console.WriteLine($"Unknown command {commandString}");
            return;
        }

        DispatchCommand(command);
    }

    private void DispatchCommand(Command command)
    {
        switch (command)
        {
            case UciCommand:
                Console.WriteLine("id name Onyx");
                Console.WriteLine("id author JackWeddell");
                Console.WriteLine("uciok");
                break;
            case GoCommand goCommand:
                HandleGo(goCommand);

                break;
            case PositionCommand positionCommand:
                HandlePosition(positionCommand);
                break;
            case UciNewGameCommand:
                _player.Reset();
                break;
            case IsReadyCommand:
                Console.WriteLine("readyok");
                break;
        }

        Console.Out.Flush();
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
            var move = _player.RequestSearch(command.Depth, command.TimeControl);
            Console.WriteLine($"bestmove {move.bestMove}");
        }
    }

    private UciParser _parser = new UciParser();
}