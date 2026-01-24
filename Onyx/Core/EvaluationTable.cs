using Onyx.Statics;

namespace Onyx.Core;

public struct EvalTableEntry(ulong hash, int eval, bool forWhite, int age)
{
    public readonly ulong Hash = hash;
    public int Eval = eval;
    public bool ForWhite = forWhite;
    public int Age = age;
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

    protected void Store(ulong hash, int eval, bool forWhite, int age)
    {
        var index = hash % (ulong)_entries.Length;
        // always replace
        _entries[index] = new EvalTableEntry(hash, eval, forWhite, age);
    }

    protected EvalTableEntry? Poll(ulong hash)
    {
        var index = hash % (ulong)_entries.Length;
        var entry = _entries[index];

        if (entry.Hash == hash)
            return entry;

        return null;
    }

    public int Evaluate(Position board, int age)
    {
        var existingEval = Poll(board.ZobristState);
        if (existingEval.HasValue)
        {
            var flipEval = board.WhiteToMove != existingEval.Value.ForWhite;
            var value = existingEval.Value.Eval * (flipEval ? -1 : 1);
            return value;
        }

        var eval = Evaluator.Evaluate(board);
        var forWhite = board.WhiteToMove;
        Store(board.ZobristState, eval,forWhite, age);

        return eval;
    }
}