using Onyx.Core;
using Onyx.Statics;

namespace Onyx.UCI;

public class UciInterface
{
    private readonly Engine _player = new();
    private Thread _engineThread;
    private CancellationTokenSource _searchCTS;


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
            case DebugCommand:
                Console.WriteLine(_player.Board.GetFen());
                break;
            case StopCommand:
                StopSearch();
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
        StopSearch();
        _searchCTS = new CancellationTokenSource();
        var depth = command.Depth ?? 5; // Default to 5 if not specified
        if (command.IsPerft)
        {
            if (command.IsPerftDivide)
            {
                _player.PerftDivide(depth);
            }
            else
            {
                for (var i = 1; i <= depth; i++)
                {
                    var perftResult = _player.Perft(i);
                    var result = $"Depth {i} :  {perftResult}";
                    Logger.Log(LogType.UCISent, result);
                    Console.WriteLine($"Depth {i} :  {perftResult}");
                }
            }
        }
        else
        {
            _engineThread = new Thread(() =>
            {
                try
                {
                    var move = _player.Search(new SearchParameters
                    {
                        CancellationToken =  _searchCTS.Token, 
                        MaxDepth = depth,
                        TimeControl = command.TimeControl
                    });
                    
                    var result = $"bestmove {move.BestMove}";
                    var infoString = GetInf(move.Statistics);
                    
                    Console.WriteLine($"bestmove {move.BestMove}");
                    Console.WriteLine(infoString);
                    
                    Logger.Log(LogType.UCISent, result);
                    Logger.Log(LogType.UCISent, infoString);
                }

                catch (OperationCanceledException)
                {
                    Console.WriteLine("search cancelled");
                }
            });
            
            _engineThread.Start();
        }
    }

    private void StopSearch()
    {
        _searchCTS?.Cancel();
        _engineThread?.Join(1000);
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