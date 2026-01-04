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
    private TranspositionTable TranspositionTable { get; } = new();
    private StopwatchManager StopwatchManager { get; set; } = new();
    private const int MateScore = 30000;
    private CancellationToken _ct; // for threading
    private Move?[,] _killerMoves = new Move?[128, 2];
    private Move[,] _pvTable = new Move[128, 128];
    private int[] _pvLength = new int[128];

    // search members
    private SearchStatistics _statistics;
    private int _currentSearchId;
    private bool _loggingEnabled;
    private readonly TimeManager _timeManager;

    public Engine()
    {
        _timeManager = new TimeManager(this);
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
        _statistics = new SearchStatistics();
        Position = new Position();
        StopwatchManager = new StopwatchManager();
        _killerMoves = new Move?[128, 2];
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

    public SearchResults Search(SearchParameters searchParameters)
    {
        _statistics = new SearchStatistics();
        _currentSearchId++;
        Array.Clear(_killerMoves);
        Array.Clear(_pvTable);
        Array.Clear(_pvLength);
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

        StopwatchManager.Start(timeLimit);
        var depthLimit = searchParameters.MaxDepth ?? 100;
        return IterativeDeepeningSearch(depthLimit, isTimed);
    }

    private SearchResults IterativeDeepeningSearch(int depthLimit, bool isTimed)
    {
        Move bestMove = default;
        var bestScore = 0;
        List<Move> pv = [];
        for (var depth = 1; depth <= depthLimit; depth++)
        {
            var searchResult = ExecuteSearch(depth, isTimed);
            if (!searchResult.completed) continue;

            // stop flag thrown from gui, return best so far
            if (_ct.IsCancellationRequested)
            {
                return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics, PV = pv };
            }

            bestMove = searchResult.bestMove;
            bestScore = searchResult.score;
            pv.Clear();
            BuildPV(pv);
            _statistics.Depth = depth;

            if (bestScore > MateScore - 100)
            {
                // We found a way to win. No need to look deeper.
                break;
            }
        }

        _statistics.RunTime = StopwatchManager.Elapsed;
        Console.Error.WriteLine($"{_statistics.Get()}");

        return new SearchResults { BestMove = bestMove, Score = bestScore, Statistics = _statistics, PV = pv };
    }

    public void BuildPV(List<Move> pv)
    {
        for (int i = 0; i < _pvLength[0]; i++)
        {
            var move = _pvTable[0, i];
            if (move.Data == 0) break; // No more moves
            pv.Add(move);
        }
    }

    private (bool completed, Move bestMove, int score) ExecuteSearch(int depth, bool timed)
    {
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(Position, moveBuffer);
        var moves = moveBuffer[..moveCount];
        var bestMove = moves[0];
        var bestScore = int.MinValue + 1;

        var alpha = int.MinValue + 1;
        var beta = int.MaxValue;

        foreach (var move in moves)
        {
            if (!Referee.MoveIsLegal(move, Position))
                continue;
            Position.ApplyMove(move);
            var result = AlphaBeta(depth - 1, -beta, -alpha, Position, timed, 1, false);
            Position.UndoMove(move);
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

            if (_loggingEnabled)
                Logger.Log(LogType.Search, $"{Position.GetFen()} {move} Score: {score} Depth: {depth}");
            alpha = Math.Max(alpha, score);
        }

        return (true, bestMove, bestScore);
    }

    private SearchFlag AlphaBeta(int depth, int alpha, int beta, Position position, bool timed, int ply, bool nullMoveAllowed = false)
    {
        _pvLength[ply] = ply;
        if ((_statistics.Nodes & 2047) == 0)
        {
            if ((timed && StopwatchManager.ShouldStop) || _ct.IsCancellationRequested)
                return SearchFlag.Abort;
        }

        _statistics.Nodes++;

        // get the moves
        Span<Move> moveBuffer = stackalloc Move[256];
        var moveCount = MoveGenerator.GetMoves(position, moveBuffer);
        var moves = moveBuffer[..moveCount]; // for easier iteration, slice the moves down.

        // Early exit if found the TTMove at a greater depth
        var hash = position.Zobrist.HashValue;
        var TTResult = TranspositionTable.Retrieve(hash);
        if (TTResultUsable(alpha, beta, depth, TTResult))
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
            boardState = Referee.GetBoardState(position);

        // no moves, either checkmate or stalemate
        if (moveCount == 0)
        {
            _pvLength[ply] = ply;
            return boardState == BoardStatus.Checkmate
                ? new SearchFlag(true, -(MateScore - ply))
                : new SearchFlag(true, 0); // stalemate
        }

        // reached our bottom depth, go to quiescence search
        if (depth == 0)
        {
            _pvLength[ply] = ply;
            if (boardState == BoardStatus.Checkmate)
                return new SearchFlag(true, -(MateScore - ply));

            return QuiescenceSearch(alpha, beta, position, timed, ply);
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
            if (!Referee.MoveIsLegal(move, position)) continue;
            legalMoveCount++;
            position.ApplyMove(move);
            var child = AlphaBeta(depth - 1, -beta, -alpha, position, timed, ply + 1, false);
            position.UndoMove(move);

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
                endGameScore = -(MateScore - ply);
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
        TranspositionTable.Store(hash, bestValue, depth, _currentSearchId, flag, bestMove, boardState);
        _statistics.TtStores++;

        
        return new SearchFlag(true, bestValue);
    }

    private SearchFlag QuiescenceSearch(int alpha, int beta, Position board, bool timed, int ply)
    {

        // make sure we can handle timing out in quiescence search
        if ((timed && StopwatchManager.ShouldStop) || _ct.IsCancellationRequested)
            return SearchFlag.Abort;

        // seb lague does this - not sure if it's needed though
        var eval = Evaluator.Evaluate(board);
        if (eval >= beta) return new SearchFlag(true, beta);
        if (eval > alpha) alpha = eval;

        // make sure we can tarack the stats
        _statistics.QuiescencePlyReached = ply;
        _statistics.Nodes++;

        // get only capture moves
        Span<Move> moveBuffer = stackalloc Move[128];
        var moveCount = MoveGenerator.GetMoves(board, moveBuffer, true);
        var moves = moveBuffer[..moveCount];

        if (moves.Length > 1)
            Evaluator.SortMoves(moves, null, null, ply);

        foreach (var move in moves)
        {
            board.ApplyMove(move);
            var child = QuiescenceSearch(-beta, -alpha, board, timed, ply + 1);
            board.UndoMove(move);
            if (!child.Completed) return SearchFlag.Abort;
            eval = -child.Value;

            if (eval >= beta) return new SearchFlag(true, beta);
            if (eval > alpha) alpha = eval;
        }
        return new SearchFlag(true, alpha);
    }

    private static bool TTResultUsable(int alpha, int beta, int depth, TranspositionTableEntry? entry)
    {
        // Checks if the transposition table entry can be used to cut off search based on the bounds etc


        if (!entry.HasValue)
            return false;

        var entryValue = entry.Value;

        // only use if depth is sufficient
        if (entryValue.Depth < depth)
            return false;

        switch (entryValue.BoundFlag)
        {
            case BoundFlag.Exact:
                return true;
            case BoundFlag.Upper:
                // we at least know we're better than alpha
                if (entryValue.Eval <= alpha)
                    return true;
                break;
            case BoundFlag.Lower:
                // basically a beta cutoff?
                if (entryValue.Eval >= beta)
                    return true;
                break;
        }
        return false;
    }    

    internal readonly struct SearchFlag(bool completed, int value)
    {
        public bool Completed { get; } = completed;
        public int Value { get; } = value;

        public static SearchFlag Abort => new(false, 0);
    }
}