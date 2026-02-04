using System.Diagnostics.CodeAnalysis;
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
    public volatile bool StopFlag;
    public SearchResults SearchResults;
    private SearchResults _thisIterationResults;
    public SearchStatistics Statistics;
    public bool IsFinished;
    private const int Reduction = -1;
    public int LmrThreshold = 3;
    public int DeltaMargin = 1000;
    public int DeltaPerMove = 100;

    private readonly AutoResetEvent _startSignal = new(false);
    private bool _isQuitting;

    // internal search variables

    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];

    private SearcherInstructions _currentInstructions;
    private Position _currentPosition = new();
    public event Action<SearchResults, SearchStatistics> OnDepthFinished = null!;


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
    }

    private void IterativeDeepeningSearch(SearcherInstructions searchParameters)
    {
        Reset();

        // some vague diversification stuff
        var startDepth = searcherId == 0 ? 1 : searcherId % 2 == 0 ? 1 : 2;
        var depthInterval = searcherId == 0 ? 1 : Math.Max(searcherId % 3, 1);

        for (var depth = startDepth; depth <= searchParameters.MaxDepth; depth += depthInterval)
        {
            // dont even enter the search if we need to exit
            if (StopFlag)
                break;

            // do the search from this depth.
            SearchFlag searchFlag;
            try
            {
                searchFlag = Search(depth, 0, -Infinity, Infinity, lastMoveNulled: false);
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

            // build the pv
            SearchResults = _thisIterationResults;
            SearchResults.Pv = [];
            for (var i = 0; i < _pvLength[0]; i++)
            {
                SearchResults.Pv.Add(_pvTable[0, i]);
            }

            Statistics.Depth = depth;
            Statistics.RunTime = engine.StopwatchManager.Elapsed;

            // on completion of each depth emit an info string
            if (searcherId == 0)
            {
                OnDepthFinished?.Invoke(SearchResults, Statistics);
            }

            // We found a way to win. No need to look deeper.
            if (_thisIterationResults.Score > Engine.MateScore - 100)
                break;
        }

        // didn't find a move - get the first legal one.
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
        public static SearchFlag NullMoveFailed => new(true, -1);
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

    private SearchFlag Search(int depthRemaining, int depthFromRoot, int alpha, int beta, bool lastMoveNulled = false)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (Statistics.Nodes % 2047 == 0 && StopFlag)
            return SearchFlag.Abort;

        // entering at a drawn position
        if (depthFromRoot > 0)
        {
            if (_currentPosition.HalfMoves >= 50 || Referee.IsRepetition(_currentPosition))
            {
                return SearchFlag.Zero;
            }
        }


        // See if this position has already been searched for
        var zobristHashValue = _currentPosition.ZobristState;
        var entryExists = engine.TranspositionTable.TryRetrieve(zobristHashValue, out var ttValue);
        if (entryExists)
        {
            // it has, now check if it's usable (bounds, sufficient depth etc)
            if (engine.TranspositionTable.PollEntry(ttValue, alpha, beta, depthRemaining, zobristHashValue))
            {
                var ttEval = DecodeMateScore(ttValue.Eval, depthFromRoot);

                if (depthFromRoot == 0)
                {
                    if (ttValue.BestMove.Data != 0)
                        _thisIterationResults.BestMove = ttValue.BestMove;
                    _thisIterationResults.Score = ttEval;
                }
                else // Only cutoff if not at root
                {
                    if (ttValue.BestMove.Data != 0)
                        ExtractPvData(depthRemaining, depthFromRoot, ttValue);

                    Statistics.HashCutoffs++;
                    return new SearchFlag(true, ttEval); // Return on ANY bound!
                }
            }
        }

        // we'll now be searching this node
        Statistics.Nodes++;

        // leaf node - continue until the position is quiet before evaluating
        if (depthRemaining == 0)
        {
            var qEval = QuiescenceSearch(alpha, beta, depthFromRoot);
            if (!qEval.Completed)
                return SearchFlag.Abort;

            _pvLength[depthFromRoot] = _pvLength[depthFromRoot];
            return new SearchFlag(true, qEval.Score);
        }

       
        var isInCheck = Referee.IsInCheck(_currentPosition.WhiteToMove, _currentPosition);

        // we're so far ahead that it's not worth searching the remainder
        if (!isInCheck && depthRemaining is <= 6 and > 0 && !lastMoveNulled && depthFromRoot > 0)
        {
            var staticEval = Evaluator.Evaluate(_currentPosition);
            var margin = staticEval + depthRemaining * 80;
            if (staticEval - margin >= beta && Math.Abs(beta) < 20000) // seeks to avoid mate scores
            {
                Statistics.RFPCutoffs++;
                return new SearchFlag(true, staticEval - margin);
            }
        }
        
        // try to cut off a node if making no move has no effect on our position. We can skip a move and still be winning
        // so why would we search it?
        if (!isInCheck && depthRemaining >= 3 && depthFromRoot > 0 && !lastMoveNulled)
        {
            if (NullMoveReduction(depthRemaining, depthFromRoot, beta, out var searchFlag)) return searchFlag;
        }

        // get the moves
        Span<Move> moveBuffer = stackalloc Move[256];
        var legalMoveCount = MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer, alreadyKnowBoardInCheck: true,
            isAlreadyInCheck: isInCheck);
        var moves = moveBuffer[..legalMoveCount];

        // no legal moves left - decide if its checkmate or stalemate
        if (legalMoveCount == 0)
        {
            return isInCheck ? new SearchFlag(true, -(Engine.MateScore - depthFromRoot)) : SearchFlag.Zero;
        }

        // order the moves
        Evaluator.SortMoves(moves, entryExists ? ttValue.BestMove : default, _killerMoves, depthFromRoot);

        // start to search through each of them
        var moveCount = 0;
        var storingFlag = BoundFlag.Upper;
        Move bestMove = default;
        foreach (var move in moves)
        {
            moveCount++;
            _currentPosition.ApplyMove(move);
            
            // extend in scenarios it'd be beneficial
            var extension = GetExtension();
            
            var childResult = new SearchFlag(false, 0);

            // reduce later moves as the best ones should be up front
            var needsFullSearch = TryLateMoveReduction(depthRemaining, depthFromRoot, alpha, moveCount, move, extension, ref childResult);

            // either we didn't reduce, or we did and unexpectedly got a good move. Either way, do a full search.
            if (needsFullSearch)
                childResult = Search(depthRemaining - 1 + extension,
                    depthFromRoot + 1, -beta, -alpha, false);


            _currentPosition.UndoMove(move);

            // timed out in a child node
            if (!childResult.Completed)
                return SearchFlag.Abort;

            var eval = -childResult.Score;


            // move was too good, opponent will avoid it as had a better move available earlier.
            if (eval >= beta)
            {
                if (moveCount == 1)
                    Statistics.FirstMoveCutoffs++;
                Statistics.BetaCutoffs++;

                // store as a lower bound, as we know we might be able to get better if the opponent doesn't avoid it
                engine.TranspositionTable.Store(zobristHashValue, EncodeMateScore(alpha, depthFromRoot), depthRemaining,
                    engine.CurrentSearchId,
                    BoundFlag.Lower, move);

                // beta cutoffs need to store killer moves
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


        engine.TranspositionTable.Store(zobristHashValue, EncodeMateScore(alpha, depthFromRoot),
            depthRemaining, engine.CurrentSearchId, storingFlag, bestMove);


        return new SearchFlag(true, alpha);
    }

    private int GetExtension()
    {
        var incheck = Referee.IsInCheck(_currentPosition.WhiteToMove, _currentPosition);
        var extension = incheck ? 1 : 0;
        return extension;
    }

    private bool TryLateMoveReduction(int depthRemaining, int depthFromRoot, int alpha, int moveCount, Move move,
        int extension, ref SearchFlag childResult)
    {
        var needsFullSearch = true;
        var lmrThreshold = depthRemaining <= 3 ? 5 : 4;
        if (moveCount < lmrThreshold || depthRemaining <= 2 || move.CapturedPiece != 0 || extension != 0)
            return true;
        
        
        var reduction = 1;
                
        if (depthRemaining > 6 && moveCount > 8)
            reduction = -2;  // Only go to 2 ply in deep searches of late moves

        if (IsKillerMove(move, depthFromRoot))
            reduction = Math.Min(reduction + 1, 0);
                
        if (reduction >0)

        {
            // search with a super narrow window - basically only checking if any of these are better than alpha
            Statistics.ReducedSearches++;
            childResult = Search(depthRemaining - 1 - reduction,
                depthFromRoot + 1, -alpha - 1, -alpha, lastMoveNulled: false);

            var reducedEval = -childResult.Score;
            needsFullSearch = reducedEval > alpha;
            if (needsFullSearch)
                Statistics.FullResearches++;
        }

        return needsFullSearch;
    }

    private bool NullMoveReduction(int depthRemaining, int depthFromRoot, int beta, out SearchFlag searchFlag)
    {
        _currentPosition.MakeNullMove();
        var nullMoveReduction = depthRemaining > 6 ? 3 : 2;
        var nmrResult = Search(depthRemaining - 1 - nullMoveReduction, depthFromRoot + 1, -beta, -beta + 1, true);
        _currentPosition.UndoNullMove();

        if (!nmrResult.Completed)
        {
            searchFlag = SearchFlag.Abort;
            return true;
        }

        var nullScore = -nmrResult.Score;

        if (nullScore >= beta)
        {
            Statistics.NullMoveCutoffs++;
            searchFlag = new SearchFlag(true, beta);
            return true;
        }

        Statistics.FailedNullMoveCutoffs++;
        searchFlag = SearchFlag.NullMoveFailed;
        return false;
    }

    private SearchFlag QuiescenceSearch(int alpha, int beta, int depthFromRoot)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (Statistics.Nodes % 2047 == 0 && StopFlag)
            return SearchFlag.Abort;

        if (Referee.IsRepetition(_currentPosition) || _currentPosition.HalfMoves >= 50)
        {
            _pvLength[depthFromRoot + 1] = depthFromRoot + 1;
            return SearchFlag.Zero;
        }

        var isInCheck = Referee.IsInCheck(_currentPosition.WhiteToMove, _currentPosition);

        // stand pat to prevent explosion. This says that we're not necessarily forced to capture
        var standPat = engine.EvaluationTable.Evaluate(_currentPosition, engine.CurrentSearchId);

        if (!isInCheck)
        {
            if (standPat >= beta) return new SearchFlag(true, beta); // beta cutoff

            var originalAlpha = alpha;
            if (standPat > alpha) alpha = standPat;

            if (standPat + DeltaMargin <= originalAlpha) // margin tuned
            {
                // delta cutoff
                Statistics.DeltaCutoffs++;
                return new SearchFlag(true, alpha);
            }
        }

        Span<Move> moveBuffer = stackalloc Move[128];
        // if we're in check need to search evasions too.
        var moveCount =
            isInCheck
                ? MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer,
                    alreadyKnowBoardInCheck: true,
                    isAlreadyInCheck: isInCheck,
                    capturesOnly: false
                )
                : MoveGenerator.GetLegalMoves(_currentPosition, moveBuffer,
                    alreadyKnowBoardInCheck: true,
                    isAlreadyInCheck: isInCheck,
                    capturesOnly: true
                );

        var moves = moveBuffer[..moveCount];

        Evaluator.SortMoves(moves, new Move(), _killerMoves, depthFromRoot);


        foreach (var move in moves)
        {
            if (!isInCheck && move.CapturedPiece != 0)
            {
                var capturedPiece = PieceTypes.PieceTypeIndex(move.CapturedPiece);
                var capturedValue = Evaluator.PieceValues[capturedPiece];
                if (standPat + capturedValue + DeltaPerMove < alpha)
                {
                    Statistics.DeltaPerMove++;
                    continue;
                }
            }

            Statistics.Nodes++;
            Statistics.qNodes++;

            _currentPosition.ApplyMove(move);
            var child = QuiescenceSearch(-beta, -alpha, depthFromRoot + 1);
            _currentPosition.UndoMove(move);
            if (!child.Completed) return SearchFlag.Abort;
            var score = -child.Score;

            // beta cutoff - the opponent won't let it get here
            if (score >= beta)
            {
                return new SearchFlag(true, beta);
            }

            if (score <= alpha) continue;


            // we've improved
            alpha = score;
            _pvTable[depthFromRoot, depthFromRoot] = move;
            var nextPlyDepth = _pvLength[depthFromRoot + 1];
            for (var nextPly = depthFromRoot + 1; nextPly < nextPlyDepth; nextPly++)
            {
                _pvTable[depthFromRoot, nextPly] = _pvTable[depthFromRoot + 1, nextPly];
            }

            _pvLength[depthFromRoot] = Math.Max(depthFromRoot + 1, nextPlyDepth);
        }

        return new SearchFlag(true, alpha);
    }

    private bool IsKillerMove(Move move, int ply)
    {
        return _killerMoves[ply, 0] == move || _killerMoves[ply, 1] == move;
    }

    private void ExtractPvData(int depthRemaining, int depthFromRoot, [DisallowNull] TtEntry? ttValue)
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

    private int GetPvFromTt(int depthFromRoot, int depthRemaining)
    {
        _pvLength[depthFromRoot] = depthFromRoot;
        if (depthRemaining <= 0) return depthFromRoot;

        var ttValue = engine.TranspositionTable.Retrieve(_currentPosition.ZobristState);
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

    private int EncodeMateScore(int score, int depthFromRoot)
    {
        if (score > Engine.MateScore - 1000) return score + depthFromRoot;
        if (score < -(Engine.MateScore - 1000)) return score - depthFromRoot;
        return score;
    }

    private int DecodeMateScore(int score, int depthFromRoot)
    {
        if (score > Engine.MateScore - 1000) return score - depthFromRoot;
        if (score < -(Engine.MateScore - 1000)) return score + depthFromRoot;
        return score;
    }
}