using Onyx.Statics;

namespace Onyx.Core;

public struct EvalTableEntry(ulong hash, int eval, bool forWhite, int age)
{
    public readonly ulong Hash = hash;
    public int Eval = eval;
    public bool ForWhite = forWhite;
    public int Age = age;
}

internal struct EvalTableStats
{
    public int Stores = 0;
    public int Replacements = 0;
    public int Empties = 0;

    public int AttemptedRetrieves = 0;
    public int Hit = 0;

    public string Get()
    {
        return $"Eval Stores: {Stores}, " +
               $"Rep {Replacements}, {Empties}, " +
               $"Polls {AttemptedRetrieves} hit {Hit}, re-eval {AttemptedRetrieves - Hit}";
    }

    public EvalTableStats()
    {
    }
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
    private EvalTableStats stats;

    public void WriteStats()
    {
        Console.Error.WriteLine(stats.Get());
    }
    protected void Store(ulong hash, int eval, bool forWhite, int age)
    {
        stats.Stores++;
        var index = hash % (ulong)_entries.Length;
        var existingEntry = _entries[index];
        if (existingEntry.Hash == 0) stats.Empties++;
        else stats.Replacements++;
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
        stats.AttemptedRetrieves++;
        var existingEval = Poll(board.ZobristState);
        if (existingEval.HasValue)
        {
            
            var flipEval = board.WhiteToMove != existingEval.Value.ForWhite;
            var value = existingEval.Value.Eval * (flipEval ? -1 : 1);
            stats.Hit++;
            return value;
        }

        
        var eval = Evaluator.Evaluate(board);
        var forWhite = board.WhiteToMove;
        Store(board.ZobristState, eval, forWhite, age);

        return eval;
    }
}