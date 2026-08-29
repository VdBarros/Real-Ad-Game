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
        public void NoBadgeAnybodyIsNotAimingAtIsEverGreyedOrHueShifted()
        {
            foreach (var mark in Resting)
            {
                var look = TargetMarks.Look(mark);

                foreach (BadgeStyle style in Enum.GetValues(typeof(BadgeStyle)))
                {
                    Assert.That(
                        BadgeTints.Washed(style, look),
                        Is.EqualTo(BadgeTints.Of(style)),
                        mark + " repaints a " + style + " badge, and a badge's colour says what it is.");
                }
            }
        }

        [Test]
        public void EveryBadgeHoldsAColourNobodyCouldCallGrey()
        {
            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var look = TargetMarks.Look(mark);

                foreach (BadgeStyle style in Enum.GetValues(typeof(BadgeStyle)))
                {
                    Assert.That(
                        BadgeTints.Chroma(BadgeTints.Washed(style, look)),
                        Is.GreaterThan(0.2f),
                        "a " + style + " badge wearing " + mark + " has drained to a grey.");
                }
            }
        }

        [Test]
        public void ANodeBehindAnUnbeatenEnemyKeepsItsHueAndFadesInstead()
        {
            var look = TargetMarks.Look(TargetMark.Unreachable);

            Assert.That(look.Weight, Is.EqualTo(0f), "an unreachable badge is faded, never repainted.");
            Assert.That(
                look.Opacity,
                Is.EqualTo(0.4f).Within(0.1f),
                "a sealed node reads as about two fifths there.");
            Assert.That(look.Opacity, Is.LessThan(TargetMarks.Look(TargetMark.Idle).Opacity));
        }

        [Test]
        public void AimingAtOneNodeFadesTheRestRatherThanDrainingThem()
        {
            var aside = TargetMarks.Look(TargetMark.Aside);
            var idle = TargetMarks.Look(TargetMark.Idle);
            var unreachable = TargetMarks.Look(TargetMark.Unreachable);

            Assert.That(aside.Weight, Is.EqualTo(0f), "standing aside is a fade, not a coat of grey.");
            Assert.That(aside.Opacity, Is.LessThan(idle.Opacity));
            Assert.That(
                aside.Opacity,
                Is.GreaterThan(unreachable.Opacity),
                "a node merely standing aside is still one the player could have tapped.");
        }

        [Test]
        public void ANodeAtRestIsWhollyThereAndAnAimedOneNeverFades()
        {
            Assert.That(TargetMarks.Look(TargetMark.Idle).Opacity, Is.EqualTo(1f));

            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                var look = TargetMarks.Look(mark);

                Assert.That(look.Opacity, Is.InRange(0.0001f, 1f), mark + " fades by an impossible amount.");

                if (TargetMarks.IsAimed(mark))
                {
                    Assert.That(
                        look.Opacity,
                        Is.EqualTo(1f),
                        mark + " is the answer under the finger, so it is never the faded one.");
                }
            }
        }

        [Test]
        public void AFadedMarkIsAFadeAndNothingElse()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MarkLook(new Tint(1f, 1f, 1f), 0f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MarkLook(new Tint(1f, 1f, 1f), 0f, 1f, 1.4f));
        }

        [Test]
        public void AGainWorthAlmostNothingBesideThePowerItIsAddedToStopsDrawing()
        {
            Assert.That(GateWorth.ShareOf(1, 244), Is.LessThan(GateWorth.Negligible));

            Assert.That(
                TargetMarks.Opacity(TargetMark.Idle, BadgeStyle.Additive, 1, 244),
                Is.EqualTo(TargetMarks.Suppressed),
                "a +1 beside a held 244 is worth four tenths of a percent and still takes up the screen.");
            Assert.That(
                TargetMarks.IsSuppressed(TargetMark.Idle, BadgeStyle.Additive, 1, 244),
                Is.True);
        }

        [Test]
        public void AGainStillWorthWalkingForKeepsTheBadgeItsMarkGivesIt()
        {
            foreach (var mark in Resting)
            {
                Assert.That(
                    TargetMarks.Opacity(mark, BadgeStyle.Additive, 40, 244),
                    Is.EqualTo(TargetMarks.Look(mark).Opacity),
                    "a gain worth a sixth of what the player holds is not clutter, and " + mark
                    + " is the only thing that may fade it.");
            }
        }

        [Test]
        public void TheCutIsTheShareItself()
        {
            var atTheCut = (int)Math.Round(1000 * GateWorth.Negligible);

            Assert.That(GateWorth.IsNegligible(atTheCut, 1000), Is.False, "the cut itself still draws.");
            Assert.That(GateWorth.IsNegligible(atTheCut - 1, 1000), Is.True);
            Assert.That(GateWorth.IsNegligible(atTheCut, 1001), Is.True);
        }

        [Test]
        public void AGateHiddenAtOnePowerIsBackTheMomentADrainDropsThePlayerUnderTheCut()
        {
            var ramp = new[] { 40, 400, 4000, 400, 40, 4000, 40 };
            var answers = new bool[ramp.Length];

            for (var step = 0; step < ramp.Length; step++)
            {
                answers[step] = TargetMarks.IsSuppressed(
                    TargetMark.Idle, BadgeStyle.Additive, 5, ramp[step]);
            }

            Assert.That(answers[0], Is.False, "a +5 beside 40 is an eighth of the run and reads.");
            Assert.That(answers[2], Is.True, "a +5 beside 4000 is worth an eighth of a percent.");

            for (var step = 0; step < ramp.Length; step++)
            {
                Assert.That(
                    answers[step],
                    Is.EqualTo(TargetMarks.IsSuppressed(TargetMark.Idle, BadgeStyle.Additive, 5, ramp[step])),
                    "power " + ramp[step] + " answered differently the second time round, so the mark "
                    + "is latched rather than read off the state.");

                for (var other = 0; other < ramp.Length; other++)
                {
                    if (ramp[other] != ramp[step])
                    {
                        continue;
                    }

                    Assert.That(
                        answers[other],
                        Is.EqualTo(answers[step]),
                        "power " + ramp[step] + " read one way climbing and another falling, so what "
                        + "the player already passed through is deciding what they see now.");
                }
            }
        }

        [Test]
        public void OnlyAGainIsEverSuppressedAndNeverAFightOrThePlayer()
        {
            foreach (BadgeStyle style in Enum.GetValues(typeof(BadgeStyle)))
            {
                if (style == BadgeStyle.Additive)
                {
                    continue;
                }

                Assert.That(
                    TargetMarks.IsSuppressed(TargetMark.Idle, style, 4, 4000),
                    Is.False,
                    "a " + style + " badge went quiet because its number was small beside the player's. "
                    + "A gate is clutter once it is worth nothing; an enemy worth nothing is a door "
                    + "standing open and the player still has to see it.");
            }
        }

        [Test]
        public void TheBadgeUnderTheFingerIsNeverTheOneThatVanishes()
        {
            foreach (TargetMark mark in Enum.GetValues(typeof(TargetMark)))
            {
                if (!TargetMarks.IsAimed(mark))
                {
                    continue;
                }

                Assert.That(
                    TargetMarks.Opacity(mark, BadgeStyle.Additive, 1, 4000),
                    Is.EqualTo(TargetMarks.Look(mark).Opacity),
                    mark + " is the answer the player asked for, so it is drawn however little it is worth.");
            }
        }

        [Test]
        public void ARunHoldingNoPowerYetHidesNothing()
        {
            Assert.That(GateWorth.IsNegligible(1, 0), Is.False);
            Assert.That(
                TargetMarks.Opacity(TargetMark.Idle, BadgeStyle.Additive, 1, 0),
                Is.EqualTo(TargetMarks.Look(TargetMark.Idle).Opacity));
            Assert.Throws<ArgumentOutOfRangeException>(() => GateWorth.ShareOf(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GateWorth.ShareOf(-1, 10));
        }

        [Test]
        public void NoGainTheCutHidesCouldHaveBrokenTheNearestWall()
        {
            var gateMoments = 0;
            var hidden = 0;
            var unlocking = 0;
            var smallestUnlockingShare = double.MaxValue;

            foreach (var levelNumber in new[] { 1, 7, LevelPlan.PlateauLevel, 20 })
            {
                var plan = LevelPlan.For(levelNumber);

                for (var seed = 0; seed < SuppressionSeeds; seed++)
                {
                    LevelGenerationReport report;
                    var level = LevelGenerator.Generate(seed, plan.Preset, plan.Recipe, plan.Tuning, out report);
                    var decisions = level.Graph.Decisions;
                    var walk = ParWalk.Richest(level.Graph, level.Tuning);
                    var state = RunState.Begin(level.Graph, level.StartingPower);

                    foreach (var target in walk.Targets)
                    {
                        var wall = NearestWall(state);

                        foreach (var node in decisions.Nodes)
                        {
                            if (node.Type != NodeType.Additive || state.IsConsumed(node.Id))
                            {
                                continue;
                            }

                            gateMoments++;
                            var quiet = TargetMarks.IsSuppressed(
                                TargetMark.Idle, BadgeStyle.Additive, node.Value, state.Power);
                            var breaks = wall > 0 && state.Power + node.Value > wall;

                            if (quiet)
                            {
                                hidden++;
                            }

                            if (!breaks)
                            {
                                continue;
                            }

                            unlocking++;
                            var share = node.Value / (double)state.Power;
                            if (share < smallestUnlockingShare)
                            {
                                smallestUnlockingShare = share;
                            }

                            Assert.That(
                                quiet,
                                Is.False,
                                "a gain of " + node.Value + " beside a held " + state.Power
                                + " is small enough to hide and large enough to break the wall at "
                                + wall + ", so the cut is set too high.");
                        }

                        var result = ActionResolver.Resolve(state, target);
                        if (result.Outcome == ActionOutcome.Rejected)
                        {
                            break;
                        }

                        state = result.State;
                        if (state.IsLevelComplete)
                        {
                            break;
                        }
                    }
                }
            }

            Console.WriteLine(
                "gate-moments " + gateMoments + ", hidden by a cut of " + GateWorth.Negligible + ": "
                + hidden + " (" + (100.0 * hidden / gateMoments).ToString("0.0") + "%), of which none "
                + "of the " + unlocking + " that break the nearest wall; the smallest wall-breaking "
                + "share seen was " + smallestUnlockingShare.ToString("0.0000"));

            Assert.That(unlocking, Is.GreaterThan(0), "the sweep found no gain that unlocks anything.");
            Assert.That(hidden, Is.GreaterThan(0), "the sweep hid nothing, so it proves nothing.");
            Assert.That(
                smallestUnlockingShare,
                Is.GreaterThan((double)GateWorth.Negligible),
                "the cut has caught up with the smallest gain that still breaks a wall.");
        }

        [Test]
        public void AMarkThatDoesNotExistHasNoLook()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TargetMarks.Look((TargetMark)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => TargetMarks.IsAimed((TargetMark)99));
        }

        const int SuppressionSeeds = 30;

        static readonly TargetMark[] Resting =
        {
            TargetMark.Idle, TargetMark.Aside, TargetMark.Unreachable
        };

        static int NearestWall(RunState state)
        {
            var wall = 0;

            foreach (var nodeId in state.ReachableNodes)
            {
                if (!state.BlocksPassage(nodeId))
                {
                    continue;
                }

                var barrier = state.Level.Decisions.Node(nodeId).Value;
                if (state.Power <= barrier && (wall == 0 || barrier < wall))
                {
                    wall = barrier;
                }
            }

            return wall;
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
