using Onyx.Statics;


namespace Onyx.Core;

public struct SearcherInstructions
{
    public bool IsTimed = true;
    public long TimeLimit = 0;
    public int MaxDepth = 128;
    public int StartDepth = 1;
    public int DepthInterval = 1;


    public SearcherInstructions(bool isTimed, long timeLimit, int maxDepth, int startDepth, int depthInterval)
    {
        IsTimed = isTimed;
        TimeLimit = timeLimit;
        MaxDepth = maxDepth;
        StartDepth = startDepth;
        DepthInterval = depthInterval;
    }
}

public class Searcher(Engine engine, int searcherId = 0)
{
    // engine interaction
    private Engine _engine = engine;
    public volatile bool stopFlag;
    public SearchResults _searchResults;
    private SearchResults _thisIterationResults;
    public SearchStatistics _statistics;
    public bool IsFinished;

    private int _searcherId = searcherId;
    private readonly AutoResetEvent _startSignal = new(false);
    private bool _isQuitting;

    // internal search variables

    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];


    private SearcherInstructions _currentInstructions;
    private Position _currentPosition;


    public void Start()
    {
        while (!_isQuitting)
        {
            _startSignal.WaitOne(); // pause here waiting for engine instruction

            if (_isQuitting) break;

            IsFinished = false;
            stopFlag = false;

            IterativeDeepeningSearch(_currentInstructions, _currentPosition);

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
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        _statistics = new SearchStatistics();
        _currentInstructions = inst;
        _currentPosition = pos;
        _searchResults = new SearchResults();
        _startSignal.Set();
    }

    private void IterativeDeepeningSearch(SearcherInstructions searchParameters, Position _position)
    {
        Reset();

        // some vague diversification stuff
        var startDepth = _searcherId == 0 ? 1 : _searcherId % 2 == 0 ? 1 : 2;
        var depthInterval = _searcherId == 0 ? 1 : Math.Max(_searcherId % 3, 1);

        for (var depth = startDepth; depth <= searchParameters.MaxDepth; depth += depthInterval)
        {
            // dont even enter the search if we need to exit
            if (stopFlag)
                break;

            // do the search from this depth.
            var searchResult = Search(depth, 0, int.MinValue, int.MaxValue);

            // if we timed out or were stopped, we can't use the results of this depth
            if (!searchResult.Completed)
                break;
            
            _searchResults = _thisIterationResults;

            // We found a way to win. No need to look deeper.
            if (_thisIterationResults.Score > _engine.MateScore - 100)
            {
                break;
            }

            
        }

        // didn't find a move
        if (_thisIterationResults.BestMove.Data == 0)
        {
            Span<Move> moveBuffer = stackalloc Move[256];
            MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer);
            _searchResults.BestMove = moveBuffer[0];
        }

        IsFinished = true;
    }

    private void Reset()
    {
        _statistics = new SearchStatistics();
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        Array.Clear(_killerMoves);
        _thisIterationResults = new SearchResults();
        IsFinished = false;
    }

    internal readonly struct SearchFlag(bool completed, int value)
    {
        public bool Completed { get; } = completed;
        public int Value { get; } = value;

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

    SearchFlag Search(int depthRemaining, int depthFromRoot, int alpha, int beta)
    {
        if (stopFlag)
            return SearchFlag.Abort;

        if (depthFromRoot > 0)
        {
            // dont try to draw
            if (_currentPosition.HalfMoves >= 50 || Referee.IsThreeFoldRepetition(_currentPosition))
                return SearchFlag.Zero;
        }

        // see if we have already evaluated this to at least the same depth, and that bounds are OK
        var zobristHashValue = _currentPosition.Zobrist.HashValue;
        var ttValue = _engine.TranspositionTable.Retrieve(zobristHashValue);
        if (ttValue.HasValue)
        {
            if (ttValue.Value.ShouldUseEntry(alpha, beta, depthFromRoot, zobristHashValue))
            {
                _thisIterationResults.BestMove = ttValue.Value.BestMove;
                _thisIterationResults.Score = ttValue.Value.Eval;
                return new SearchFlag(true, ttValue.Value.Eval);
            }
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
        

        // leaf node
        if (depthRemaining == 0)
        {
            var evaluation = Evaluator.Evaluate(_currentPosition);
            return new SearchFlag(true, evaluation);
        }

        // order the moves
        Evaluator.SortMoves(moves, ttValue?.BestMove ?? new Move(), _killerMoves, depthFromRoot);

        var storingFlag = BoundFlag.Upper;
        Move bestMove = default;

        // start to search through each of them
        foreach (var move in moves)
        {
            // make, search recursively, then undo the move
            _currentPosition.ApplyMove(move);
            var childResult = Search(depthRemaining - 1, depthFromRoot + 1, -beta, alpha);
            _currentPosition.UndoMove(move);

            // timed out in a child node
            if (!childResult.Completed)
                return SearchFlag.Abort;


            var eval = -childResult.Value;

            // move was too good, opponent will avoid it as had a better move available earlier.
            if (eval >= beta)
            {
                _statistics.BetaCutoffs++;

                // store as a lower bound, as we know we might be able to get better if the opponent doesn't avoid it
                _engine.TranspositionTable.Store(zobristHashValue, beta, depthFromRoot, _engine.CurrentSearchId,
                    BoundFlag.Lower, move);

                // the best we can get in this chain is beta, since the opponent will avoid it - exit now
                return new SearchFlag(true, beta);
            }


            // we've bettered our previous best
            if (eval > alpha)
            {
                alpha = eval;
                bestMove = move;
                storingFlag = BoundFlag.Exact; // exact bound

                if (depthFromRoot == 0)
                {
                    _thisIterationResults.BestMove = bestMove;
                    _thisIterationResults.Score = alpha;
                }
            }
        }

        _engine.TranspositionTable.Store(zobristHashValue, alpha, depthFromRoot, _engine.CurrentSearchId, storingFlag,
            bestMove);

        return new SearchFlag(true, alpha);
    }
}