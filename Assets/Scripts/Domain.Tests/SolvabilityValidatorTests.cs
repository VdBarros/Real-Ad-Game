using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SolvabilityValidatorTests
    {
        [Test]
        public void ASolvableLevelIsSafeAndSaysNothingElse()
        {
            var verdict = SolvabilityValidator.Validate(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(verdict.IsSafe, Is.True, verdict.ToString());
            Assert.That(verdict.Reason, Is.EqualTo(SolvabilityReason.None));
            Assert.That(verdict.Stall, Is.Null);
            Assert.That(verdict.BossNodeId, Is.EqualTo(LevelSketch.BossNodeId));
            Assert.That(verdict.BossPower, Is.EqualTo(39));
            Assert.That(verdict.Bound, Is.EqualTo(49));
            Assert.That(verdict.BeelinePower, Is.EqualTo(LevelSketch.Tuning.StartingPower));
        }

        [Test]
        public void AGraphWithNoStartIsRejected()
        {
            Expect(
                LevelSketch.Solvable().Retyped(0, 0, NodeType.Empty),
                SolvabilityReason.NoStart);
        }

        [Test]
        public void AGraphWithTwoStartsIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(1, 0, NodeType.Start),
                SolvabilityReason.ManyStarts);
        }

        [Test]
        public void AGraphWithNoBossIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(0, 1, NodeType.Empty),
                SolvabilityReason.NoBoss);
        }

        [Test]
        public void AGraphWithTwoBossesIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(1, 0, NodeType.Boss, 5),
                SolvabilityReason.ManyBosses);
        }

        [Test]
        public void AnUnassignedSlotIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(1, 0, NodeType.Unassigned),
                SolvabilityReason.NodeUnassigned,
                LevelSketch.AdditiveNodeId);
        }

        [Test]
        public void AMultiplierThatMultipliesByOneIsRejected()
        {
            Expect(
                LevelSketch.Solvable().Revalued(3, 0, 1),
                SolvabilityReason.ValueOutOfRange,
                LevelSketch.MultiplierNodeId);
        }

        [Test]
        public void AnEnemyWorthNothingIsRejected()
        {
            Expect(
                LevelSketch.Solvable().Revalued(2, 0, 0),
                SolvabilityReason.ValueOutOfRange,
                LevelSketch.GateEnemyNodeId);
        }

        [Test]
        public void AStartCarryingPowerIsRejected()
        {
            Expect(
                LevelSketch.Solvable().Revalued(0, 0, 7),
                SolvabilityReason.ValueOutOfRange,
                LevelSketch.StartNodeId);
        }

        [Test]
        public void ANodeNoCorridorLeadsToIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(7, 7, NodeType.Empty),
                SolvabilityReason.NodeUnreachable,
                6);
        }

        [Test]
        public void ContentGatedBehindTheBossIsRejected()
        {
            Expect(
                LevelSketch.Solvable().NodeAt(0, 2, NodeType.Additive, 5).Joined(0, 1, 0, 2),
                SolvabilityReason.GatedBehindBoss,
                6);
        }

        [Test]
        public void ABossAboveTheBoundIsRejected()
        {
            Expect(LevelSketch.Branching(boss: 500), SolvabilityReason.BossBeyondBound);
        }

        [Test]
        public void ABossABeelineAlreadyBeatsIsRejected()
        {
            Expect(LevelSketch.Branching(boss: 2), SolvabilityReason.BossWithinReach);
        }

        [Test]
        public void ALevelOnePolicyStrandsIsRejectedWithItsStallReport()
        {
            var verdict = SolvabilityValidator.Validate(
                LevelSketch.StrandingOnlyTheEnemyFirstPolicy().Build(), LevelSketch.Tuning);

            Assert.That(verdict.Reason, Is.EqualTo(SolvabilityReason.AdversaryStalled));
            Assert.That(verdict.Stall, Is.Not.Null);
            Assert.That(verdict.Stall.Policy, Is.EqualTo(AdversaryPolicy.EnemyFirst));
            Assert.That(verdict.ToString(), Does.Contain("EnemyFirst"));
        }

        [Test]
        public void InvariantAIsWalkedEvenWhenInvariantBHasAlreadyFailed()
        {
            var verdict = SolvabilityValidator.Validate(
                LevelSketch.Branching(gateEnemy: 500, boss: 600).Build(), LevelSketch.Tuning);

            Assert.That(verdict.Reason, Is.EqualTo(SolvabilityReason.BossBeyondBound));
            Assert.That(
                verdict.Stall,
                Is.Not.Null,
                "The panel was skipped because another invariant had already failed.");
            Assert.That(verdict.Stall.Power, Is.EqualTo(22));
        }

        [Test]
        public void InvariantBReadsAGraphNoBoardCouldBeBuiltFrom()
        {
            var headless = LevelSketch.Solvable().Retyped(0, 0, NodeType.Empty).Build();

            Assert.That(PowerBound.Of(headless, LevelSketch.Tuning), Is.EqualTo(49));
        }

        [Test]
        public void InvariantBIsAPureLinearComputation()
        {
            var level = LevelSketch.Solvable().Build();
            var scattered = LevelSketch.Solvable().WithoutCorridors().Build();

            Assert.That(PowerBound.Of(level, LevelSketch.Tuning), Is.EqualTo(49));
            Assert.That(
                PowerBound.Of(scattered, LevelSketch.Tuning),
                Is.EqualTo(PowerBound.Of(level, LevelSketch.Tuning)),
                "Invariant B reads values, never the maze it has to walk.");
        }

        [Test]
        public void InvariantBCountsTheBossOutOfItsOwnBound()
        {
            var lean = PowerBound.Of(LevelSketch.Branching(boss: 39).Build(), LevelSketch.Tuning);
            var fat = PowerBound.Of(LevelSketch.Branching(boss: 400).Build(), LevelSketch.Tuning);

            Assert.That(fat, Is.EqualTo(lean));
        }

        static void Expect(LevelSketch sketch, SolvabilityReason reason, int offendingNodeId = -1)
        {
            var verdict = SolvabilityValidator.Validate(sketch.Build(), LevelSketch.Tuning);

            Assert.That(verdict.Reason, Is.EqualTo(reason));
            Assert.That(verdict.IsSafe, Is.False);
            Assert.That(verdict.OffendingNodeId, Is.EqualTo(offendingNodeId));
            Assert.That(verdict.ToString(), Does.Contain(reason.ToString()));
        }
    }
}
