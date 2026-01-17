using System.Text;
using Onyx.Statics;


namespace Onyx.Core;



public class Engine
{
    public static string Version => "0.10.4";
    // data members
    public Position Position = new();
    public TranspositionTable TranspositionTable { get; } = new();
    public Stopwatch Stopwatch { get; set; } = new();
    public int MateScore { get; private set; } = 30000;
    private CancellationToken _ct; // for threading
    public event Action<string> OnSearchInfoUpdate;
    // search members
    public int CurrentSearchId { get; private set; }
    private readonly List<Searcher> _workers = [];
    public int MaxThreads = 1;
    public SearchStatistics _statistics;
    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];

    public Engine()
    {
        IsReady = false;
        InitializeWorkerThreads();
        _workers[0].OnDepthFinished += (results, stats) => OnSearchInfoUpdate?.Invoke(GetSearchInfoString(results, stats));
        IsReady = true;
    }

    public void InitializeWorkerThreads()
    {
        for (var workerID = 0; workerID < MaxThreads; workerID++)
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

    
    public void SetPosition(string fen)
    {
        Position.SetFen(fen);
    }

    public void Reset()
    {
        IsReady = false;
        Position = new Position();
        Stopwatch = new Stopwatch();
        foreach (var worker in _workers)
        {
            worker.ResetState();
        }
        IsReady = true;
    }

    public bool IsReady { get; set; }

    public virtual SearchResults Search(SearchParameters searchParameters)
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
            timeLimit = TimeManager.TimeBudgetPerMove(this,searchParameters.TimeControl.Value);
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
        Stopwatch.Start(timeLimit - 30);
        var depthCount = 1;
        foreach (var worker in _workers)
        {
            //searchInstructions.StartDepth = depthCount;
            worker.TriggerSearch(searchInstructions, Position.Clone());
            depthCount++;
        }

        try
        {
            while (!_ct.IsCancellationRequested)
            {
                if (isTimed && Stopwatch.ShouldStop) break;
                if (_workers[0].IsFinished) break;
                
                Thread.Sleep(1);
            }
        }
        finally
        {
            foreach (var worker in _workers) worker.stopFlag = true;

            // Need to wait for the main worker to come back to root
            while (!_workers[0].IsFinished)
            {
                Thread.Sleep(1);
            }
        }

        var result = _workers[0]._searchResults;
        _statistics = _workers[0]._statistics;
        _statistics.RunTime = Stopwatch.Elapsed;
        Stopwatch.Reset();
        return result;
    }

    private string GetSearchInfoString(SearchResults results, SearchStatistics stats)
    {
        var pv = results.PV;
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;

        string scoreString = "";
        
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
            $"info depth {stats.Depth} multipv 1 {scoreString} nodes {stats.Nodes} nps {nps} time {stats.RunTime} pv {Helpers.MovesToString(pv)} ";
    }
    
    
    
    private SearchFlag QuiescenceSearch(int alpha, int beta, Position position, int depthFromRoot, bool timed)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (_ct.IsCancellationRequested || (timed && Stopwatch.ShouldStop))
            return SearchFlag.Abort;

        var eval = Evaluator.Evaluate(position);
        if (eval >= beta)
            return new SearchFlag(true, beta);


        if (eval > alpha)
            alpha = eval;

        _statistics.QuiescencePlyReached = depthFromRoot;
        _statistics.Nodes++;

        Span<Move> moveBuffer = stackalloc Move[128];
        var moveCount = MoveGenerator.GetLegalMoves(Position, moveBuffer, capturesOnly: true);

        var moves = moveBuffer[..moveCount];

        if (moves.Length > 1)
            Evaluator.SortMoves(moves, null, _killerMoves, depthFromRoot);

        foreach (var move in moves)
        {
            Position.ApplyMove(move);
            var child = QuiescenceSearch(-beta, -alpha, Position, depthFromRoot + 1, timed);
            Position.UndoMove(move);
            if (!child.Completed) return SearchFlag.Abort;
            eval = -child.Score;

            // beta cutoff - the opponent won't let it get here
            if (eval >= beta) return new SearchFlag(true, beta);

            if (eval > alpha)
            {
                alpha = eval;

                _pvTable[depthFromRoot, depthFromRoot] = move;
                var nextPlyDepth = _pvLength[depthFromRoot + 1];
                for (var nextPly = depthFromRoot + 1; nextPly < nextPlyDepth; nextPly++)
                {
                    _pvTable[depthFromRoot, nextPly] = _pvTable[depthFromRoot + 1, nextPly];
                }
                _pvLength[depthFromRoot] = Math.Max(depthFromRoot + 1, nextPlyDepth);
            }
        }
        return new SearchFlag(true, alpha);
    }
    
    internal readonly struct SearchFlag(bool completed, int score)
    {
        public bool Completed { get; } = completed;
        public int Score { get; } = score;

        public static SearchFlag Abort => new(false, 0);
        public static SearchFlag Zero => new(true, 0);
    }
    
    private void StoreKillerMove(Move move, int ply)
    {
        var existingMove = _killerMoves[ply, 0];
        if (existingMove == null)
        {
            _killerMoves[ply, 0] = move;
            return;
        }

        // don't store the same move twice
        if (existingMove!.Value.Data == move.Data)
            return;

        _killerMoves[ply, 0] = move;
        _killerMoves[ply, 1] = existingMove;
    }

    private const int Infinity = 1_000_000;

    private int EncodeMateScore(int score, int depthFromRoot)
    {
        if (score > MateScore - 1000) return score + depthFromRoot;
        if (score < -(MateScore - 1000)) return score - depthFromRoot;
        return score;
    }

    private int DecodeMateScore(int score, int depthFromRoot)
    {
        if (score > MateScore - 1000) return score - depthFromRoot;
        if (score < -(MateScore - 1000)) return score + depthFromRoot;
        return score;
    }
}