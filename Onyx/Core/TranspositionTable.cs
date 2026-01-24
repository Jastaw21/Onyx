using Onyx.Statics;
using System.Runtime.CompilerServices;

namespace Onyx.Core;

public enum BoundFlag
{
    Exact = 1 << 0,
    Lower = 1 << 1,
    Upper = 1 << 2
}

public struct TtStats : ILoggable
{
    // retrieval
    public int Probes = 0;
    public int Collisions = 0;
    public int DepthInsufficient = 0;
    public int PassUpperBound = 0;
    public int FailUpperBound = 0;
    public int PassLowerBound = 0;
    public int FailLowerBound = 0;
    public int PassExact = 0;

    public int AgeReplacements = 0;
    public int AttemptedStores = 0;
    public int Empties = 0;
    public int DepthReplacements;
    public int NonReplacements;

    public TtStats()
    {
    }


    public string Get()
    {
        return
            $"Probes {Probes}, " +
            $"Collisions {PctRetrieve(Collisions)}, " +
            $"DepthInsufficient {PctRetrieve(DepthInsufficient)}, " +
            $"PassUB {PctRetrieve(PassUpperBound)}, " +
            $"FailUB {PctRetrieve(FailUpperBound)}, " +
            $"PassLB {PctRetrieve(PassLowerBound)}, " +
            $"FailLB {PctRetrieve(FailLowerBound)}, " +
            $"Exact {PctRetrieve(PassExact)}\n" +
            $"Attempted Stores {AttemptedStores}," +
            $"Age Repl {AgeReplacements} {PctStore(AgeReplacements)}, " +
            $"Empties {Empties} {PctStore(Empties)}, " +
            $"Depth Repl {DepthReplacements} {PctStore(DepthReplacements)}, " +
            $"Non Repl {NonReplacements} {PctStore(NonReplacements)}";
    }

    private string PctRetrieve(int value)
    {
        double pct = (double)value / Math.Max(1, Probes) * 100;
        return $"{value} ({pct:F1}%)";
    }

    private string PctStore(int value)
    {
        double pct = (double)value / Math.Max(1, AttemptedStores) * 100;
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

    public static TtEntry InvalidEntry => new()
        { Hash = 0, Eval = 0, Depth = 0, Age = 0, BestMove = default, BoundFlag = default };
}

public class TranspositionTable
{
    public bool PollEntry(TtEntry entry, int alpha, int beta, int depth, ulong hash)
    {
        TTStats.Probes++;
        if (entry.Hash != hash)
        {
            TTStats.Collisions++;
            return false;
        }

        // only use if depth is sufficient
        if (entry.Depth < depth)
        {
            TTStats.DepthInsufficient++;
            return false;
        }

        switch (entry.BoundFlag)
        {
            case BoundFlag.Exact:
            {
                TTStats.PassExact++;
                return true;
            }
            case BoundFlag.Upper:
                // we at least know we're better than alpha
                if (entry.Eval <= alpha)
                {
                    TTStats.PassUpperBound++;
                    return true;
                }

                TTStats.FailUpperBound++;
                break;
            case BoundFlag.Lower:
                // basically a beta cutoff?
                if (entry.Eval >= beta)
                {
                    TTStats.PassLowerBound++;
                    return true;
                }

                TTStats.FailLowerBound++;
                break;
        }

        return false;
    }

    private TtEntry[] _entries;
    private ulong _indexMask;
    public TtStats TTStats = new();

    public TranspositionTable(int sizeInMb = 512)
    {
        var entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TtEntry>();
        var numberOfEntries = sizeInMb * 1024 * 1024 / entrySize; // raw number of entries
        
        // next power of two
        var v = numberOfEntries;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        _entries = new TtEntry[v];
        _indexMask = (ulong)v - 1UL;
    }


    public void Store(ulong hash, int eval, int depth, int age, BoundFlag boundFlag, Move bestMove)
    {
        // faster modulo via bitmask
        var index = (int)(hash & _indexMask);
        TTStats.AttemptedStores++;
        var existingEntry = _entries[index];

        var store = false;

        // always replace empties
        if (existingEntry.Hash == 0)
        {
            store = true;
            TTStats.Empties++;
        }

        // if we already have this hash needs to think about a replacement scheme
        else if (existingEntry.Hash == hash)
        {
            if (depth > existingEntry.Depth)
            {
                store = true;
                TTStats.DepthReplacements++;
            }
        }
        
        if (existingEntry.Age != age)
        {
            store = true;
            TTStats.AgeReplacements++;
        }

        if (store)
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
            return;
        }

        TTStats.NonReplacements++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRetrieve(ulong hash, out TtEntry entry)
    {
        var index = (int)(hash & _indexMask);
        entry = _entries[index];
        return entry.Hash == hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TtEntry? Retrieve(ulong hash)
    {
        if (TryRetrieve(hash, out var entry))
            return entry;
        return null;
    }
}