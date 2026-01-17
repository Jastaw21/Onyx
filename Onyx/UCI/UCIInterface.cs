using Onyx.Core;
using Onyx.Statics;

namespace Onyx.UCI;

public class UciInterface
{
    private readonly Engine _player = new();
    private readonly UciParser _parser = new();
    private readonly Options _options = new();
    private readonly object _lock = new();

    private Thread _searchThread;
    private CancellationTokenSource _searchCts;

    public Engine Player => _player;

    public UciInterface()
    {
        _options.AddOption("threads", "spin", "5", "1", "8", SetThreads);
        _player.OnSearchInfoUpdate += Console.WriteLine;
    }

    public void HandleCommand(string commandString)
    {
        var command = _parser.Parse(commandString);
        if (command == null) return;

        lock (_lock)
        {
            switch (command)
            {
                case UciCommand:
                    Console.WriteLine($"id name Onyx {Engine.Version}");
                    Console.WriteLine("id author JackWeddell");
                    _options.PrintOptions();
                    Console.WriteLine("uciok");
                    break;

                case IsReadyCommand:
                    Console.WriteLine("readyok");
                    break;

                case UciNewGameCommand:
                    StopSearch();
                    _player.Reset();
                    break;

                case PositionCommand pos:
                    StopSearch();
                    if (pos.FenString != null) _player.Position.SetFen(pos.FenString);
                    _player.Position.ApplyMoves(pos.Moves);
                    break;

                case GoCommand go:
                    StartSearch(go);
                    break;

                case StopCommand:
                    StopSearch();
                    break;

                case SetOptionCommand opt:
                    _options.SetOption(opt.Name, int.Parse(opt.Value));
                    break;

                case DebugCommand:
                    Console.WriteLine(_player.Position.GetFen());
                    break;

                case EvaluateCommand:
                    Console.WriteLine($"Score: {Evaluator.Evaluate(_player.Position)}");
                    break;
            }
            Console.Out.Flush();
        }
    }

    private void StartSearch(GoCommand command)
    {
        StopSearch();

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (command.IsPerft)
        {
            HandlePerft(command);
            return;
        }

        _searchThread = new Thread(() =>
        {
            try
            {
                var result = _player.Search(new SearchParameters
                {
                    CancellationToken = token,
                    MaxDepth = command.Depth,
                    TimeControl = command.TimeControl
                });

                
                    Console.WriteLine($"bestmove {result.BestMove}");
                    Console.Out.Flush();
                
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Log(LogType.EngineLog, $"Search error: {ex}");
            }
        }) { IsBackground = true, Name = "SearchThread" };

        _searchThread.Start();
    }

    private void StopSearch()
    {
        _searchCts?.Cancel();
        if (_searchThread != null && _searchThread.IsAlive)
        {
            _searchThread.Join(500); // Short timeout to keep it responsive
        }
        _searchCts?.Dispose();
        _searchCts = null;
        _searchThread = null;
    }

    private void HandlePerft(GoCommand command)
    {
        var depth = command.Depth ?? 1;
        if (command.IsPerftDivide)
        {
            PerftSearcher.PerftDivide(_player.Position, depth);
        }
        else
        {
            for (var i = 1; i <= depth; i++)
            {
                Console.WriteLine($"Depth {i} :  {PerftSearcher.GetPerftResults(_player.Position, i)}");
            }
        }
    }

    private void SetThreads(int threads)
    {
        StopSearch();
        _player.MaxThreads = threads;
    }
}