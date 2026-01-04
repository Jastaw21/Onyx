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
            case EvaluateCommand:
                var turnToMoveSocre = Evaluator.Evaluate(_player.Board);
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
        if (positionCommand.FenString != null) _player.Board.SetFen(positionCommand.FenString);
        _player.Board.ApplyMoves(positionCommand.Moves);
    }

    private void HandleGo(GoCommand command)
    {
        StopSearch();
        _searchCTS = new CancellationTokenSource();
        var depth = command.Depth ?? null; // Default to 5 if not specified
        if (command.IsPerft && depth != null)
        {
            if (command.IsPerftDivide)
            {
                _player.PerftDivide(depth.Value);
            }
            else
            {
                for (var i = 1; i <= depth; i++)
                {
                    
                    var perftResult = _player.Perft(i);
              
                    var result = $"Depth {i} :  {perftResult}";
                    //Logger.Log(LogType.UCISent, result);
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
                    var infoString = PrintSearchInfoString(move);
                    
                    Console.WriteLine(infoString);
                    Console.WriteLine($"bestmove {move.BestMove}");
                    
                    
                    //Logger.Log(LogType.UCISent, result);
                    //Logger.Log(LogType.UCISent, infoString);
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

    private string MovesToString(List<Move>? moves)
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

    private string PrintSearchInfoString(SearchResults results)
    {
        var stats = results.Statistics;
        var pv = results.PV;
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;
        return $"info depth {stats.Depth} multipv 1 score cp {results.Score} nodes {stats.Nodes} nps {nps} time {stats.RunTime} pv {MovesToString(pv)} ";
    }

    private readonly UciParser _parser = new();
}