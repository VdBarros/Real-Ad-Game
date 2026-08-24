using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class AdversaryPanelTests
    {
        [Test]
        public void ThePanelIsSixDistinctPolicies()
        {
            var seen = new List<AdversaryPolicy>(AdversaryPanel.Policies);

            Assert.That(seen.Count, Is.EqualTo(6));
            Assert.That(seen, Is.Unique);
            Assert.That(seen, Contains.Item(AdversaryPolicy.MultiplierFirst));
            Assert.That(seen, Contains.Item(AdversaryPolicy.AdditiveFirst));
            Assert.That(seen, Contains.Item(AdversaryPolicy.EnemyFirst));
            Assert.That(seen, Contains.Item(AdversaryPolicy.BiggestAdditiveFirst));
            Assert.That(seen, Contains.Item(AdversaryPolicy.BiggestMultiplierFirst));
            Assert.That(seen, Contains.Item(AdversaryPolicy.AdditiveLast));
        }

        [Test]
        public void EveryPolicyClearsASolvableLevel()
        {
            var level = LevelSketch.Solvable().Build();

            foreach (var policy in AdversaryPanel.Policies)
            {
                Assert.That(
                    AdversaryPanel.Walk(level, LevelSketch.Tuning, policy),
                    Is.Null,
                    policy + " stranded a level every ordering clears.");
            }

            Assert.That(AdversaryPanel.FirstStall(level, LevelSketch.Tuning), Is.Null);
        }

        [Test]
        public void TheOriginalWorstWalkMissesAStallASiblingCatches()
        {
            var level = LevelSketch.StrandingOnlyTheEnemyFirstPolicy().Build();

            Assert.That(
                AdversaryPanel.Walk(level, LevelSketch.Tuning, AdversaryPolicy.MultiplierFirst),
                Is.Null,
                "The greedy-worst walk was supposed to be silent on this level.");

            var stall = AdversaryPanel.Walk(level, LevelSketch.Tuning, AdversaryPolicy.EnemyFirst);
            Assert.That(stall, Is.Not.Null);
            Assert.That(stall.Policy, Is.EqualTo(AdversaryPolicy.EnemyFirst));
        }

        [Test]
        public void OneStrandingPolicyFailsTheWholePanel()
        {
            var level = LevelSketch.StrandingOnlyTheEnemyFirstPolicy().Build();

            var stall = AdversaryPanel.FirstStall(level, LevelSketch.Tuning);

            Assert.That(stall, Is.Not.Null);
            Assert.That(stall.Policy, Is.EqualTo(AdversaryPolicy.EnemyFirst));
        }

        [Test]
        public void AStallReportCarriesEnoughToReenterTheLevel()
        {
            var level = LevelSketch.StrandingOnlyTheEnemyFirstPolicy().Build();

            var stall = AdversaryPanel.Walk(level, LevelSketch.Tuning, AdversaryPolicy.EnemyFirst);

            Assert.That(stall.Power, Is.EqualTo(26));
            Assert.That(
                stall.Consumed,
                Is.EqualTo(new[]
                {
                    LevelSketch.AdditiveNodeId,
                    LevelSketch.GateEnemyNodeId,
                    LevelSketch.MultiplierNodeId
                }));
            Assert.That(stall.Reachable, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(stall.Stranded.Count, Is.EqualTo(1));
            Assert.That(stall.Stranded[0].NodeId, Is.EqualTo(LevelSketch.DeepEnemyNodeId));
            Assert.That(stall.Stranded[0].Type, Is.EqualTo(NodeType.Enemy));
            Assert.That(stall.Stranded[0].Value, Is.EqualTo(45));
            Assert.That(stall.Stranded[0].Reachable, Is.True);
        }

        [Test]
        public void AStrandedNodeBehindAnUnaffordableEnemyIsReportedOutOfReach()
        {
            var level = LevelSketch.StrandingOnlyTheEnemyFirstPolicy()
                .NodeAt(4, 1, NodeType.Additive, 5)
                .Joined(4, 0, 4, 1)
                .Build();

            var stall = AdversaryPanel.Walk(level, LevelSketch.Tuning, AdversaryPolicy.EnemyFirst);

            Assert.That(stall.Reachable, Does.Not.Contain(6));
            Assert.That(stall.Stranded.Count, Is.EqualTo(2));
            Assert.That(stall.Stranded[0].NodeId, Is.EqualTo(LevelSketch.DeepEnemyNodeId));
            Assert.That(stall.Stranded[0].Reachable, Is.True);
            Assert.That(stall.Stranded[1].NodeId, Is.EqualTo(6));
            Assert.That(stall.Stranded[1].Type, Is.EqualTo(NodeType.Additive));
            Assert.That(stall.Stranded[1].Value, Is.EqualTo(5));
            Assert.That(stall.Stranded[1].Reachable, Is.False);
        }

        [Test]
        public void EveryPolicyStrandsALevelNoOrderingClears()
        {
            var level = LevelSketch.Branching(gateEnemy: 500, boss: 39).Build();

            foreach (var policy in AdversaryPanel.Policies)
            {
                var stall = AdversaryPanel.Walk(level, LevelSketch.Tuning, policy);
                Assert.That(stall, Is.Not.Null, policy + " cleared a level nothing can clear.");
                Assert.That(stall.Power, Is.EqualTo(22));
            }
        }
    }
}
