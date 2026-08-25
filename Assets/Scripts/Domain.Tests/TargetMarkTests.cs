using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TargetMarkTests
    {
        [Test]
        public void AReachableNodeNobodyIsAimingAtWearsNothing()
        {
            var state = RunFixture.Begin(3);

            Assert.That(
                TargetMarks.Of(state, RunFixture.Additive, TargetPreview.None),
                Is.EqualTo(TargetMark.Idle));
        }

        [Test]
        public void ANodeBehindAnEnemyTooStrongToPassWearsUnreachable()
        {
            var state = RunFixture.Begin(1);

            Assert.That(
                TargetMarks.Of(state, RunFixture.Boss, TargetPreview.None),
                Is.EqualTo(TargetMark.Unreachable));
        }

        [Test]
        public void TheAimedNodeWearsTheOutcomeTheResolverWouldGiveIt()
        {
            var win = RunFixture.Begin(3);
            var tie = RunFixture.Begin(RunFixture.DoorstepEnemyValue);
            var loss = RunFixture.Begin(RunFixture.DoorstepEnemyValue - 1);

            Assert.That(Mark(win, RunFixture.DoorstepEnemy), Is.EqualTo(TargetMark.Win));
            Assert.That(Mark(tie, RunFixture.DoorstepEnemy), Is.EqualTo(TargetMark.Tie));
            Assert.That(Mark(loss, RunFixture.DoorstepEnemy), Is.EqualTo(TargetMark.Loss));
            Assert.That(Mark(win, RunFixture.Additive), Is.EqualTo(TargetMark.Walk));
        }

        [Test]
        public void AimingAtOneNodeStandsEveryOtherOneAside()
        {
            var state = RunFixture.Begin(3);
            var preview = TargetPreview.Of(state, RunFixture.DoorstepEnemy);

            Assert.That(
                TargetMarks.Of(state, RunFixture.Additive, preview),
                Is.EqualTo(TargetMark.Aside));
            Assert.That(
                TargetMarks.WeightOf(TargetMark.Aside),
                Is.GreaterThan(TargetMarks.WeightOf(TargetMark.Idle)),
                "A node the finger passed over has to fall back behind the one it settled on.");
            Assert.That(
                TargetMarks.WeightOf(TargetMark.Aside),
                Is.LessThan(TargetMarks.WeightOf(TargetMark.Unreachable)),
                "A node standing aside is still a legal target, so it cannot read as one that is not.");
        }

        [Test]
        public void OnlyAnIdleOrAimedMarkInvitesATap()
        {
            Assert.That(TargetMarks.IsTappable(TargetMark.Unreachable), Is.False);

            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                if (mark == TargetMark.Unreachable)
                {
                    continue;
                }

                Assert.That(TargetMarks.IsTappable(mark), Is.True, mark + " should invite a tap.");
            }
        }

        [Test]
        public void EveryMarkHasALookAndTheUnreachableOneIsTheDimmestOfThem()
        {
            var dimmest = float.MaxValue;
            var dimmestMark = TargetMark.Idle;

            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var tint = TargetMarks.TintOf(mark);
                var weight = TargetMarks.WeightOf(mark);
                var lit = (tint.Red + tint.Green + tint.Blue) * weight + (1f - weight);

                Assert.That(TargetMarks.ScaleOf(mark), Is.GreaterThan(0f), mark + " has no size.");
                Assert.That(weight, Is.InRange(0f, 1f), mark + " washes by an impossible amount.");

                if (lit < dimmest)
                {
                    dimmest = lit;
                    dimmestMark = mark;
                }
            }

            Assert.That(
                dimmestMark,
                Is.EqualTo(TargetMark.Unreachable),
                "An unreachable node has to read as the one you cannot tap.");
        }

        [Test]
        public void AnAimedNodeIsNeverTheSizeItRestsAt()
        {
            foreach (var mark in new[] { TargetMark.Walk, TargetMark.Win, TargetMark.Tie, TargetMark.Loss })
            {
                Assert.That(
                    TargetMarks.ScaleOf(mark),
                    Is.Not.EqualTo(TargetMarks.ScaleOf(TargetMark.Idle)),
                    mark + " does not read as the one under the finger.");
            }
        }

        [Test]
        public void OnlyALossRecoilsWhileTheRestOfTheAimedMarksRise()
        {
            Assert.That(
                TargetMarks.ScaleOf(TargetMark.Loss),
                Is.LessThan(TargetMarks.ScaleOf(TargetMark.Idle)),
                "A loss walks the player back, so its badge shrinks rather than rising.");

            foreach (var mark in new[] { TargetMark.Walk, TargetMark.Win, TargetMark.Tie })
            {
                Assert.That(
                    TargetMarks.ScaleOf(mark),
                    Is.GreaterThan(TargetMarks.ScaleOf(TargetMark.Idle)),
                    mark + " is an outcome worth taking, so its badge rises.");
            }
        }

        [Test]
        public void TheThreeFightOutcomesAreThreeDifferentColours()
        {
            var fights = new[] { TargetMark.Win, TargetMark.Tie, TargetMark.Loss };

            for (var first = 0; first < fights.Length; first++)
            {
                for (var second = first + 1; second < fights.Length; second++)
                {
                    Assert.That(
                        Apart(TargetMarks.TintOf(fights[first]), TargetMarks.TintOf(fights[second])),
                        Is.GreaterThan(0.5f),
                        fights[first] + " and " + fights[second] + " read as the same colour, and they are the "
                        + "answer the player taps to find out.");
                }
            }
        }

        static float Apart(Tint left, Tint right)
        {
            return Math.Abs(left.Red - right.Red)
                + Math.Abs(left.Green - right.Green)
                + Math.Abs(left.Blue - right.Blue);
        }

        static TargetMark Mark(RunState state, int nodeId)
        {
            return TargetMarks.Of(state, nodeId, TargetPreview.Of(state, nodeId));
        }
    }
}
