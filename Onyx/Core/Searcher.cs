using Onyx.Statics;


namespace Onyx.Core;

public struct SearcherInstructions
{
    public bool IsTimed = true;
    public long TimeLimit = 0;
    public int MaxDepth = 128;
    public int StartDepth = 1;
    public int DepthInterval = 1;
    public CancellationToken ct;

    public SearcherInstructions(bool isTimed, long timeLimit, int maxDepth, int startDepth, int depthInterval,
        CancellationToken ct_)
    {
        IsTimed = isTimed;
        TimeLimit = timeLimit;
        MaxDepth = maxDepth;
        StartDepth = startDepth;
        DepthInterval = depthInterval;
        ct = ct_;
    }
}

public class Searcher(Engine engine, int searcherId = 0)
{
    // internals
    private Engine _engine = engine;
    private readonly Move?[,] _killerMoves = new Move?[128, 2];
    private SearchStatistics _statistics;
    private readonly Move[,] _pvTable = new Move[128, 128];
    private readonly int[] _pvLength = new int[128];
    private CancellationToken ct;
    private int _searcherId = searcherId;

    // used by the search manager
    public volatile bool stopFlag = false;
    public SearchResults SearchResults { get; private set; } = new SearchResults();
    public bool IsFinished;

    private readonly AutoResetEvent _startSignal = new(false);
    private bool _isQuitting = false;
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
        SearchResults = new SearchResults();
        _startSignal.Set();
    }

    private void IterativeDeepeningSearch(SearcherInstructions searchParameters, Position _position)
    {
        _statistics = new SearchStatistics();
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        Array.Clear(_killerMoves);
        List<Move> pv = [];
        Move bestMove = default;
        var bestScore = 0;
        IsFinished = false;
        ct = searchParameters.ct;

        // some vague diversification stuff
        var startDepth = _searcherId == 0 ? 1 : _searcherId % 2 == 0 ? 1 : 2;
        var depthInterval = _searcherId == 0 ? 1 : Math.Max(_searcherId % 3, 1);


        for (var depth = startDepth; depth <= searchParameters.MaxDepth; depth += depthInterval)
        {
            var searchResult = ExecuteSearch(depth, _position);
            if (!searchResult.completed) continue;
            if (stopFlag || ct.IsCancellationRequested)
                break;

            bestMove = searchResult.bestMove;
            bestScore = searchResult.score;
            pv.Clear();
            Fen.BuildPVString(_pvTable, _pvLength, out pv);
            _statistics.Depth = depth;
            SearchResults = new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics, PV = pv };
            // We found a way to win. No need to look deeper.
            if (bestScore > _engine.MateScore - 100)
            {
                break;
            }
        }

        IsFinished = true;
    }

    private (bool completed, Move bestMove, int score) ExecuteSearch(int depth, Position _position)
    {
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(_position, moveBuffer);
        var moves = moveBuffer[..moveCount];
        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;

        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, _position))
                continue;
            _position.ApplyMove(move);
            var result = AlphaBeta(depth - 1, -beta, -alpha, _position, 1, false);
            _position.UndoMove(move);
            if (!result.Completed)
                return (false, default, 0);

            var score = -result.Value;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                _pvTable[0, 0] = bestMove;
                for (var i = 1; i < _pvLength[1]; i++)
                {
                    _pvTable[0, i] = _pvTable[1, i];
                }

                _pvLength[0] = _pvLength[1];
            }

            alpha = Math.Max(alpha, score);
        }

        return (true, bestMove, bestScore);
    }

    private SearchFlag AlphaBeta(int depth, int alpha, int beta, Position _position, int ply,
        bool nullMoveAllowed = false)
    {
        _pvLength[ply] = ply;
        if ((_statistics.Nodes & 2047) == 0 && (stopFlag || ct.IsCancellationRequested))
            return SearchFlag.Abort;

        _statistics.Nodes++;

        // get the moves
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(_position, moveBuffer);
        var moves = moveBuffer[..moveCount]; // for easier iteration, slice the moves down.

        // Early exit if found the TTMove at a greater depth
        var hash = _position.Zobrist.HashValue;
        var TTResult = _engine.TranspositionTable.Retrieve(hash);
        if (TTResult.HasValue && TTResult.Value.ShouldUseEntry(alpha, beta, depth, hash))
        {
            _statistics.TtHits++;
            return new SearchFlag(true, TTResult!.Value.Eval);
        }

        // exit if were in a end-game board state
        BoardStatus boardState;
        if (TTResult.HasValue)
        {
            boardState = TTResult.Value.BoardStatus;
        }

        else
            boardState = Referee.GetBoardState(_position);

        // no moves, either checkmate or stalemate
        if (moveCount == 0)
        {
            _pvLength[ply] = ply;
            return boardState == BoardStatus.Checkmate
                ? new SearchFlag(true, -(_engine.MateScore - ply))
                : new SearchFlag(true, 0); // stalemate
        }

        // reached our bottom depth, go to quiescence search
        if (depth == 0)
        {
            _pvLength[ply] = ply;
            if (boardState == BoardStatus.Checkmate)
                return new SearchFlag(true, -(_engine.MateScore - ply));

            return QuiescenceSearch(alpha, beta, _position, ply);
        }

        var alphaOrig = alpha;
        var bestValue = int.MinValue + 1;

        Move? ttMove = TTResult.HasValue ? TTResult.Value.BestMove : null;
        Evaluator.SortMoves(moves, ttMove, _killerMoves, ply);

        // check extension
        if (boardState == BoardStatus.Check && depth < 1)
            depth += 2;


        // ---- main loop ----

        Move bestMove = default;
        var legalMoveCount = 0;
        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, _position)) continue;
            legalMoveCount++;
            _position.ApplyMove(move);
            var child = AlphaBeta(depth - 1, -beta, -alpha, _position, ply + 1, false);
            _position.UndoMove(move);

            // timed out in a child search, propagate up
            if (!child.Completed)
                return SearchFlag.Abort;

            // invert the eval for the opponent
            var eval = -child.Value;

            // found a better move, so update our PV
            if (eval > bestValue)
            {
                bestValue = eval;
                bestMove = move;

                _pvTable[ply, ply] = move;
                for (int i = ply + 1; i < _pvLength[ply + 1]; i++)
                {
                    _pvTable[ply, i] = _pvTable[ply + 1, i];
                }

                _pvLength[ply] = _pvLength[ply + 1];
            }

            // alpha update - we beat the alpha.
            if (eval > alpha)
                alpha = eval;

            if (alpha < beta) continue;

            // beta cutoff
            _statistics.BetaCutoffs++;

            // store killer move if not a capture and caused a beta cutoff
            if (move.CapturedPiece == 0)
                StoreKillerMove(move, ply);
            break;
        }

        // handle if the board is in an illegal state
        var endGameScore = 0;
        var endGameScoreModified = false;
        if (legalMoveCount == 0)
        {
            endGameScoreModified = true;
            if (boardState == BoardStatus.Checkmate)
                endGameScore = -(_engine.MateScore - ply);
            else
                endGameScore = 0;
        }

        // store in transposition table but calculate the correct bound flag first
        BoundFlag flag;
        if (endGameScoreModified)
            bestValue = endGameScore;
        if (bestValue <= alphaOrig)
            flag = BoundFlag.Upper;
        else if (bestValue >= beta)
            flag = BoundFlag.Lower;
        else
            flag = BoundFlag.Exact;
        _engine.TranspositionTable.Store(hash, bestValue, depth, _engine.CurrentSearchId, flag, bestMove, boardState);
        _statistics.TtStores++;


        return new SearchFlag(true, bestValue);
    }

    private SearchFlag QuiescenceSearch(int alpha, int beta, Position _position, int ply)
    {
        // make sure we can handle timing out in quiescence search
        if (stopFlag || ct.IsCancellationRequested)
            return SearchFlag.Abort;

        // seb lague does this - not sure if it's needed though
        var eval = Evaluator.Evaluate(_position);
        if (eval >= beta) return new SearchFlag(true, beta);
        if (eval > alpha) alpha = eval;

        // make sure we can tarack the stats
        _statistics.QuiescencePlyReached = ply;
        _statistics.Nodes++;

        // get only capture moves
        Span<Move> moveBuffer = stackalloc Move[128];
        var moveCount = MoveGenerator.GetMoves(_position, moveBuffer, true);
        var moves = moveBuffer[..moveCount];

        if (moves.Length > 1)
            Evaluator.SortMoves(moves, null, null, ply);

        foreach (var move in moves)
        {
            _position.ApplyMove(move);
            var child = QuiescenceSearch(-beta, -alpha, _position, ply + 1);
            _position.UndoMove(move);
            if (!child.Completed) return SearchFlag.Abort;
            eval = -child.Value;

            if (eval >= beta) return new SearchFlag(true, beta);
            if (eval > alpha) alpha = eval;
        }

        return new SearchFlag(true, alpha);
    }

    internal readonly struct SearchFlag(bool completed, int value)
    {
        public bool Completed { get; } = completed;
        public int Value { get; } = value;

        public static SearchFlag Abort => new(false, 0);
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

    public void Reset()
    {
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
        _statistics = new SearchStatistics();
    }
}