using Onyx.Statics;
using Onyx.UCI;

namespace Onyx.Core;

public struct SearchStatistics : ILoggable
{
    public int Nodes;
    public int TtHits;
    public int TtStores;
    public int BetaCutoffs;
    public long RunTime;
    public int Depth;
    public int NullMoveReductions;
    public int QuiescencePlyReached;


    public string Get()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)} nmr {NullMoveReductions} qui {QuiescencePlyReached}";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, TTTable hits {TtHits}, TTStores {TtStores}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)} nmr {NullMoveReductions} qui {QuiescencePlyReached}";
    }
}


public struct SearchParameters
{
    public int? MaxDepth;
    public long? TimeLimit;
    public TimeControl? TimeControl;
    public CancellationToken CancellationToken;
}

public struct SearchResults
{
    public Move BestMove;
    public int Score;
    public SearchStatistics Statistics;
    public List<Move> PV;
}