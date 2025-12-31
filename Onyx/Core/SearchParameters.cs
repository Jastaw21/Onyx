using Onyx.Statics;

public struct SearchStatistics : ILoggable
{
    public int Nodes;
    public int TtHits;
    public int TtStores;
    public int BetaCutoffs;
    public long RunTime;
    public int Depth;

    public string Get()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)}";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)}";
    }
}

public readonly struct SearchResult
{
    public bool Completed { get; }
    public int Value { get; }

    public SearchResult(bool completed, int value)
    {
        Completed = completed;
        Value = value;
    }

    public static SearchResult Abort => new(false, 0);
}