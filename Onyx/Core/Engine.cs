using Onyx.Statics;
using Onyx.UCI;


namespace Onyx.Core;

public class TimeManager(Engine engine)
{
    public int TimeBudgetPerMove(TimeControl timeControl)
    {
        var time = engine.Position.WhiteToMove ? timeControl.Wtime : timeControl.Btime;
        var increment = engine.Position.WhiteToMove ? timeControl.Winc : timeControl.Binc;

        var safeInc = increment ?? 0;

        var calcMovesRemaining = MovesRemaining(engine.Position);
        var instructedMovesRemaining = timeControl.movesToGo ?? 0;

        // if moves remaining feels nonsense, use our own calc
        var movesToGo =
            Math.Abs(instructedMovesRemaining - calcMovesRemaining) > 5
                ? calcMovesRemaining
                : instructedMovesRemaining;
        var baseTime = time / 20 + safeInc * 0.5;

        // use max of 20% remaining time
        var safeMax = time * 0.2;
        var finalBudget = (int)Math.Min(baseTime!.Value, safeMax!.Value);

        var timeBudgetPerMove = Math.Max(finalBudget, 50);
        return timeBudgetPerMove;
    }

    private static int MovesRemaining(Position position)
    {
        var ply = position.FullMoves * 2;

        if (ply < 20) return 40; // opening
        if (ply < 60) return 30; // middlegame
        return 20; // endgame
    }
}

public class Engine
{
    public static string Version => "0.10.0";
    // data members
    public Position Position = new();
    public TranspositionTable TranspositionTable { get; } = new();
    public StopwatchManager StopwatchManager { get; set; } = new();
    public int MateScore { get; private set; } = 30000;
    private CancellationToken _ct; // for threading

    // search members
    public int CurrentSearchId { get; private set; }
    private readonly TimeManager _timeManager;
    private readonly List<Searcher> _workers = [];
    private int _maxThreads = 5;
    public SearchStatistics _statistics;

    public Engine()
    {
        IsReady = false;
        _timeManager = new TimeManager(this);
        InitializeWorkerThreads();
        IsReady = true;
    }

    private void InitializeWorkerThreads()
    {
        for (var workerID = 0; workerID < 2; workerID++)
        {
            var worker = new Searcher(this, workerID);
            _workers.Add(worker);
            
            var thread = new Thread(worker.Start) 
            { 
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = $"Worker {workerID}"
            };
            thread.Start();
        }
    }

    // UCI Interface methods
    public void SetLogging(bool enabled)
    {
        Evaluator.LoggingEnabled = enabled;
    }

    public void SetPosition(string fen)
    {
        Position.SetFen(fen);
    }

    public void Reset()
    {
        IsReady = false;
        Position = new Position();
        StopwatchManager = new StopwatchManager();
        _workers.Clear();
        InitializeWorkerThreads();
        IsReady = true;
    }

    public bool IsReady { get; set; }

    public SearchResults Search(SearchParameters searchParameters)
    {
        CurrentSearchId++;
        _ct = searchParameters.CancellationToken;

        var timeLimit = long.MaxValue;
        var isTimed = false;
        if (searchParameters.TimeLimit.HasValue)
        {
            timeLimit = searchParameters.TimeLimit.Value;
            isTimed = true;
        }

        else if (searchParameters.TimeControl.HasValue)
        {
            timeLimit = _timeManager.TimeBudgetPerMove(searchParameters.TimeControl.Value);
            isTimed = true;
        }



        var depthLimit = searchParameters.MaxDepth ?? 100;
        var searchInstructions = new SearcherInstructions
        {
            IsTimed = isTimed,
            MaxDepth = depthLimit,
            StartDepth = 1,
            DepthInterval = 1
        };

        StopwatchManager.Start(timeLimit);
        foreach (var worker in _workers)
        {
            worker.TriggerSearch(searchInstructions, Position.Clone());
        }
        
        while (!_ct.IsCancellationRequested)
        {
            if (isTimed && StopwatchManager.ShouldStop) break;
            if (_workers[0].IsFinished) break;
            Thread.Sleep(1);
        }
        
        foreach (var worker in _workers) worker.stopFlag = true;
        var result = _workers[0]._searchResults;
        _statistics = _workers[0]._statistics;
        StopwatchManager.Reset();
        return result;
    }
}