using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class EliteTests
    {
        [Test]
        public void AnEnemyTheCheapestWayIntoItsRegionCannotPayForIsAnElite()
        {
            var locked = Elites.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(locked, Is.EqualTo(new[] { LevelSketch.DeepEnemyNodeId }));
        }

        [Test]
        public void AnEnemyTheCheapestArrivalAlreadyAffordsIsNoElite()
        {
            var affordable = LevelSketch.Solvable().Revalued(4, 0, 1).Build();

            Assert.That(Elites.Of(affordable, LevelSketch.Tuning), Is.Empty);
        }

        [Test]
        public void LockednessIsReadOffTheNumberAndNotStoredOnTheEnemy()
        {
            var level = LevelSketch.Solvable().Build();

            Assert.That(level.Decisions.Node(LevelSketch.DeepEnemyNodeId).Type, Is.EqualTo(NodeType.Enemy));
            Assert.That(Elites.Of(level, LevelSketch.Tuning), Does.Contain(LevelSketch.DeepEnemyNodeId));
            Assert.That(
                Elites.Of(LevelSketch.Solvable().Revalued(4, 0, 1).Build(), LevelSketch.Tuning),
                Does.Not.Contain(LevelSketch.DeepEnemyNodeId));
        }

        [Test]
        public void ReadingTheElitesNeedsALevelAndATuning()
        {
            Assert.That(() => Elites.Of(null, LevelSketch.Tuning), Throws.ArgumentNullException);
            Assert.That(() => Elites.Of(LevelSketch.Solvable().Build(), null), Throws.ArgumentNullException);
        }

        [Test]
        public void WalkingIntoAnEliteCostsNothingAndOpensNothing()
        {
            var level = LevelSketch.Branching(additive: 1).Build();

            Assert.That(Elites.Of(level, LevelSketch.Tuning), Does.Contain(LevelSketch.DeepEnemyNodeId));

            var opening = RunState.Begin(level, LevelSketch.Tuning.StartingPower);
            var pastTheGate = ActionResolver.Resolve(opening, LevelSketch.GateEnemyNodeId).State;
            var arrived = ActionResolver.Resolve(pastTheGate, LevelSketch.MultiplierNodeId).State;
            var turnedBack = ActionResolver.Resolve(arrived, LevelSketch.DeepEnemyNodeId);

            Assert.That(turnedBack.Outcome, Is.EqualTo(ActionOutcome.Loss));
            Assert.That(turnedBack.State.Power, Is.EqualTo(arrived.Power), "An Elite charged for the attempt.");
            Assert.That(turnedBack.State.IsConsumed(LevelSketch.DeepEnemyNodeId), Is.False);
            Assert.That(turnedBack.State.PositionNodeId, Is.EqualTo(LevelSketch.MultiplierNodeId));
            Assert.That(
                turnedBack.State.ConsumedNodes.Count,
                Is.EqualTo(arrived.ConsumedNodes.Count),
                "Meeting an Elite opened something.");

            var again = ActionResolver.Resolve(turnedBack.State, LevelSketch.DeepEnemyNodeId);

            Assert.That(again.State.Power, Is.EqualTo(turnedBack.State.Power), "An Elite charged to be met.");
            Assert.That(again.State.IsConsumed(LevelSketch.DeepEnemyNodeId), Is.False);
            Assert.That(
                again.State.ConsumedNodes.Count,
                Is.EqualTo(turnedBack.State.ConsumedNodes.Count),
                "Meeting an Elite spent something else.");
        }

        [Test]
        public void ALevelOnThePlateauPlanAlwaysHoldsALockedDoor()
        {
            var plan = LevelPlan.For(LevelPlan.PlateauLevel);

            for (var seed = 1; seed <= 20; seed++)
            {
                LevelGenerationReport report;
                var level = LevelGenerator.Generate(seed, plan.Preset, plan.Recipe, plan.Tuning, out report);

                Assert.That(Elites.Of(level.Graph, level.Tuning), Is.Not.Empty, "Seed " + seed + " has no doors.");
            }
        }
    }
}
