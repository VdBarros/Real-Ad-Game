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
        }

        [Test]
        public void ThePlayerNeverStandsAsideOrOutOfReachOfItself()
        {
            var state = RunFixture.Begin(3);

            Assert.That(
                TargetMarks.Of(state, RunFixture.Start, TargetPreview.None),
                Is.EqualTo(TargetMark.Idle));
            Assert.That(
                TargetMarks.Of(state, RunFixture.Start, TargetPreview.Of(state, RunFixture.DoorstepEnemy)),
                Is.EqualTo(TargetMark.Idle),
                "The node the player stands on is not a target, so dimming it would say it lost a race it never ran.");
        }

        [Test]
        public void OnlyTheNodeUnderTheFingerReadsAsAimedAt()
        {
            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var aimed = mark == TargetMark.Walk
                    || mark == TargetMark.Win
                    || mark == TargetMark.Tie
                    || mark == TargetMark.Loss;

                Assert.That(TargetMarks.IsAimed(mark), Is.EqualTo(aimed), mark + " reads the wrong way round.");
            }
        }

        [Test]
        public void EveryMarkHasALookAndTheUnreachableOneIsTheDimmestOfThem()
        {
            var dimmest = float.MaxValue;
            var dimmestMark = TargetMark.Idle;

            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var look = TargetMarks.Look(mark);

                Assert.That(look.Scale, Is.GreaterThan(0f), mark + " has no size.");
                Assert.That(look.Weight, Is.InRange(0f, 1f), mark + " washes by an impossible amount.");

                if (look.Brightness < dimmest)
                {
                    dimmest = look.Brightness;
                    dimmestMark = mark;
                }
            }

            Assert.That(
                dimmestMark,
                Is.EqualTo(TargetMark.Unreachable),
                "An unreachable node has to read as the one you cannot tap.");
        }

        [Test]
        public void ANodeStandingAsideStillReadsAsOneYouCouldHaveTapped()
        {
            var aside = TargetMarks.Look(TargetMark.Aside);
            var unreachable = TargetMarks.Look(TargetMark.Unreachable);

            Assert.That(
                aside.Brightness - unreachable.Brightness,
                Is.GreaterThan(0.25f),
                "Aside and Unreachable both dim a badge, and the second one means the tap will be refused.");
            Assert.That(
                aside.Scale,
                Is.GreaterThan(unreachable.Scale),
                "An unreachable node shrinks out of the running; one merely standing aside does not.");
        }

        [Test]
        public void AnAimedNodeIsNeverTheSizeItRestsAt()
        {
            foreach (var mark in new[] { TargetMark.Walk, TargetMark.Win, TargetMark.Tie, TargetMark.Loss })
            {
                Assert.That(
                    TargetMarks.Look(mark).Scale,
                    Is.Not.EqualTo(TargetMarks.Look(TargetMark.Idle).Scale),
                    mark + " does not read as the one under the finger.");
            }
        }

        [Test]
        public void OnlyALossRecoilsWhileTheRestOfTheAimedMarksRise()
        {
            Assert.That(
                TargetMarks.Look(TargetMark.Loss).Scale,
                Is.LessThan(TargetMarks.Look(TargetMark.Idle).Scale),
                "A loss walks the player back, so its badge shrinks rather than rising.");

            foreach (var mark in new[] { TargetMark.Walk, TargetMark.Win, TargetMark.Tie })
            {
                Assert.That(
                    TargetMarks.Look(mark).Scale,
                    Is.GreaterThan(TargetMarks.Look(TargetMark.Idle).Scale),
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
                        Apart(TargetMarks.Look(fights[first]).Tint, TargetMarks.Look(fights[second]).Tint),
                        Is.GreaterThan(0.5f),
                        fights[first] + " and " + fights[second] + " read as the same colour, and they are the "
                        + "answer the player taps to find out.");
                }
            }
        }

        [Test]
        public void AMarkThatDoesNotExistHasNoLook()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TargetMarks.Look((TargetMark)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => TargetMarks.IsAimed((TargetMark)99));
        }

        static TargetMark Mark(RunState state, int nodeId)
        {
            return TargetMarks.Of(state, nodeId, TargetPreview.Of(state, nodeId));
        }

        static float Apart(Tint left, Tint right)
        {
            return Math.Abs(left.Red - right.Red)
                + Math.Abs(left.Green - right.Green)
                + Math.Abs(left.Blue - right.Blue);
        }
    }
}
