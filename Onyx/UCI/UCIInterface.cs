using System.Text;
using Onyx.Core;
using Onyx.Statics;

namespace Onyx.UCI;

public class UciInterface
{
    private readonly Engine _player = new();
    private Thread _engineThread;
    private CancellationTokenSource _searchCTS;
    private Options _options = new Options();
    private readonly UciParser _parser = new();
    private readonly object _lock = new();
    public Engine Player => _player;

    public UciInterface()
    {
        _options.AddOption("threads", "spin", "5", "1", "8", SetThreads);
        _player.OnSearchInfoUpdate += (info) =>
        {
            Console.WriteLine(info);
            Console.Out.Flush();
        };
    }

    public void HandleCommand(string commandString)
    {
        //Logger.Log(LogType.UCIReceived, commandString);

        Command command;
        try
        {
            command = _parser.Parse(commandString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing command {commandString}: {ex.Message}");
            return;
        }

        if (command is null)
        {
            Console.WriteLine($"Unknown command {commandString}");
            return;
        }

        lock (_lock)
        {
            try
            {
                DispatchCommand(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error dispatching command {commandString}: {ex.Message}");
            }
        }
    }

    private void DispatchCommand(Command command)
    {
        switch (command)
        {
            case UciCommand:
                List<string> lines = [$"id name Onyx {Engine.Version}", "id author JackWeddell", "uciok"];
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine();
                _options.PrintOptions();

                break;
            case GoCommand goCommand:
                DispatchGoCommand(goCommand);
                break;
            case PositionCommand positionCommand:
                HandlePosition(positionCommand);
                break;
            case UciNewGameCommand:
                StopSearch();
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
            case SetOptionCommand optionCommand:
                HandleOptionCommand(optionCommand);
                break;
        }

        Console.Out.Flush();
    }

    private void HandleOptionCommand(SetOptionCommand command)
    {
        _options.SetOption(command.Name, int.Parse(command.Value));
    }

    private void HandlePosition(PositionCommand positionCommand)
    {
        StopSearch();
        if (positionCommand.FenString != null) _player.Position.SetFen(positionCommand.FenString);
        _player.Position.ApplyMoves(positionCommand.Moves);
    }

    private void DispatchGoCommand(GoCommand command)
    {
        StopSearch();
        lock (_lock)
        {
            if (_engineThread != null)
            {
                 Console.WriteLine("info string Engine is already searching. Ignoring go command.");
                 return;
            }
            
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
    }

    private void HandleGo(GoCommand command, int? depth)
    {
        var token = _searchCTS.Token;
        _engineThread = new Thread(() =>
        {
            try
            {
                var searchResults = _player.Search(new SearchParameters
                {
                    CancellationToken = token,
                    MaxDepth = depth,
                    TimeControl = command.TimeControl
                });

                if (!token.IsCancellationRequested)
                {
                    Console.WriteLine($"bestmove {searchResults.BestMove}");
                    Console.Out.Flush();
                }
            }

            catch (OperationCanceledException)
            {
                // Normal cancellation, no action needed
            }

            catch (Exception ex)
            {
                try
                {
                    Logger.Log(LogType.EngineLog, $"Unhandled exception in search thread: {ex}");
                }
                catch
                {
                    // Ignore logger errors if process is crashing
                }
            }
        })
        {
            IsBackground = true,
            Name = "SearchThread"
        };
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
                var perftResult = PerftSearcher.GetPerftResults(_player.Position, i);
                Console.WriteLine($"Depth {i} :  {perftResult}");
            }
        }
    }

    private void StopSearch()
    {
        Thread threadToJoin = null;
        lock (_lock)
        {
            if (_engineThread == null) return;

            _searchCTS?.Cancel();
            threadToJoin = _engineThread;
            _engineThread = null;
        }

        // Join outside the lock to prevent deadlocks
        if (threadToJoin != null)
        {
            try
            {
                if (!threadToJoin.Join(2000))
                {
                    Logger.Log(LogType.EngineLog, "Warning: Search thread did not terminate in time.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.EngineLog, $"Error joining search thread: {ex}");
            }
        }

        lock (_lock)
        {
            if (_searchCTS != null)
            {
                try
                {
                    _searchCTS.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _searchCTS = null;
            }
        }
    }

    private void SetThreads(int threads)
    {
        StopSearch();
        _player.MaxThreads = threads;
    }
}