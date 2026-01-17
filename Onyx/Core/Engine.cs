using Onyx.Statics;


namespace Onyx.Core;

public sealed class Engine
{
    public static string Version => "0.10.4";

    private static int MateScore => 30000;
    private const int Infinity = 1_000_000;

    public Position Position;
    public TranspositionTable TranspositionTable { get; }
    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];

    private readonly Stopwatch _stopwatch;

    public SearchStatistics Statistics;
    private int _currentSearchId = 0;
    public volatile bool stopFlag;
    
    

    public Engine()
    {
        Statistics = new SearchStatistics();
        Position = new Position();
        TranspositionTable = new TranspositionTable();
        _stopwatch = new Stopwatch();
    }

    public void SetPosition(string fen)
    {
        Position.SetFen(fen);
    }

    public void Reset(string fen = Fen.DefaultFen)
    {
        Position = new Position(fen);
        _stopwatch.Reset();
        Statistics = new SearchStatistics();
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
    }

    private string GetSearchInfoString(SearchResults results, SearchStatistics stats)
    {
        List<Move> pv = results.PV;
        var nps = 0;
        if (stats.RunTime > 0)
            nps = (int)(stats.Nodes / (float)stats.RunTime) * 1000;

        var scoreString = "";

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
            $"info depth {stats.Depth} multipv 1 {scoreString} nodes {stats.Nodes} nps {nps} time {stats.RunTime} pv {MoveHelpers.MovesToString(pv)} ";
    }

    private void IterativeDeepeningSearch(SearchParameters parameters)
    {
        for (var depth = 1; depth <= parameters.MaxDepth; depth += 1)
        {
            // dont even enter the search if we need to exit
            if (stopFlag ||)
                break;

            // do the search from this depth.

            SearchFlag searchFlag = Search(depth, 0, -Infinity, Infinity);


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

            Statistics.Depth = depth;
            Statistics.RunTime = _engine.StopwatchManager.Elapsed;

            if (_searcherId == 0)
            {
                OnDepthFinished?.Invoke(_searchResults, Statistics);
            }

            // We found a way to win. No need to look deeper.
            if (_thisIterationResults.Score > MateScore - 100)
            {
                break;
            }
        }

        // didn't find a move
        if (SearchResults.BestMove.Data == 0)
        {
            Span<Move> moveBuffer = stackalloc Move[256];
            var legalMoveCount = MoveGenerator.GetLegalMoves(Position, moveBuffer);
            if (legalMoveCount > 0)
            {
                SearchResults.BestMove = moveBuffer[0];
            }
        }

        IsFinished = true;
        _startSignal.Reset();
    }

    internal readonly struct SearchFlag(bool completed, int score)
    {
        public bool Completed { get; } = completed;
        public int Score { get; } = score;

        public static SearchFlag Abort => new(false, 0);
        public static SearchFlag Zero => new(true, 0);
    }

    private SearchFlag Search(int depthRemaining, int depthFromRoot, int alpha, int beta)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (Statistics.Nodes % 2047 == 0 && stopFlag)
            return SearchFlag.Abort;

        if (depthFromRoot > 0)
        {
            // dont try to draw
            if (_currentPosition.HalfMoves >= 50 || Referee.IsThreeFoldRepetition(_currentPosition))
                return SearchFlag.Zero;
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
        foreach (var move in moves)
        {
            // make, search recursively, then undo the move
            _currentPosition.ApplyMove(move);
            var childResult = Search(depthRemaining - 1, depthFromRoot + 1, -beta, -alpha);
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

        TranspositionTable.Store(zobristHashValue,
            EncodeMateScore(alpha, depthFromRoot),
            depthRemaining,
            _currentSearchId,
            storingFlag,
            bestMove);

        return new SearchFlag(true, alpha);
    }

    private SearchFlag QuiescenceSearch(int alpha, int beta, Position position, int depthFromRoot)
    {
        _pvLength[depthFromRoot] = depthFromRoot;

        if (stopFlag)
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