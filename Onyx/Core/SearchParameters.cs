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
    public int NullMoveCutoffs;
    public int qNodes;
    public int ReducedSearches;
    public int FullResearches;
    public int FirstMoveCutoffs; // first move cutoffs
    public int FailedNullMoveCutoffs;
    
    public int DeltaCutoffs;
    public int DeltaPerMove;
    public int RFPCutoffs;


    public void WriteStats()
    {
        Console.Error.WriteLine(Get());
    }
    
    public string Get()
    {
        var runtimeMs = Math.Max(RunTime, 2);
        var nps = Nodes / (float)(runtimeMs / 1000.0);
        var ebf = Depth > 0 ? Math.Pow(Nodes, 1.0 / Depth) : 0.0;

        string[] parts =
        [
            $"Depth: {Depth}",
            $"Nodes Searched: {Nodes}",
            $"Time (ms): {RunTime}",
            $"NPS: {nps}",
            $"HashCutoffs: {HashCutoffs}",
            $"BetaCutoffs: {BetaCutoffs}",
            $"ebf: {ebf}",
            $"Null Move Comp: {NullMoveCutoffs}",
            $"Failed Null Move: {FailedNullMoveCutoffs}",
            $"qNodes: {qNodes}",
            $"reduced: {ReducedSearches}",
            $"full: {FullResearches}",
            $"fmc: {FirstMoveCutoffs}",
            $"delta: {DeltaCutoffs}",
            $"delta/move: {DeltaPerMove}",
            $"rfpc: {RFPCutoffs}"
        ];

        return string.Join("\n", parts);
    }

    public override string ToString()
    {
        return
            Get();
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