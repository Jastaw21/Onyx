using System.Text;
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
        //Logger.Log(LogType.UCIReceived, commandString);

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
                List<string> lines = [$"id name Onyx {Engine.Version}", "id author JackWeddell", "uciok"];
                foreach (var line in lines)
                {
                    //Logger.Log(LogType.UCISent, line);
                    Console.WriteLine(line);
                }

                break;
            case GoCommand goCommand:
                DispatchGoCommand(goCommand);
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
                Console.WriteLine(_player.Position.GetFen());
                break;
            case StopCommand:
                StopSearch();
                break;
            case EvaluateCommand:
                var turnToMoveSocre = Evaluator.Evaluate(_player.Position);
                Console.WriteLine($"Score: {turnToMoveSocre}");
                break;
            case SetLoggingOn:
                _player.SetLogging(true);
                break;
        }

        Console.Out.Flush();
    }

    private void HandlePosition(PositionCommand positionCommand)
    {
        if (positionCommand.FenString != null) _player.Position.SetFen(positionCommand.FenString);
        _player.Position.ApplyMoves(positionCommand.Moves);
    }

    private void DispatchGoCommand(GoCommand command)
    {
        StopSearch();
        _searchCTS = new CancellationTokenSource();
        var depth = command.Depth ?? null;
        if (command.IsPerft && depth != null)
        {
            HandlePerft(command, depth);
        }
        else
        {
            HandleGo(command, depth);
        }
    }

    private void HandleGo(GoCommand command, int? depth)
    {
        _engineThread = new Thread(() =>
        {   
            try
            {
                var searchResults = _player.Search(new SearchParameters
                {
                    CancellationToken = _searchCTS.Token,
                    MaxDepth = depth,
                    TimeControl = command.TimeControl
                });
                var infoString = GetSearchInfoString(searchResults,_player._statistics);

                Console.WriteLine(infoString);
                Console.WriteLine($"bestmove {searchResults.BestMove}");
            }

            catch (OperationCanceledException oce)
            {
                Logger.Log(LogType.EngineLog, $"{oce.Message}");
                Console.WriteLine("search cancelled");
            }

            catch (Exception ex)
            {
                Logger.Log(LogType.EngineLog, $"{ex.Message}");
                Console.WriteLine("search cancelled");
            }
        });
        _engineThread.Start();
    }

    private void HandlePerft(GoCommand command, int? depth)
    {
        if (command.IsPerftDivide)
        {
            PerftSearcher.PerftDivide(_player.Position, depth.Value);           
        }
        else
        {
            for (var i = 1; i <= depth; i++)
            {

                var perftResult = PerftSearcher.GetPerftResults(_player.Position,i);

                var result = $"Depth {i} :  {perftResult}";
                //Logger.Log(LogType.UCISent, result);
                Console.WriteLine($"Depth {i} :  {perftResult}");
            }
        }
    }

    private void StopSearch()
    {
        if (_engineThread == null) return;
        
        _searchCTS?.Cancel();
        _engineThread.Join();
        
        _searchCTS?.Dispose();
        _searchCTS = null;
        _engineThread = null;
    }

    private static string MovesToString(List<Move>? moves)
    {
        var sb = new StringBuilder();
        if (moves == null || moves.Count == 0) return sb.ToString();
        foreach (var move in moves)
        {
            sb.Append(move.Notation);
            sb.Append(' ');
        }
        sb.Remove(sb.Length - 1, 1); // remove last space
        return sb.ToString();
    }

    private static string GetSearchInfoString(SearchResults results, SearchStatistics stats)
    {
        var pv = results.PV;
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;
        return $"info depth {stats.Depth} multipv 1 score cp {results.Score} nodes {stats.Nodes} nps {nps} time {stats.RunTime} pv {MovesToString(pv)} ";
    }

    private readonly UciParser _parser = new();
}