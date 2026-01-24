using Onyx.Statics;

namespace Onyx.Core;

public struct EvalTableEntry(ulong hash, int eval)
{
    public readonly ulong Hash = hash;
    public int Eval = eval;
}

public class EvaluationTable
{
    public EvaluationTable(int sizeInMb = 512)
    {
        var entrySize = System.Runtime.InteropServices.Marshal.SizeOf<EvalTableEntry>();
        var numberOfEntries = sizeInMb * 1024 * 1024 / entrySize;
        _entries = new EvalTableEntry[numberOfEntries];
    }

    private EvalTableEntry[] _entries;

    protected void Store(ulong hash, int eval)
    {
        var index = hash % (ulong)_entries.Length;
        var store = false;

        // always replace
        _entries[index] = new EvalTableEntry(hash, eval);
    }

    protected EvalTableEntry? Poll(ulong hash)
    {
        var index = hash % (ulong)_entries.Length;
        var entry = _entries[index];

        if (entry.Hash == hash)
            return entry;

        return null;
    }

    public int Evaluate(Position board)
    {
        var existingEval = Poll(board.ZobristState);
        if (existingEval.HasValue)
        {
            return existingEval.Value.Eval;
        }

        var eval = Evaluator.Evaluate(board);
        Store(board.ZobristState, eval);

        return eval;
    }
}