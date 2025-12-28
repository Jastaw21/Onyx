using Onyx.Core;
using Onyx.Statics;

namespace Onyx.UCI;

public class UciInterface
{
    private readonly Engine _player = new Engine();

    public Engine Player => _player;

    public void HandleCommand(string commandString)
    {
        Logger.Log(LogType.UCIReceived, commandString);

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
                List<string> lines = [$"id name Onyx {_player.Version}", "id author JackWeddell", "uciok"];
                foreach (var line in lines)
                {
                    Logger.Log(LogType.UCISent, line);
                    Console.WriteLine(line);
                }

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
        if (positionCommand.FenString != null) _player.Board = new Board(positionCommand.FenString);
        _player.Board.ApplyMoves(positionCommand.Moves);
    }

    private void HandleGo(GoCommand command)
    {
        var depth = command.Depth ?? 10; // Default to 5 if not specified
        if (command.IsPerft)
        {
            for (var i = 1; i <= depth; i++)
            {
                var perftResult = _player.Perft(i);
                var result = $"Depth {i} :  {perftResult}";
                Logger.Log(LogType.UCISent, result);
                Console.WriteLine($"Depth {i} :  {perftResult}");
            }
        }
        else
        {
            var move = _player.CalcAndDispatchTimedSearch(depth, command.TimeControl);
            var result = $"bestmove {move.bestMove}";
            Logger.Log(LogType.UCISent, result);
            var infoString = GetInf(move.stats);
            Logger.Log(LogType.UCISent, infoString);
            Console.WriteLine($"bestmove {move.bestMove}");
            Console.WriteLine(infoString);
        }
    }

    private string GetInf(SearchStatistics stats)
    {
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;
        return $"info depth {stats.Depth} nodes {stats.Nodes} time {stats.RunTime} nps {nps}";
    }

    private readonly UciParser _parser = new();
}