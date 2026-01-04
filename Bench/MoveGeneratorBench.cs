using BenchmarkDotNet.Attributes;
using Onyx.Core;
using Onyx.Statics;
using System;
using Microsoft.VSDiagnostics;

namespace Onyx.Benchmarks
{
    [CPUUsageDiagnoser]
    public class MoveGeneratorBench
    {
        private Board board;
        private Move[] moves;
        [GlobalSetup]
        public void Setup()
        {
            board = new Board();
            moves = new Move[256];
        }

        [Benchmark]
        public int GetMoves()
        {
            Span<Move> span = moves;
            return MoveGenerator.GetMoves(board, span);
        }
    }
}