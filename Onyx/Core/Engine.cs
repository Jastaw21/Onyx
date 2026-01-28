using System.Text;
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
        var instructedMovesRemaining = timeControl.MovesToGo ?? 0;

        // if moves remaining feels nonsense, use our own calc
        var movesToGo =
            Math.Abs(instructedMovesRemaining - calcMovesRemaining) > 5
                ? calcMovesRemaining
                : instructedMovesRemaining;

        if (movesToGo <= 0) movesToGo = calcMovesRemaining;

        var baseTime = time / movesToGo + safeInc * 0.5;

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
    public static string Version => "0.10.6";
    // data members
    public Position Position = new();
    public TranspositionTable TranspositionTable { get; } = new();
    public EvaluationTable EvaluationTable { get; } = new();
    public StopwatchManager StopwatchManager { get; private set; } = new();
    public static int MateScore => 30000;
    private CancellationToken _ct; // for threading
    public void SetLmrThreshold(int lmr) => _workers[0].LmrThreshold = lmr;
    public event Action<string> OnSearchInfoUpdate = null!;

    // search members
    public int CurrentSearchId { get; private set; }
    private readonly TimeManager _timeManager;
    private readonly List<Searcher> _workers = [];
    public Searcher PrimaryWorker => _workers[0];
    private readonly int _maxThreads = 1;
    public SearchStatistics Statistics;

    public Engine()
    {
        _timeManager = new TimeManager(this);
        InitializeWorkerThreads();
        _workers[0].OnDepthFinished +=
            (results, stats) => OnSearchInfoUpdate(GetSearchInfoString(results, stats));
    }

    private void InitializeWorkerThreads()
    {
        for (var workerId = 0; workerId < _maxThreads; workerId++)
        {
            var worker = new Searcher(this, workerId);
            _workers.Add(worker);

            var thread = new Thread(worker.Start)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = $"Worker {workerId}"
            };
            thread.Start();
        }
    }


    public void SetPosition(string fen)
    {
        Position.SetFen(fen);
    }

    public void Reset()
    {
        Position = new Position();
        StopwatchManager = new StopwatchManager();
        foreach (var worker in _workers)
        {
            worker.ResetState();
        }
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
        var searchInstructions = new SearcherInstructions
        {
            MaxDepth = depthLimit,
            StartDepth = 1,
            DepthInterval = 1
        };

        // take off some buffer time
        StopwatchManager.Start(timeLimit - 30);
        foreach (var worker in _workers)
        {
            //searchInstructions.StartDepth = depthCount;
            worker.TriggerSearch(searchInstructions, Position);
        }

        try
        {
            while (!_ct.IsCancellationRequested)
            {
                if (isTimed && StopwatchManager.ShouldStop) break;
                if (_workers[0].IsFinished) break;

                Thread.Sleep(1);
            }
        }
        finally
        {
            foreach (var worker in _workers) worker.StopFlag = true;

            // Need to wait for the main worker to come back to root
            while (!_workers[0].IsFinished)
            {
                Thread.Sleep(1);
            }
        }

        var result = _workers[0].SearchResults;
        Statistics = _workers[0].Statistics;
        Statistics.RunTime = StopwatchManager.Elapsed;
        StopwatchManager.Reset();

        if (Logger.LoggingEnabled)
        {
            Statistics.WriteStats();
            TranspositionTable.WriteStats();
            EvaluationTable.WriteStats();
        }

        return result;
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

        sb.Remove(sb.Length - 1, 1); // remove the last space
        return sb.ToString();
    }

    private string GetSearchInfoString(SearchResults results, SearchStatistics stats)
    {
        var pv = results.Pv;
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;

        string scoreString;

        // is a mating score
        if (Math.Abs(results.Score) > 29000)
        {
            var plyToMate = MateScore - Math.Abs(results.Score);
            var mateString = plyToMate * Math.Sign(results.Score);
            scoreString = $"score mate {mateString}";
        }
        else
        {
            scoreString = $"score cp {results.Score}";
        }


        return
            $"info depth {stats.Depth} multipv 1 {scoreString} nodes {stats.Nodes} nps {nps} time {stats.RunTime} pv {MovesToString(pv)} ";
    }
}