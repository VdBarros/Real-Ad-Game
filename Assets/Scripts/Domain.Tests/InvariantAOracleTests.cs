using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class InvariantAOracleTests
    {
        static List<int> Affordable(RunState state)
        {
            var moves = new List<int>();

            foreach (var nodeId in state.ReachableNodes)
            {
                if (state.IsConsumed(nodeId))
                {
                    continue;
                }

                var node = state.Level.Decisions.Node(nodeId);

                switch (node.Type)
                {
                    case NodeType.Additive:
                    case NodeType.Multiplier:
                        moves.Add(nodeId);
                        break;

                    case NodeType.Enemy:
                    case NodeType.Boss:
                        if (state.Power > node.Value)
                        {
                            moves.Add(nodeId);
                        }

                        break;
                }
            }

            return moves;
        }

        [Test]
        public void ALevelEveryOrderingFinishesCarriesNoStall()
        {
            var verdict = InvariantAOracle.Sweep(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(verdict.Aborted, Is.False);
            Assert.That(verdict.Stalled, Is.False, verdict.ToString());
            Assert.That(verdict.IsSafe, Is.True);
        }

        [Test]
        public void TheOracleFindsAStallOnlyOneOrderingWalksInto()
        {
            var level = LevelSketch.StrandingOnlyTheEnemyFirstPolicy().Build();

            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);

            Assert.That(
                AdversaryPanel.Walk(level, LevelSketch.Tuning, AdversaryPolicy.MultiplierFirst),
                Is.Null,
                "The greedy-worst walk was supposed to be silent on this level.");
            Assert.That(verdict.Stalled, Is.True);
            Assert.That(verdict.FirstStall.Power, Is.EqualTo(26));
            Assert.That(verdict.FirstStall.Consumed, Is.EqualTo(new[]
            {
                LevelSketch.AdditiveNodeId,
                LevelSketch.GateEnemyNodeId,
                LevelSketch.MultiplierNodeId
            }));
            Assert.That(verdict.FirstStall.Stranded.Count, Is.EqualTo(1));
            Assert.That(verdict.FirstStall.Stranded[0].NodeId, Is.EqualTo(LevelSketch.DeepEnemyNodeId));
            Assert.That(verdict.FirstStall.Stranded[0].Reachable, Is.True);
        }

        [Test]
        public void TheOracleFindsTheStallNoOrderingEscapes()
        {
            var level = LevelSketch.Branching(gateEnemy: 500, boss: 39).Build();

            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);

            Assert.That(verdict.Stalls, Is.EqualTo(1));
            Assert.That(verdict.FirstStall.Power, Is.EqualTo(22));
            Assert.That(verdict.FirstStall.Consumed, Is.EqualTo(new[] { LevelSketch.AdditiveNodeId }));
        }

        [Test]
        public void ATieIsNotProgress()
        {
            var level = new LevelSketch()
                .NodeAt(0, 0, NodeType.Start)
                .NodeAt(1, 0, NodeType.Enemy, LevelSketch.Tuning.StartingPower)
                .NodeAt(2, 0, NodeType.Additive, 5)
                .NodeAt(0, 1, NodeType.Boss, 39)
                .Joined(0, 0, 1, 0)
                .Joined(1, 0, 2, 0)
                .Joined(0, 0, 0, 1)
                .Build();

            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);

            Assert.That(verdict.Stalled, Is.True);
            Assert.That(verdict.FirstStall.Power, Is.EqualTo(LevelSketch.Tuning.StartingPower));
            Assert.That(verdict.FirstStall.Consumed, Is.Empty);
            Assert.That(verdict.FirstStall.Stranded.Count, Is.EqualTo(2));
        }

        [Test]
        public void NothingIsWonBySteppingThroughTheBoss()
        {
            var level = LevelSketch.Solvable()
                .NodeAt(0, 2, NodeType.Additive, 5)
                .Joined(0, 1, 0, 2)
                .Build();

            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);

            Assert.That(verdict.Stalled, Is.True);
            Assert.That(verdict.FirstStall.Stranded.Count, Is.EqualTo(1));
            Assert.That(verdict.FirstStall.Stranded[0].Type, Is.EqualTo(NodeType.Additive));
            Assert.That(verdict.FirstStall.Stranded[0].Reachable, Is.False);
        }

        [Test]
        public void ADrainedStateIsNotOneTheOracleExplores()
        {
            var level = LevelSketch.Solvable().Build();
            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);
            var full = RunState.Begin(level, LevelSketch.Tuning.StartingPower);
            var floored = full.Drained(Drain.Floor);

            Assert.That(verdict.Stalled, Is.False);
            Assert.That(floored.ConsumedNodes, Is.EqualTo(full.ConsumedNodes));
            Assert.That(floored.ReachableNodes, Is.EqualTo(full.ReachableNodes));
            Assert.That(Affordable(full), Has.Member(LevelSketch.GateEnemyNodeId));
            Assert.That(Affordable(floored), Has.No.Member(LevelSketch.GateEnemyNodeId));
        }

        [Test]
        public void InvariantAIsQuantifiedOverProgressAndADrainMakesNone()
        {
            var level = new LevelSketch()
                .NodeAt(0, 0, NodeType.Start)
                .NodeAt(1, 0, NodeType.Enemy, 1)
                .NodeAt(2, 0, NodeType.Additive, 20)
                .NodeAt(0, 1, NodeType.Boss, 21)
                .Joined(0, 0, 1, 0)
                .Joined(1, 0, 2, 0)
                .Joined(0, 0, 0, 1)
                .Build();

            var verdict = InvariantAOracle.Sweep(level, LevelSketch.Tuning);
            var full = RunState.Begin(level, LevelSketch.Tuning.StartingPower);

            Assert.That(verdict.Stalled, Is.False, verdict.ToString());
            Assert.That(Affordable(full), Is.Not.Empty);
            Assert.That(
                Affordable(full.Drained(Drain.Floor)),
                Is.Empty,
                "A run that throws its power away can walk itself into a corner Invariant A never "
                + "promised it out of: the oracle quantifies over orderings of moves that consume, "
                + "and a drain consumes nothing, so it adds no ordering and no state to the sweep.");
        }

        [Test]
        public void ABlownBudgetIsNotAVerdict()
        {
            var verdict = InvariantAOracle.Sweep(LevelSketch.Solvable().Build(), LevelSketch.Tuning, stateBudget: 2);

            Assert.That(verdict.Aborted, Is.True);
            Assert.That(verdict.IsSafe, Is.False);
            Assert.That(verdict.PeakStates, Is.GreaterThan(0));
        }

        [Test]
        public void TheOracleReportsWhatItExplored()
        {
            var verdict = InvariantAOracle.Sweep(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(verdict.PeakStates, Is.GreaterThan(1));
            Assert.That(verdict.ExploredStates, Is.GreaterThan(1));
            Assert.That(verdict.PeakStates, Is.LessThanOrEqualTo(InvariantAOracle.DefaultStateBudget));
        }
    }
}
