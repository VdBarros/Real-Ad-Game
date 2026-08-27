using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SpineTests
    {
        [Test]
        public void TheSpineStopsTheMomentTheBossBecomesAffordable()
        {
            var spine = Spine.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(spine.ReachesTheBoss, Is.True);
            Assert.That(
                spine.NodeIds,
                Is.EqualTo(new[]
                {
                    LevelSketch.AdditiveNodeId, LevelSketch.GateEnemyNodeId, LevelSketch.MultiplierNodeId
                }));
        }

        [Test]
        public void WhatTheSpineNeverReachedIsOffIt()
        {
            var spine = Spine.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(spine.Holds(LevelSketch.MultiplierNodeId), Is.True);
            Assert.That(spine.Holds(LevelSketch.DeepEnemyNodeId), Is.False);
            Assert.That(spine.ArrivalPowerOn(LevelSketch.DeepEnemyNodeId), Is.EqualTo(-1));
        }

        [Test]
        public void EveryEnemyOnTheSpineIsAffordableWhenTheSpineReachesIt()
        {
            var level = LevelSketch.Solvable().Build();
            var spine = Spine.Of(level, LevelSketch.Tuning);

            for (var index = 0; index < spine.Length; index++)
            {
                var node = level.Decisions.Node(spine.NodeIds[index]);
                if (node.Type != NodeType.Enemy)
                {
                    continue;
                }

                Assert.That(
                    spine.ArrivalPowerAt(index),
                    Is.GreaterThan(node.Value),
                    "The Spine reached enemy #" + node.Id + " unable to pay for it.");
            }
        }

        [Test]
        public void TheSpineCarriesThePowerItArrivedOnEveryNodeWith()
        {
            var spine = Spine.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(spine.ArrivalPowerAt(0), Is.EqualTo(LevelSketch.Tuning.StartingPower));
            Assert.That(spine.ArrivalPowerOn(LevelSketch.AdditiveNodeId), Is.EqualTo(2));
            Assert.That(spine.ArrivalPowerOn(LevelSketch.GateEnemyNodeId), Is.EqualTo(22));
            Assert.That(spine.ArrivalPowerOn(LevelSketch.MultiplierNodeId), Is.EqualTo(23));
        }

        [Test]
        public void ASpineThatRunsOutOfAffordableContentStopsShortOfTheBoss()
        {
            var spine = Spine.Of(LevelSketch.Branching(boss: 1000).Build(), LevelSketch.Tuning);

            Assert.That(spine.ReachesTheBoss, Is.False);
            Assert.That(spine.Holds(LevelSketch.DeepEnemyNodeId), Is.True);
        }

        [Test]
        public void ASpineWalksNoFurtherThanTheFirstEnemyItCannotPayFor()
        {
            var spine = Spine.Of(
                LevelSketch.Branching(additive: 1, gateEnemy: 9, boss: 1000).Build(), LevelSketch.Tuning);

            Assert.That(spine.ReachesTheBoss, Is.False);
            Assert.That(spine.NodeIds, Is.EqualTo(new[] { LevelSketch.AdditiveNodeId }));
        }

        [Test]
        public void ASpineNeedsALevelAndATuningToWalk()
        {
            Assert.That(() => Spine.Of(null, LevelSketch.Tuning), Throws.ArgumentNullException);
            Assert.That(() => Spine.Of(LevelSketch.Solvable().Build(), null), Throws.ArgumentNullException);
        }

        [Test]
        public void ALevelWithNoBossHasNoMomentToTruncateAt()
        {
            var bossless = LevelSketch.Solvable().Retyped(0, 1, NodeType.Additive).Build();

            Assert.That(() => Spine.Of(bossless, LevelSketch.Tuning), Throws.ArgumentException);
        }
    }
}
