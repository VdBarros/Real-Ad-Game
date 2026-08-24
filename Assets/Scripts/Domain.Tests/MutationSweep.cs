using System;
using System.Collections.Generic;

namespace Game.Domain.Tests
{
    sealed class Mutant
    {
        Mutant(PlacedLevel source, int nodeId, int factor, LevelGraph graph)
        {
            Source = source;
            NodeId = nodeId;
            Factor = factor;
            Graph = graph;
        }

        public static Mutant Of(PlacedLevel source, int nodeId, int factor)
        {
            return new Mutant(source, nodeId, factor, LevelMutation.WithNodeInflated(source.Graph, nodeId, factor));
        }

        public PlacedLevel Source { get; }

        public int NodeId { get; }

        public int Factor { get; }

        public LevelGraph Graph { get; }

        public PowerTuning Tuning
        {
            get { return Source.Tuning; }
        }

        public override string ToString()
        {
            return "Seed " + Source.AttemptSeed + ", enemy #" + NodeId + " inflated " + Factor + "-fold";
        }
    }

    sealed class MutationSweep
    {
        readonly List<int> peaks = new List<int>();
        readonly List<string> disagreements = new List<string>();

        public MutationSweep(int levels)
        {
            Levels = levels;
        }

        public int Levels { get; }

        public int Cases { get; private set; }

        public int OracleStalls { get; private set; }

        public int PanelStalls { get; private set; }

        public int Misses { get; private set; }

        public int FalseAlarms { get; private set; }

        public int Aborts { get; private set; }

        public int CrossExamined
        {
            get { return Cases - Aborts; }
        }

        public double MissRate
        {
            get { return OracleStalls == 0 ? 1.0 : (double)Misses / OracleStalls; }
        }

        public IReadOnlyList<string> Disagreements
        {
            get { return disagreements; }
        }

        public string Report
        {
            get { return this + Environment.NewLine + string.Join(Environment.NewLine, disagreements); }
        }

        public void Compared(Mutant mutant, OracleVerdict oracle, StallReport panel)
        {
            Cases++;
            peaks.Add(oracle.PeakStates);

            if (oracle.Aborted)
            {
                Aborts++;
                return;
            }

            if (oracle.Stalled)
            {
                OracleStalls++;
            }

            if (panel != null)
            {
                PanelStalls++;
            }

            if (oracle.Stalled == (panel != null))
            {
                return;
            }

            if (oracle.Stalled)
            {
                Misses++;
            }
            else
            {
                FalseAlarms++;
            }

            disagreements.Add(
                mutant + ": oracle says " + (oracle.Stalled ? "stall" : "safe") + ", panel says "
                + (panel != null ? "stall" : "safe") + " — " + oracle);
        }

        public override string ToString()
        {
            var sorted = new List<int>(peaks);
            sorted.Sort();

            return "tiny mutants: " + Levels + " levels, " + Cases + " cases, "
                + CrossExamined + " cross-examined, oracle stalls " + OracleStalls
                + ", panel stalls " + PanelStalls + ", panel missed " + Misses
                + ", panel false-alarmed " + FalseAlarms
                + ", peak states p50 " + SweepStatistics.Percentile(sorted, 0.5)
                + " p90 " + SweepStatistics.Percentile(sorted, 0.9)
                + " max " + sorted[sorted.Count - 1];
        }
    }
}
