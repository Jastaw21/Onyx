using Onyx.UCI;
namespace Onyx.Core;
public class Stopwatch
{
    private System.Diagnostics.Stopwatch _stopwatch = null!;
    private bool _started;
    private long _milliseconds;
    public readonly bool instantStopFlag = false;

    public void Start(long milliseconds)
    {
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _milliseconds = milliseconds;
        _started = true;
    }

    public void Start()
    {
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _started = true;
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _started = false;
        _milliseconds = 0;
    }

    public long Elapsed => _stopwatch.ElapsedMilliseconds;

    public bool ShouldStop
    {
        get
        {
            if (instantStopFlag) return true;
            if (!_started)
                return false;
            return _stopwatch.ElapsedMilliseconds > _milliseconds;
        }
    }
}

public  static class TimeManager
{
    public static int TimeBudgetPerMove(Engine engine,TimeControl timeControl)
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