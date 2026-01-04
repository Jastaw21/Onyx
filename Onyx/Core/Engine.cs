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
        var baseTime = time / movesToGo + safeInc * 0.8;

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
    public static string Version => "0.8.4";
    // data members
    public Position Position = new();
    public TranspositionTable TranspositionTable { get; } = new();
    private StopwatchManager StopwatchManager { get; set; } = new();
    public int MateScore { get; } = 30000;
    private CancellationToken _ct; // for threading

    // search members
    public int CurrentSearchId { get; private set; }
    private bool _loggingEnabled;
    private readonly TimeManager _timeManager;
    private Searcher _searcher;
    private Thread _searchThread;

    public Engine()
    {
        _timeManager = new TimeManager(this);
        _searcher = new Searcher(this);
    }

    // UCI Interface methods
    public void SetLogging(bool enabled)
    {
        _loggingEnabled = enabled;
        Evaluator.LoggingEnabled = enabled;
    }

    public void SetPosition(string fen)
    {
        Position.SetFen(fen);
    }

    public void Reset()
    {
        Position = new Position();
        StopwatchManager = new StopwatchManager();
        _searcher.Reset();
    }

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
        _searchThread = new Thread((() =>
                _searcher.IterativeDeepeningSearch(
                    new SearcherInstructions
                        { IsTimed = isTimed, MaxDepth = depthLimit, StartDepth = 1, DepthInterval = 1 }
                    , Position))
        );
        
        _searchThread.Name = $"Search {CurrentSearchId}";
        _searcher.IsFinished = false;
        _searchThread.Start();
        StopwatchManager.Start(timeLimit);
        

        while (!_searcher.IsFinished)
        {
            if (_ct.IsCancellationRequested)
                _searcher.stopFlag = true;
            if (isTimed && StopwatchManager.ShouldStop)
                _searcher.stopFlag = true;
        }

        return _searcher.SearchResults;
    }
}