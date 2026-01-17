using Onyx.Statics;


namespace Onyx.Core;


public class Searcher()
{
    // engine interaction

    public SearchStatistics _statistics;
    public bool IsFinished;

   
    

    // internal search variables

    
   
    private Position _currentPosition;


    

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
        _searchResults = new SearchResults();
        _startSignal.Set();
    }

    public void ResetState()
    {
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        _statistics = new SearchStatistics();
        _thisIterationResults = new SearchResults();
        _searchResults = new SearchResults();
        _currentPosition = new Position();
        IsFinished = false;
    }

    public event Action<SearchResults, SearchStatistics> OnDepthFinished;

    private void IterativeDeepeningSearch(Position _position)
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
            SearchFlag searchFlag;
            try
            {
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

            _searchResults = _thisIterationResults;
            _searchResults.PV = [];
            for (var i = 0; i < _pvLength[0]; i++)
            {
                _searchResults.PV.Add(_pvTable[0, i]);
            }
            _statistics.Depth = depth;
            _statistics.RunTime = _engine.StopwatchManager.Elapsed;

            if (_searcherId == 0)
            {
                OnDepthFinished?.Invoke(_searchResults, _statistics);
            }

            // We found a way to win. No need to look deeper.
            if (_thisIterationResults.Score > _engine.MateScore - 100)
            {
                break;
            }
        }

        // didn't find a move
        if (_searchResults.BestMove.Data == 0)
        {
            Span<Move> moveBuffer = stackalloc Move[256];
            var legalMoveCount = MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer);
            if (legalMoveCount > 0)
            {
                _searchResults.BestMove = moveBuffer[0];
            }
        }

        IsFinished = true;
        _startSignal.Reset();
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

    
}