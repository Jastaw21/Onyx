using System.Diagnostics;

namespace Onyx.Core;
public class StopwatchManager
{
    private Stopwatch _stopwatch = null!;
    private bool _started;
    private long _milliseconds;
    public readonly bool InstantStopFlag = false;

    public void Start(long milliseconds)
    {
        _stopwatch = Stopwatch.StartNew();
        _milliseconds = milliseconds;
        _started = true;
    }

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
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
            if (InstantStopFlag) return true;
            if (!_started)
                return false;
            return _stopwatch.ElapsedMilliseconds > _milliseconds;
        }
    }
}