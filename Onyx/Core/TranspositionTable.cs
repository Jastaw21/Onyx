namespace Onyx.Core;

public enum BoundFlag
{
    Exact = 1 << 0,
    Lower = 1 << 1,
    Upper = 1 << 2
}

public struct TranspositionTableEntry
{
    public ulong Hash;
    public int Eval;
    public int Depth;
    public int Age;
    public BoundFlag BoundFlag;
    public Move bestMove;
}

public class TranspositionTable
{
    private TranspositionTableEntry[] _entries;


    public TranspositionTable(int sizeInMb = 512)
    {
        var entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionTableEntry>();
        var numberOfEntries = (sizeInMb * 1024 * 1024) / entrySize;
        _entries = new TranspositionTableEntry[numberOfEntries];
    }


    public void Store(ulong hash, int eval, int depth, int age, BoundFlag boundFlag, Move bestMove)
    {
        var index = hash % (ulong)_entries.Length;

        var existingEntry = _entries[index];
        if (existingEntry.Hash == 0 || existingEntry.Hash == hash || existingEntry.Age != age ||
            depth > existingEntry.Depth)
        {
            _entries[index] = new TranspositionTableEntry
            {
                Hash = hash,
                Eval = eval,
                Depth = depth,
                Age = age,
                BoundFlag = boundFlag,
                bestMove =  bestMove
            };
        }
    }

    public TranspositionTableEntry? Retrieve(ulong hash)
    {
        var index = hash % (ulong)_entries.Length;
        var entry = _entries[index];

        if (entry.Hash == hash)
            return entry;

        return null;
    }
}