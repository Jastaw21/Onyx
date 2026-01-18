using Onyx.Statics;


namespace Onyx.Core;

public struct SearcherInstructions
{
    public int MaxDepth = 128;
    public int StartDepth = 1;
    public int DepthInterval = 1;

    public SearcherInstructions(bool isTimed, long timeLimit, int maxDepth, int startDepth, int depthInterval)
    {
        MaxDepth = maxDepth;
        StartDepth = startDepth;
        DepthInterval = depthInterval;
    }
}

public class Searcher(Engine engine, int searcherId = 0)
{
    // engine interaction
    private Engine _engine = engine;
    public volatile bool StopFlag;
    public SearchResults SearchResults;
    private SearchResults _thisIterationResults;
    public SearchStatistics Statistics;
    public bool IsFinished;
    private const int Reduction = -1;
    public int LMRThreshold = 5;

    private readonly int _searcherId = searcherId;
    private readonly AutoResetEvent _startSignal = new(false);
    private bool _isQuitting;

    // internal search variables

    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];
    private const int MaxExtensions = 16;
    private int _extensions = 0;
    private bool searchAsWhite = false;

    private SearcherInstructions _currentInstructions;
    private Position _currentPosition = new();
    public event Action<SearchResults, SearchStatistics> OnDepthFinished;

    public void Start()
    {
        while (!_isQuitting)
        {
            _startSignal.WaitOne(); // pause here waiting for engine instruction

            if (_isQuitting) break;

            IsFinished = false;
            StopFlag = false;

            IterativeDeepeningSearch(_currentInstructions);

            IsFinished = true;
        }
    }

    public void Quit()
    {
        _isQuitting = true;
        _startSignal.Set();
    }

    public void TriggerSearch(SearcherInstructions inst, Position pos)
    {
        ResetState();
        _currentInstructions = inst;
        _currentPosition = pos;
        _startSignal.Set();
    }

    public void ResetState()
    {
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        Statistics = new SearchStatistics();
        _thisIterationResults = new SearchResults();
        SearchResults = new SearchResults();
        IsFinished = false;
        _extensions = 0;
    }


    private void IterativeDeepeningSearch(SearcherInstructions searchParameters)
    {
        Reset();
        searchAsWhite = _currentPosition.WhiteToMove;

        // some vague diversification stuff
        var startDepth = _searcherId == 0 ? 1 : _searcherId % 2 == 0 ? 1 : 2;
        var depthInterval = _searcherId == 0 ? 1 : Math.Max(_searcherId % 3, 1);

        for (var depth = startDepth; depth <= searchParameters.MaxDepth; depth += depthInterval)
        {
            // dont even enter the search if we need to exit
            if (StopFlag)
                break;

            // do the search from this depth.
            SearchFlag searchFlag;
            try
            {
                _extensions = 0;
                searchFlag = Search(depth, 0, -Infinity, Infinity);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // if we timed out or were stopped, we can't use the results of this depth
            if (!searchFlag.Completed)
            {
                break;
            }

            SearchResults = _thisIterationResults;
            SearchResults.Pv = [];
            for (var i = 0; i < _pvLength[0]; i++)
            {
                SearchResults.Pv.Add(_pvTable[0, i]);
            }

            Statistics.Depth = depth;
            Statistics.RunTime = _engine.StopwatchManager.Elapsed;

            if (_searcherId == 0)
            {
                OnDepthFinished?.Invoke(SearchResults, Statistics);
            }

            // We found a way to win. No need to look deeper.
            if (_thisIterationResults.Score > _engine.MateScore - 100)
            {
                break;
            }
        }

        // didn't find a move
        if (SearchResults.BestMove.Data == 0)
        {
            Span<Move> moveBuffer = stackalloc Move[256];
            var legalMoveCount = MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer);
            if (legalMoveCount > 0)
            {
                SearchResults.BestMove = moveBuffer[0];
            }
        }

        IsFinished = true;
        _startSignal.Reset();
    }

    private void Reset()
    {
        Statistics = new SearchStatistics();
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        Array.Clear(_killerMoves);
        _thisIterationResults = new SearchResults();
        IsFinished = false;
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
        if (score > _engine.MateScore - 1000) return score + depthFromRoot;
        if (score < -(_engine.MateScore - 1000)) return score - depthFromRoot;
        return score;
    }

    private int DecodeMateScore(int score, int depthFromRoot)
    {
        if (score > _engine.MateScore - 1000) return score - depthFromRoot;
        if (score < -(_engine.MateScore - 1000)) return score + depthFromRoot;
        return score;
    }

    private SearchFlag Search(int depthRemaining, int depthFromRoot, int alpha, int beta)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (Statistics.Nodes % 2047 == 0 && StopFlag)
            return SearchFlag.Abort;

        if (depthFromRoot > 0)
        {
            // dont try to draw
            if (_currentPosition.HalfMoves >= 50 || Referee.IsRepetition(_currentPosition))
            {
                var isOurTurn = _currentPosition.WhiteToMove == searchAsWhite;
                var drawContempt = isOurTurn ? -100 : 100;
                return new SearchFlag(true, drawContempt); // draw contempt
            }
        }

        // if we have already evaluated this to at least the same depth, and that bounds are OK
        var zobristHashValue = _currentPosition.ZobristState;
        var ttValue = _engine.TranspositionTable.Retrieve(zobristHashValue);
        if (ttValue.HasValue)
        {
            if (ttValue.Value.ShouldUseEntry(alpha, beta, depthRemaining, zobristHashValue))
            {
                Statistics.TtHits++;
                var ttEval = DecodeMateScore(ttValue.Value.Eval, depthFromRoot);
                if (depthFromRoot == 0)
                {
                    if (ttValue.Value.BestMove.Data != 0)
                    {
                        _thisIterationResults.BestMove = ttValue.Value.BestMove;
                    }

                    _thisIterationResults.Score = ttEval;
                }

                if (ttValue.Value.BestMove.Data != 0)
                {
                    _pvTable[depthFromRoot, depthFromRoot] = ttValue.Value.BestMove;

                    _currentPosition.ApplyMove(ttValue.Value.BestMove);
                    var nextPlyDepth = GetPvFromTt(depthFromRoot + 1, depthRemaining - 1);
                    _currentPosition.UndoMove(ttValue.Value.BestMove);

                    for (var nextPly = depthFromRoot + 1; nextPly < nextPlyDepth; nextPly++)
                    {
                        _pvTable[depthFromRoot, nextPly] = _pvTable[depthFromRoot + 1, nextPly];
                    }

                    _pvLength[depthFromRoot] = nextPlyDepth;
                }

                return new SearchFlag(true, ttEval);
            }
        }

        Statistics.Nodes++;

        // leaf node
        if (depthRemaining == 0)
        {
            var qEval = QuiescenceSearch(alpha, beta, _currentPosition, depthFromRoot);
            if (!qEval.Completed)
                return SearchFlag.Abort;

            _pvLength[depthFromRoot] = _pvLength[depthFromRoot];
            return new SearchFlag(true, qEval.Score);
        }

        // get the moves
        Span<Move> moveBuffer = stackalloc Move[256];
        var legalMoveCount = MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer);
        var moves = moveBuffer[..legalMoveCount];

        // no legal moves left - decide if its checkmate or stalemate
        if (legalMoveCount == 0)
        {
            if (Referee.IsInCheck(_currentPosition.WhiteToMove, _currentPosition))
            {
                return new SearchFlag(true, -(_engine.MateScore - depthFromRoot));
            }

            return SearchFlag.Zero;
        }

        // order the moves
        if (moves.Length > 1)
            Evaluator.SortMoves(moves, ttValue?.BestMove ?? new Move(), _killerMoves, depthFromRoot);

        var storingFlag = BoundFlag.Upper;
        Move bestMove = default;

        // start to search through each of them
        var moveCount = 0;
        foreach (var move in moves)
        {
            moveCount++;
            _currentPosition.ApplyMove(move);

            // extend in scenarios it'd be beneficial
            var extension = 0;
            if (_extensions < MaxExtensions)
            {
                if (Referee.IsInCheck(_currentPosition.WhiteToMove, _currentPosition))
                    extension = 1;
            }


            var needsFullSearch = true;
            SearchFlag childResult = new SearchFlag(false, 0);

            // reduce later moves as the best ones should be up front
            if (moveCount >= LMRThreshold && extension == 0 && depthRemaining > 2 && move.CapturedPiece == 0)
            {
                // search with a super narrow window - basically only checking if any of these are better than alpha
                childResult = Search(depthRemaining - 1 + Reduction, depthFromRoot + 1, -alpha - 1, -alpha);
                needsFullSearch = -childResult.Score > alpha;
            }

            if (needsFullSearch)
                childResult = Search(depthRemaining - 1 + extension, depthFromRoot + 1, -beta, -alpha);

            _currentPosition.UndoMove(move);

            // timed out in a child node
            if (!childResult.Completed)
                return SearchFlag.Abort;


            var eval = -childResult.Score;

            // move was too good, opponent will avoid it as had a better move available earlier.
            if (eval >= beta)
            {
                Statistics.BetaCutoffs++;

                // store as a lower bound, as we know we might be able to get better if the opponent doesn't avoid it
                _engine.TranspositionTable.Store(zobristHashValue, EncodeMateScore(beta, depthFromRoot), depthRemaining,
                    _engine.CurrentSearchId,
                    BoundFlag.Lower, move);

                if (move.CapturedPiece == 0)
                    StoreKillerMove(move, depthFromRoot);

                // the best we can get in this chain is beta, since the opponent will avoid it - exit now
                return new SearchFlag(true, beta);
            }


            // we've bettered our previous best
            if (eval > alpha)
            {
                alpha = eval;
                bestMove = move;
                storingFlag = BoundFlag.Exact; // exact bound

                _pvTable[depthFromRoot, depthFromRoot] = move;
                var nextPlyDepth = _pvLength[depthFromRoot + 1];
                for (var nextPly = depthFromRoot + 1; nextPly < nextPlyDepth; nextPly++)
                {
                    _pvTable[depthFromRoot, nextPly] = _pvTable[depthFromRoot + 1, nextPly];
                }

                _pvLength[depthFromRoot] = Math.Max(depthFromRoot + 1, nextPlyDepth);

                if (depthFromRoot == 0)
                {
                    _thisIterationResults.BestMove = bestMove;
                    _thisIterationResults.Score = alpha;
                }
            }
        }

        _engine.TranspositionTable.Store(zobristHashValue,
            EncodeMateScore(alpha, depthFromRoot),
            depthRemaining,
            _engine.CurrentSearchId,
            storingFlag,
            bestMove);

        return new SearchFlag(true, alpha);
    }

    private int GetPvFromTt(int depthFromRoot, int depthRemaining)
    {
        _pvLength[depthFromRoot] = depthFromRoot;
        if (depthRemaining <= 0) return depthFromRoot;

        var ttValue = _engine.TranspositionTable.Retrieve(_currentPosition.ZobristState);
        if (ttValue.HasValue && ttValue.Value.BestMove.Data != 0)
        {
            var move = ttValue.Value.BestMove;
            _pvTable[depthFromRoot, depthFromRoot] = move;

            _currentPosition.ApplyMove(move);
            var nextPlyDepth = GetPvFromTt(depthFromRoot + 1, depthRemaining - 1);
            _currentPosition.UndoMove(move);

            for (var nextPly = depthFromRoot + 1; nextPly < nextPlyDepth; nextPly++)
            {
                _pvTable[depthFromRoot, nextPly] = _pvTable[depthFromRoot + 1, nextPly];
            }

            return nextPlyDepth;
        }

        return depthFromRoot;
    }

    private SearchFlag QuiescenceSearch(int alpha, int beta, Position position, int depthFromRoot)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (StopFlag)
            return SearchFlag.Abort;

        var eval = Evaluator.Evaluate(position);
        if (eval >= beta)
        {
            return new SearchFlag(true, beta);
        }

        if (eval > alpha)
        {
            alpha = eval;
        }

        Statistics.QuiescencePlyReached = depthFromRoot;
        Statistics.Nodes++;

        Span<Move> moveBuffer = stackalloc Move[128];
        var moveCount = MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer, capturesOnly: true);

        var moves = moveBuffer[..moveCount];

        if (moves.Length > 1)
            Evaluator.SortMoves(moves, null, _killerMoves, depthFromRoot);

        foreach (var move in moves)
        {
            _currentPosition.ApplyMove(move);
            var child = QuiescenceSearch(-beta, -alpha, _currentPosition, depthFromRoot + 1);
            _currentPosition.UndoMove(move);
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
}