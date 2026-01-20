using Onyx.Statics;
using Onyx.UCI;

namespace Onyx.Core;

public struct SearchStatistics : ILoggable
{
    public int Nodes;
    public int HashCutoffs;
    public int BetaCutoffs;
    public long RunTime;
    public int Depth;
    public int NullMoveReductions;
    public int qNodes;
    public int ReducedSearches;
    public int FullResearches;
    public int FirstMoveCutoffs; // first move cutoffs


    public string Get()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, HashCutoffs {HashCutoffs}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)} nmr {NullMoveReductions} qui {qNodes} reduced {ReducedSearches} full {FullResearches} fmc {FirstMoveCutoffs}";
    }

    public override string ToString()
    {
        return
            $"Depth: {Depth}, Nodes Searched: {Nodes}, Time (ms): {RunTime}, NPS {Nodes / (float)(Math.Max(RunTime, 2) / 1000.0)}, HashCutoffs {HashCutoffs}, BetaCutoffs {BetaCutoffs}, ebf {Math.Pow(Nodes, 1.0 / Depth)} nmr {NullMoveReductions} qui {qNodes} reduced {ReducedSearches} full {FullResearches} fmc {FirstMoveCutoffs}";
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
    public List<Move> Pv;
}