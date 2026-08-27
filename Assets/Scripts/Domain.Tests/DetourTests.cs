using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class DetourTests
    {
        [Test]
        public void ASlotStandingBetweenTheStartAndAnotherSlotIsNoDetour()
        {
            var detours = Detours.Of(LevelSketch.Solvable().Build());

            Assert.That(detours.Holds(LevelSketch.AdditiveNodeId), Is.False);
            Assert.That(detours.Holds(LevelSketch.GateEnemyNodeId), Is.False);
            Assert.That(detours.Holds(LevelSketch.MultiplierNodeId), Is.False);
        }

        [Test]
        public void ADeadEndWithNothingBehindItIsADetour()
        {
            var detours = Detours.Of(LevelSketch.Solvable().Build());

            Assert.That(detours.Holds(LevelSketch.DeepEnemyNodeId), Is.True);
            Assert.That(detours.Count, Is.EqualTo(1));
        }

        [Test]
        public void ASlotOnTheRouteToTheBossIsNoDetourEvenWithNothingBehindIt()
        {
            var level = LoopedAroundTheBoss();
            var detours = Detours.Of(level);

            Assert.That(detours.Holds(IdAt(level, 1, 0)), Is.False);
        }

        [Test]
        public void ASlotOffTheRouteToTheBossWithAWayAroundItIsADetour()
        {
            var level = LoopedAroundTheBoss();
            var detours = Detours.Of(level);

            Assert.That(detours.Holds(IdAt(level, 1, 1)), Is.True);
        }

        [Test]
        public void TheBossIsNeverADetourOfItsOwn()
        {
            var detours = Detours.Of(LevelSketch.Solvable().Build());

            Assert.That(detours.Holds(LevelSketch.BossNodeId), Is.False);
            Assert.That(detours.NodeIds, Has.No.Member(LevelSketch.BossNodeId));
        }

        [Test]
        public void EverySlotADetourListsIsOneItHolds()
        {
            var detours = Detours.Of(LevelSketch.Solvable().Build());

            foreach (var nodeId in detours.NodeIds)
            {
                Assert.That(detours.Holds(nodeId), Is.True);
            }

            Assert.That(detours.Count, Is.EqualTo(detours.NodeIds.Count));
        }

        [Test]
        public void ALevelWithNoBossHasNoRouteToBeOff()
        {
            var bossless = LevelSketch.Solvable().Retyped(0, 1, NodeType.Additive).Build();

            Assert.That(() => Detours.Of(bossless), Throws.ArgumentException);
        }

        [Test]
        public void ADetourIsAskedOfSomething()
        {
            Assert.That(() => Detours.Of((LevelGraph)null), Throws.ArgumentNullException);
            Assert.That(() => Detours.Of((MazeLayout)null), Throws.ArgumentNullException);
            Assert.That(() => Detours.DeepestSlotOf(null), Throws.ArgumentNullException);
        }

        static LevelGraph LoopedAroundTheBoss()
        {
            return new LevelSketch()
                .NodeAt(0, 0, NodeType.Start)
                .NodeAt(1, 0, NodeType.Enemy, 1)
                .NodeAt(2, 0, NodeType.Boss, 9)
                .NodeAt(0, 1, NodeType.Empty)
                .NodeAt(1, 1, NodeType.Additive, 3)
                .NodeAt(2, 1, NodeType.Empty)
                .Joined(0, 0, 1, 0)
                .Joined(1, 0, 2, 0)
                .Joined(0, 0, 0, 1)
                .Joined(0, 1, 1, 1)
                .Joined(1, 1, 2, 1)
                .Joined(2, 1, 2, 0)
                .Build();
        }

        static int IdAt(LevelGraph level, int x, int y)
        {
            return level.Decisions.NodeAt(new TilePosition(0, x, y)).Id;
        }
    }
}
