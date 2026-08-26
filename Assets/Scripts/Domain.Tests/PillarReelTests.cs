using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PillarReelTests
    {
        const float Frame = 1f / 60f;

        const float Tolerance = 0.0001f;

        [Test]
        public void TheReelPlaysStartToFinishInUnderTwentySeconds()
        {
            Assert.That(PillarStage.Total, Is.LessThan(20f));

            var reel = PillarReel.Opening;
            var seconds = 0f;

            while (!reel.IsOver)
            {
                reel = reel.Advanced(Frame);
                seconds += Frame;
                Assert.That(seconds, Is.LessThan(20f), "The reel outran twenty seconds of frames.");
            }

            Assert.That(reel.Elapsed, Is.EqualTo(PillarStage.Total).Within(Tolerance));
        }

        [Test]
        public void AReelOpensOnTheEstablishingShotWithNothingSpent()
        {
            var reel = PillarReel.Opening;

            Assert.That(reel.Elapsed, Is.Zero);
            Assert.That(reel.Beat, Is.EqualTo(PillarBeat.Establish));
            Assert.That(reel.IsOver, Is.False);
        }

        [Test]
        public void EveryBeatFiresOnceAndInOrder()
        {
            var expected = new[]
            {
                PillarBeat.Establish,
                PillarBeat.Throw,
                PillarBeat.Drain,
                PillarBeat.Count,
                PillarBeat.Crown,
                PillarBeat.Cross,
                PillarBeat.Fall,
                PillarBeat.Over
            };

            var seen = new List<PillarBeat>();
            var reel = PillarReel.Opening;
            seen.Add(reel.Beat);

            while (!reel.IsOver)
            {
                reel = reel.Advanced(Frame);
                if (seen[seen.Count - 1] != reel.Beat)
                {
                    seen.Add(reel.Beat);
                }
            }

            Assert.That(seen, Is.EqualTo(expected));
        }

        [Test]
        public void ThePlayerGivesAwayThreeAndTheAdNumbersAreWhatShows()
        {
            Assert.That(At(PillarStage.Drain - Frame).Player.Number, Is.EqualTo(5));
            Assert.That(At(PillarStage.Drain).Player.Number, Is.EqualTo(4));
            Assert.That(At(PillarStage.Drain + PillarStage.LadderStep).Player.Number, Is.EqualTo(2));
            Assert.That(At(PillarStage.Total).Player.Number, Is.EqualTo(2));
        }

        [Test]
        public void TheGirlCountsUpThroughTheAdRungsToFifty()
        {
            Assert.That(At(PillarStage.Count - Frame).Girl.Number, Is.EqualTo(25));
            Assert.That(At(PillarStage.Count).Girl.Number, Is.EqualTo(34));
            Assert.That(At(PillarStage.Count + PillarStage.LadderStep).Girl.Number, Is.EqualTo(46));
            Assert.That(At(PillarStage.Count + 2f * PillarStage.LadderStep).Girl.Number, Is.EqualTo(50));
            Assert.That(At(PillarStage.Total).Girl.Number, Is.EqualTo(50));
        }

        [Test]
        public void TheGirlIsAtFiftyExactlyAsSheIsCrowned()
        {
            var crowned = At(PillarStage.Crown);

            Assert.That(crowned.Girl.Number, Is.EqualTo(50));
            Assert.That(crowned.Girl.Look, Is.EqualTo(CastLook.Queen));
            Assert.That(At(PillarStage.Crown - Frame).Girl.Look, Is.EqualTo(CastLook.Peasant));
        }

        [Test]
        public void TheRivalNeverMovesOffNinetyNine()
        {
            for (var seconds = 0f; seconds <= PillarStage.Total; seconds += 0.1f)
            {
                var rival = At(seconds).Rival;

                Assert.That(rival.Number, Is.EqualTo(PillarStage.RivalNumber));
                Assert.That(rival.Look, Is.EqualTo(CastLook.Champion));
            }
        }

        [Test]
        public void APillarStandsAsTallAsTheNumberOnIt()
        {
            var opening = PillarReel.Opening;

            AssertProportional(opening.Player);
            AssertProportional(opening.Girl);
            AssertProportional(opening.Rival);

            var drained = At(PillarStage.Count - Frame);

            AssertProportional(drained.Player);
            AssertProportional(drained.Girl);
        }

        [Test]
        public void TheGirlPillarIsLevelWithTheRivalBeforeSheCrosses()
        {
            var crossing = At(PillarStage.Cross);

            Assert.That(
                crossing.Girl.PillarHeight,
                Is.EqualTo(crossing.Rival.PillarHeight).Within(Tolerance));

            Assert.That(At(PillarStage.Crown).Girl.PillarHeight, Is.LessThan(crossing.Rival.PillarHeight));
        }

        [Test]
        public void TheGirlWalksAcrossToTheRivalAndThePlayerIsLeftBehind()
        {
            var crossing = At(PillarStage.Cross);
            var met = At(PillarStage.Cross + PillarStage.WalkSeconds);

            Assert.That(PillarStage.GirlOffsetAt(PillarStage.Cross), Is.EqualTo(PillarStage.GirlOffset));
            Assert.That(
                PillarStage.GirlOffsetAt(PillarStage.Cross + PillarStage.WalkSeconds),
                Is.EqualTo(PillarStage.MeetOffset).Within(Tolerance));

            Assert.That(met.Girl.Position.Y, Is.EqualTo(met.Rival.Position.Y).Within(Tolerance));
            Assert.That(crossing.Player.Number, Is.EqualTo(2));
            Assert.That(met.Player.Number, Is.EqualTo(2));
        }

        [Test]
        public void TheGirlLeavesHerOwnPillarStandingWhereItWas()
        {
            var seated = PillarReel.Opening.Girl.PillarBase;

            for (var seconds = 0f; seconds <= PillarStage.Total; seconds += 0.1f)
            {
                Assert.That(At(seconds).Girl.PillarBase, Is.EqualTo(seated));
            }

            var met = At(PillarStage.Cross + PillarStage.WalkSeconds);

            Assert.That(Apart(met.Girl.Position, met.Girl.PillarBase), Is.GreaterThan(PillarStage.Elbow));
            Assert.That(PillarReel.Opening.Player.PillarBase.Y, Is.Zero);
            Assert.That(PillarReel.Opening.Rival.PillarBase.Y, Is.Zero);
        }

        [Test]
        public void TheHeartFliesFromThePlayerToTheGirlAndDrainsHimOnArrival()
        {
            Assert.That(At(PillarStage.Throw - Frame).HeartIsFlying, Is.False);
            Assert.That(At(PillarStage.Throw).HeartIsFlying, Is.True);
            Assert.That(At(PillarStage.Drain).HeartIsFlying, Is.False);

            var thrown = At(PillarStage.Throw);
            var landing = At(PillarStage.Drain - Frame);

            Assert.That(Apart(thrown.HeartPosition, thrown.Player.Position), Is.LessThan(0.1f));
            Assert.That(Apart(landing.HeartPosition, landing.Girl.Position), Is.LessThan(0.5f));
        }

        [Test]
        public void ThePlayerIsASkeletonForEveryFrameAfterTheDrain()
        {
            Assert.That(At(PillarStage.Drain - Frame).Player.Look, Is.EqualTo(CastLook.Peasant));

            for (var seconds = PillarStage.Drain; seconds <= PillarStage.Total; seconds += 0.1f)
            {
                Assert.That(At(seconds).Player.Look, Is.EqualTo(CastLook.Skeleton));
            }
        }

        [Test]
        public void ThePortalOpensBeforeThePlayerDropsThroughIt()
        {
            Assert.That(At(PillarStage.Fall - Frame).PortalOpen, Is.Zero);
            Assert.That(At(PillarStage.Fall - Frame).PlayerFall, Is.Zero);

            var opened = At(PillarStage.Fall + PillarStage.PortalSeconds);

            Assert.That(opened.PortalOpen, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(opened.PlayerFall, Is.Zero);

            var ended = At(PillarStage.Total);

            Assert.That(ended.PlayerFall, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                ended.Player.Position.Y,
                Is.LessThan(At(PillarStage.Fall).Player.Position.Y - PillarStage.FallDepth * 0.9f));
        }

        [Test]
        public void TheCameraPullsBackFromThePlayerToTheWholeStage()
        {
            Assert.That(PillarReel.Opening.Framing, Is.EqualTo(PillarStage.Near));
            Assert.That(At(PillarStage.Throw).Framing, Is.EqualTo(PillarStage.Wide));
            Assert.That(At(PillarStage.Total).Framing, Is.EqualTo(PillarStage.Wide));
            Assert.That(PillarStage.Near.OrthographicSize, Is.LessThan(PillarStage.Wide.OrthographicSize));
        }

        [Test]
        public void SkippingAtAnyFrameLandsWherePlayingRightThroughLands()
        {
            var played = PillarReel.Opening;
            while (!played.IsOver)
            {
                played = played.Advanced(Frame);
            }

            var reel = PillarReel.Opening;

            for (var frame = 0; !reel.IsOver; frame++)
            {
                Assert.That(reel.Skipped(), Is.EqualTo(played), "Skipping on frame " + frame + " landed elsewhere.");
                Assert.That(reel.Skipped().IsOver, Is.True);
                reel = reel.Advanced(Frame);
            }

            Assert.That(reel.Skipped(), Is.EqualTo(played));
        }

        [Test]
        public void SkippingOnTheOpeningFrameIsAlreadyOver()
        {
            Assert.That(PillarReel.Opening.Skipped().IsOver, Is.True);
            Assert.That(PillarReel.Opening.Skipped().Beat, Is.EqualTo(PillarBeat.Over));
        }

        [Test]
        public void AReelStopsAtItsEndRatherThanRunningPastIt()
        {
            var reel = PillarReel.Opening.Advanced(PillarStage.Total * 4f);

            Assert.That(reel.Elapsed, Is.EqualTo(PillarStage.Total));
            Assert.That(reel.Advanced(PillarStage.Total), Is.EqualTo(reel));
        }

        [Test]
        public void AReelOnlyEverRunsForwards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PillarReel.Opening.Advanced(-Frame));
        }

        [Test]
        public void TheLaddersAreWalkedOffTheSameStepTheScriptIsCutOn()
        {
            Assert.That(
                PillarStage.Count + (PillarStage.GirlLadder.Count - 2) * PillarStage.LadderStep,
                Is.EqualTo(PillarStage.Crown).Within(Tolerance));

            Assert.That(
                PillarStage.Drain + (PillarStage.PlayerLadder.Count - 2) * PillarStage.LadderStep,
                Is.LessThan(PillarStage.Count));

            Assert.That(PillarStage.Crown + PillarStage.RiseSeconds, Is.EqualTo(PillarStage.Cross).Within(Tolerance));
            Assert.That(PillarStage.Cross + PillarStage.WalkSeconds, Is.LessThan(PillarStage.Fall));
            Assert.That(PillarStage.Fall + PillarStage.PortalSeconds, Is.LessThan(PillarStage.Total));
        }

        [Test]
        public void NobodyStandsOnTopOfAnybodyElse()
        {
            Assert.That(PillarStage.PlayerOffset, Is.LessThan(PillarStage.GirlOffset));
            Assert.That(PillarStage.GirlOffset, Is.LessThan(PillarStage.RivalOffset));
            Assert.That(PillarStage.MeetOffset, Is.LessThan(PillarStage.RivalOffset));
            Assert.That(PillarStage.MeetOffset, Is.GreaterThan(PillarStage.GirlOffset));
        }

        [Test]
        public void TheBadgePlanHoldsTheWidestNumberOnTheStage()
        {
            var plan = PillarStage.Plan;

            Assert.That(plan.Capacity, Is.EqualTo(BadgeText.Digits(PillarStage.RivalNumber)));
            Assert.That(PillarReel.Opening.Rival.Cells, Is.LessThanOrEqualTo(plan.Capacity));
            Assert.That(At(PillarStage.Total).Girl.Cells, Is.LessThanOrEqualTo(plan.Capacity));
            Assert.That(PillarReel.Opening.Player.Cells, Is.LessThanOrEqualTo(plan.Capacity));
        }

        [Test]
        public void TheCastCarriesTheAdBadgeColours()
        {
            Assert.That(PillarReel.Opening.Player.Badge, Is.EqualTo(BadgeStyle.Player));
            Assert.That(PillarReel.Opening.Girl.Badge, Is.EqualTo(BadgeStyle.Enemy));
            Assert.That(PillarReel.Opening.Rival.Badge, Is.EqualTo(BadgeStyle.Enemy));
        }

        static void AssertProportional(CastMark mark)
        {
            Assert.That(
                mark.PillarHeight,
                Is.EqualTo(mark.Number * PillarStage.MetresPerPoint).Within(Tolerance),
                mark.ToString());
        }

        static float Apart(WorldPoint first, WorldPoint second)
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            var z = first.Z - second.Z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        static PillarReel At(float seconds)
        {
            return PillarReel.Opening.Advanced(seconds);
        }
    }
}
