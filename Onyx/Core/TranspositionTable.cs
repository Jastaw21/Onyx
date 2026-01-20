using Onyx.Statics;

namespace Onyx.Core;

public enum BoundFlag
{
    Exact = 1 << 0,
    Lower = 1 << 1,
    Upper = 1 << 2
}

public struct TTStats : ILoggable
{
    public int Probes = 0;
    public int Collisions = 0;
    public int depthInsufficient = 0;
    public int passUpperBound = 0;
    public int failUpperBound = 0;
    public int passLowerBound = 0;
    public int failLowerBound = 0;
    public int passExact = 0;


    public string Get()
    {
        return
            $"Probes {Probes}, " +
            $"Collisions {Pct(Collisions)}, " +
            $"DepthInsufficient {Pct(depthInsufficient)}, " +
            $"PassUB {Pct(passUpperBound)}, " +
            $"FailUB {Pct(failUpperBound)}, " +
            $"PassLB {Pct(passLowerBound)}, " +
            $"FailLB {Pct(failLowerBound)}, " +
            $"Exact {Pct(passExact)}";
    }

    private string Pct(int value)
    {
        double pct = (double)value / Math.Max(1, Probes) * 100;
        return $"{value} ({pct:F1}%)";
    }

}
public struct TtEntry
{
    public ulong Hash;
    public int Eval;
    public int Depth;
    public int Age;
    public BoundFlag BoundFlag;
    public Move BestMove;

    public static TtEntry InvalidEntry => new() { Hash = 0, Eval = 0, Depth = 0, Age = 0, BestMove = default, BoundFlag = default };
}

public class TranspositionTable
{
    public bool PollEntry(TtEntry entry, int alpha, int beta, int depth, ulong hash)
    {
        tTStats.Probes++;
        if (entry.Hash != hash)
        {
            tTStats.Collisions++;
            return false;
        }

        // only use if depth is sufficient
        if (Depth < depth)
        {
            tTStats.depthInsufficient++;
            return false;
        }

        switch (BoundFlag)
        {
            case BoundFlag.Exact:
                {
                    tTStats.passExact++;
                    return true;
                }
            case BoundFlag.Upper:
                // we at least know we're better than alpha
                if (Eval <= alpha)
                {
                    tTStats.passUpperBound++;
                    return true;
                }
                tTStats.failUpperBound++;
                break;
            case BoundFlag.Lower:
                // basically a beta cutoff?
                if (Eval >= beta)
                {
                    tTStats.passLowerBound;
                    return true;
                }
                tTStats.failLowerBound++;
                break;
        }
        return false;

    }
    private TtEntry[] _entries;

    public TTStats tTStats = new();

    public TranspositionTable(int sizeInMb = 512)
    {
        var entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TtEntry>();
        var numberOfEntries = sizeInMb * 1024 * 1024 / entrySize;
        _entries = new TtEntry[numberOfEntries];
    }


    public void Store(ulong hash, int eval, int depth, int age, BoundFlag boundFlag, Move bestMove)
    {
        var index = hash % (ulong)_entries.Length;

        var existingEntry = _entries[index];
        if (existingEntry.Hash == 0 || existingEntry.Hash == hash || existingEntry.Age != age ||
            depth > existingEntry.Depth)
        {
            _entries[index] = new TtEntry
            {
                Hash = hash,
                Eval = eval,
                Depth = depth,
                Age = age,
                BoundFlag = boundFlag,
                BestMove = bestMove
            };
        }
    }

    public TtEntry? Retrieve(ulong hash)
    {
        var index = hash % (ulong)_entries.Length;
        var entry = _entries[index];

        if (entry.Hash == hash)
            return entry;

        return null;
    }
}