using Onyx.Statics;

namespace Onyx.Core;

public enum BoundFlag
{
    Exact = 1 << 0,
    Lower = 1 << 1,
    Upper = 1 << 2
}

public struct TtEntry
{
    public ulong Hash;
    public int Eval;
    public int Depth;
    public int Age;
    public BoundFlag BoundFlag;
    public Move BestMove;
    public BoardStatus BoardStatus;

    public static bool TtResultUsable(int alpha, int beta, int depth, TtEntry? entry)
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
}

public class TranspositionTable
{
    private TtEntry[] _entries;


    public TranspositionTable(int sizeInMb = 512)
    {
        var entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TtEntry>();
        var numberOfEntries = sizeInMb * 1024 * 1024 / entrySize;
        _entries = new TtEntry[numberOfEntries];
    }


    public void Store(ulong hash, int eval, int depth, int age, BoundFlag boundFlag, Move bestMove, BoardStatus boardStatus)
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
                BestMove =  bestMove,
                BoardStatus = boardStatus
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