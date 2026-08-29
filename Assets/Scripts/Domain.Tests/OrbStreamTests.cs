using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class OrbStreamTests
    {
        const float Tolerance = 1e-4f;

        const float Frame = 1f / 60f;

        const int Consecutive = 400;

        static readonly WorldPoint Site = new WorldPoint(4f, 0.55f, -2f);

        static readonly WorldPoint Post = new WorldPoint(-3f, 0.55f, 5f);

        [Test]
        public void TheThreeRewardStagesRunAfterTheDissolveAndNoneOfThemHoldsTheControls()
        {
            Assert.That(
                VictoryStages.SecondsOf(VictoryStage.OrbFlight), Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(
                VictoryStages.SecondsOf(VictoryStage.Burst), Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(
                VictoryStages.SecondsOf(VictoryStage.Count),
                Is.EqualTo(CountUp.Seconds).Within(Tolerance));

            Assert.That(VictoryStages.BlocksInput(VictoryStage.OrbFlight), Is.False);
            Assert.That(VictoryStages.BlocksInput(VictoryStage.Burst), Is.False);
            Assert.That(VictoryStages.BlocksInput(VictoryStage.Count), Is.False);

            Assert.That(VictoryStages.After(VictoryStage.Dissolve), Is.EqualTo(VictoryStage.OrbFlight));
            Assert.That(VictoryStages.After(VictoryStage.OrbFlight), Is.EqualTo(VictoryStage.Burst));
            Assert.That(VictoryStages.After(VictoryStage.Burst), Is.EqualTo(VictoryStage.Count));
            Assert.That(VictoryStages.After(VictoryStage.Count), Is.EqualTo(VictoryStage.Done));
        }

        [Test]
        public void TheRewardStagesLeaveTheHoldOnTheControlsExactlyWhereTheClashAndDissolveLeftIt()
        {
            Assert.That(VictoryStages.BlockingSeconds, Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(
                VictoryStages.BlockingSeconds,
                Is.EqualTo(VictoryStages.ClosesAt(VictoryStage.Dissolve)).Within(Tolerance));
            Assert.That(
                VictoryStages.OpensAt(VictoryStage.OrbFlight),
                Is.EqualTo(VictoryStages.BlockingSeconds).Within(Tolerance));
            Assert.That(
                VictoryStages.Seconds,
                Is.EqualTo(1.2f + 1.5f + 0.3f + CountUp.Seconds).Within(Tolerance));
            Assert.That(VictoryStages.Seconds, Is.GreaterThan(VictoryStages.BlockingSeconds));
        }

        [Test]
        public void AStreamNobodyWonCarriesNothingAndNeverFlies()
        {
            var empty = OrbStream.None;

            Assert.That(empty.IsCarried, Is.False);
            Assert.That(empty.Gain, Is.EqualTo(0));
            Assert.That(empty.Orbs, Is.EqualTo(0));
            Assert.That(empty.IsFlying, Is.False);
            Assert.That(empty.HasLanded, Is.False);
            Assert.That(empty.IsSpent, Is.True);
            Assert.That(empty.Flare, Is.EqualTo(0f));
            Assert.That(empty.Advanced(10f), Is.EqualTo(empty));
            Assert.That(OrbStream.From(Site, 0), Is.EqualTo(empty));
            Assert.That(OrbStream.From(Site, -4), Is.EqualTo(empty));
            Assert.That(empty.ToString(), Does.Contain("no orbs"));
        }

        [Test]
        public void TheOrbsWaitOnTheDissolveBeforeTheyLeaveTheDeathSite()
        {
            var stream = OrbStream.From(Site, 5);

            Assert.That(stream.IsCarried, Is.True);
            Assert.That(stream.IsFlying, Is.False);

            var holding = stream.Advanced(VictoryStages.BlockingSeconds - 0.001f);

            Assert.That(holding.IsFlying, Is.False);
            Assert.That(holding.Stage, Is.EqualTo(VictoryStage.Dissolve));

            var loosed = stream.Advanced(VictoryStages.BlockingSeconds);

            Assert.That(loosed.IsFlying, Is.True);
            Assert.That(loosed.Stage, Is.EqualTo(VictoryStage.OrbFlight));

            for (var orb = 0; orb < loosed.Orbs; orb++)
            {
                Assert.That(loosed.ThroughOf(orb), Is.EqualTo(0f), orb.ToString());
                Assert.That(loosed.PositionOf(orb, Post), Is.EqualTo(Site), orb.ToString());
            }
        }

        [Test]
        public void EveryOrbLandsOnTheLivePlayerRatherThanWhereTheFightWasFought()
        {
            var stream = OrbStream.From(Site, 4);
            var landed = stream.Advanced(VictoryStages.ClosesAt(VictoryStage.Burst));

            for (var orb = 0; orb < landed.Orbs; orb++)
            {
                Assert.That(landed.ThroughOf(orb), Is.EqualTo(1f), orb.ToString());
                Assert.That(landed.PositionOf(orb, Post), Is.EqualTo(Post), orb.ToString());
            }

            var elsewhere = new WorldPoint(11f, 3.25f, -9f);

            for (var orb = 0; orb < landed.Orbs; orb++)
            {
                Assert.That(landed.PositionOf(orb, elsewhere), Is.EqualTo(elsewhere), orb.ToString());
            }
        }

        [Test]
        public void OrbsHomeOnAPlayerWhoKeepsWalkingWhileTheyFly()
        {
            var stream = OrbStream.From(Site, 6);
            var player = Post;
            var closing = float.MaxValue;
            var opened = false;

            for (var frame = 0; frame < 400 && !stream.HasLanded; frame++)
            {
                player = new WorldPoint(player.X - 0.03f, player.Y, player.Z + 0.05f);
                stream = stream.Advanced(Frame);

                if (!stream.IsFlying)
                {
                    continue;
                }

                var away = Away(stream.PositionOf(0, player), player);

                if (opened && stream.ThroughOf(0) > 0.4f)
                {
                    Assert.That(away, Is.LessThan(closing + Tolerance), stream.ToString());
                }

                closing = away;
                opened = true;
            }

            Assert.That(stream.HasLanded, Is.True);

            for (var orb = 0; orb < stream.Orbs; orb++)
            {
                Assert.That(Away(stream.PositionOf(orb, player), player), Is.LessThan(Tolerance));
            }
        }

        [Test]
        public void ThePlayerIsFreeToMoveForEveryInstantAnOrbIsInTheAir()
        {
            var stream = OrbStream.From(Site, 7);
            var timeline = VictoryTimeline.Begun;
            var flown = 0;

            for (var frame = 0; frame < 400 && !stream.IsSpent; frame++)
            {
                stream = stream.Advanced(Frame);
                timeline = timeline.Advanced(Frame);

                if (!stream.IsFlying)
                {
                    continue;
                }

                flown++;
                Assert.That(timeline.BlocksInput, Is.False, timeline.ToString());
            }

            Assert.That(flown, Is.GreaterThan(0));
        }

        [Test]
        public void AStreamHandsItsValueOverExactlyOnceHoweverLongItIsAdvancedFor()
        {
            var stream = OrbStream.From(Site, 9);
            var handovers = 0;

            for (var frame = 0; frame < 2000; frame++)
            {
                var carried = stream.HasLanded;
                stream = stream.Advanced(Frame);

                if (!carried && stream.HasLanded)
                {
                    handovers++;
                }
            }

            Assert.That(handovers, Is.EqualTo(1));
            Assert.That(stream.Gain, Is.EqualTo(9));
            Assert.That(stream.IsSpent, Is.True);
            Assert.That(stream.IsFlying, Is.False);
        }

        [Test]
        public void TheNumberOnlyStartsClimbingOnceTheOrbsHaveLanded()
        {
            var stream = OrbStream.From(Site, 5);

            Assert.That(
                VictoryStages.OpensAt(VictoryStage.Count),
                Is.EqualTo(VictoryStages.ClosesAt(VictoryStage.Burst)).Within(Tolerance));

            var justBefore = stream.Advanced(VictoryStages.OpensAt(VictoryStage.Count) - 0.001f);
            var justAfter = stream.Advanced(VictoryStages.OpensAt(VictoryStage.Count));

            Assert.That(justBefore.HasLanded, Is.False);
            Assert.That(justAfter.HasLanded, Is.True);
            Assert.That(justAfter.Stage, Is.EqualTo(VictoryStage.Count));

            var climbing = CountUp.Settled(20).Toward(25);
            var steps = new List<int>();

            for (var frame = 0; frame < 200 && !climbing.IsSettled; frame++)
            {
                climbing = climbing.Advanced(Frame);
                if (steps.Count == 0 || steps[steps.Count - 1] != climbing.Display)
                {
                    steps.Add(climbing.Display);
                }
            }

            Assert.That(steps.Count, Is.GreaterThan(1));
            Assert.That(steps[steps.Count - 1], Is.EqualTo(25));
            Assert.That(
                CountUp.Seconds,
                Is.LessThanOrEqualTo(VictoryStages.SecondsOf(VictoryStage.Count) + Tolerance));
        }

        [Test]
        public void EveryOrbOfAStreamIsInTheAirInsideTheFlightAndBurstItIsCutTo()
        {
            var stream = OrbStream.From(Site, OrbStream.Most);

            for (var orb = 0; orb < stream.Orbs; orb++)
            {
                Assert.That(
                    stream.OpensAt(orb),
                    Is.GreaterThanOrEqualTo(VictoryStages.OpensAt(VictoryStage.OrbFlight) - Tolerance),
                    orb.ToString());
                Assert.That(
                    stream.LandsAt(orb),
                    Is.LessThanOrEqualTo(VictoryStages.ClosesAt(VictoryStage.Burst) + Tolerance),
                    orb.ToString());

                if (orb > 0)
                {
                    Assert.That(
                        stream.LandsAt(orb), Is.GreaterThan(stream.LandsAt(orb - 1)), orb.ToString());
                }
            }
        }

        [Test]
        public void ARicherKillSendsMoreOrbsWithinTheBoundsTheStreamKeeps()
        {
            Assert.That(OrbStream.From(Site, 1).Orbs, Is.EqualTo(OrbStream.Fewest));
            Assert.That(OrbStream.From(Site, OrbStream.Fewest).Orbs, Is.EqualTo(OrbStream.Fewest));
            Assert.That(OrbStream.From(Site, 5).Orbs, Is.EqualTo(5));
            Assert.That(OrbStream.From(Site, 400).Orbs, Is.EqualTo(OrbStream.Most));
            Assert.That(OrbStream.From(Site, 400).Gain, Is.EqualTo(400));
        }

        [Test]
        public void EachOrbDrawsATaperingTrailBehindItAndNothingAtAllOnceItHasArrived()
        {
            var stream = OrbStream.From(Site, 4).Advanced(
                VictoryStages.OpensAt(VictoryStage.OrbFlight) + VictoryStages.OrbFlightSeconds * 0.6f);

            Assert.That(stream.SizeOf(0, 0), Is.EqualTo(OrbStream.Size).Within(Tolerance));

            for (var dot = 1; dot <= OrbStream.TrailDots; dot++)
            {
                Assert.That(stream.SizeOf(0, dot), Is.LessThan(stream.SizeOf(0, dot - 1)), dot.ToString());
                Assert.That(stream.SizeOf(0, dot), Is.GreaterThan(0f), dot.ToString());
                Assert.That(
                    Away(stream.TrailOf(0, dot, Post), Post),
                    Is.GreaterThan(Away(stream.TrailOf(0, dot - 1, Post), Post)),
                    dot.ToString());
            }

            var arrived = stream.Advanced(VictoryStages.OrbFlightSeconds);

            for (var dot = 0; dot <= OrbStream.TrailDots; dot++)
            {
                Assert.That(arrived.SizeOf(0, dot), Is.EqualTo(0f), dot.ToString());
            }
        }

        [Test]
        public void TheBurstFlaresOnceBetweenTheLastArrivalAndTheCount()
        {
            var stream = OrbStream.From(Site, 5);

            Assert.That(stream.Flare, Is.EqualTo(0f));
            Assert.That(
                stream.Advanced(VictoryStages.ClosesAt(VictoryStage.OrbFlight)).Flare,
                Is.EqualTo(0f).Within(Tolerance));
            Assert.That(
                stream.Advanced(
                    VictoryStages.ClosesAt(VictoryStage.OrbFlight) + VictoryStages.BurstSeconds * 0.5f)
                    .Flare,
                Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                stream.Advanced(VictoryStages.ClosesAt(VictoryStage.Burst)).Flare,
                Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AStreamOnlyEverFliesForwardsAndCarriesNoOrbItWasNotGiven()
        {
            var stream = OrbStream.From(Site, 4);

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Advanced(-Frame));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.ThroughOf(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.ThroughOf(stream.Orbs));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.PositionOf(stream.Orbs, Post));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.SizeOf(0, OrbStream.TrailDots + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.TrailOf(0, -1, Post));
            Assert.Throws<ArgumentOutOfRangeException>(() => OrbStream.None.ThroughOf(0));
        }

        [Test]
        public void TwoStreamsShowingTheSameThingAreTheSameValue()
        {
            var one = OrbStream.From(Site, 5).Advanced(0.2f);
            var other = OrbStream.From(Site, 5).Advanced(0.2f);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one.Equals((object)other), Is.True);
            Assert.That(one, Is.Not.EqualTo(OrbStream.From(Site, 6).Advanced(0.2f)));
            Assert.That(one, Is.Not.EqualTo(OrbStream.From(Post, 5).Advanced(0.2f)));
            Assert.That(one.Site, Is.EqualTo(Site));
            Assert.That(one.ToString(), Does.Contain("worth 5"));
        }

        [Test]
        public void AStreamStandsClearOfTheFightThatSpawnedItAndOutlivesIt()
        {
            var fight = Fight.Of(ActionOutcome.Win);
            var stream = OrbStream.From(Site, 5);

            for (var frame = 0; frame < 400 && !stream.IsSpent; frame++)
            {
                fight = fight.Advanced(Frame);
                stream = stream.Advanced(Frame);

                if (!fight.IsSettled)
                {
                    Assert.That(stream.IsFlying, Is.False, stream.ToString());
                }
            }

            Assert.That(fight.IsSettled, Is.True);
            Assert.That(stream.IsSpent, Is.True);
            Assert.That(
                VictoryStages.Seconds, Is.GreaterThan(fight.Seconds + VictoryStages.OrbFlightSeconds));
        }

        [Test]
        public void ASingleTapCrossingThreeEnemiesResolvesToOneResultAndOneStreamWorthTheWholeGain()
        {
            var level = Gauntlet();
            var opening = RunState.Begin(level, 10);
            var resolved = ActionResolver.Along(opening, new[] { 0, 1, 2, 3 });

            Assert.That(resolved.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(resolved.State.Power, Is.EqualTo(10 + 1 + 2 + 3));
            Assert.That(resolved.State.ConsumedNodes.Count, Is.EqualTo(3));

            var batched = OrbStream.From(Site, resolved.State.Power - opening.Power);

            Assert.That(batched.Gain, Is.EqualTo(6));

            var handovers = 0;
            var carried = 0;

            for (var frame = 0; frame < 600; frame++)
            {
                var landed = batched.HasLanded;
                batched = batched.Advanced(Frame);

                if (landed || !batched.HasLanded)
                {
                    continue;
                }

                handovers++;
                carried += batched.Gain;
            }

            Assert.That(handovers, Is.EqualTo(1));
            Assert.That(carried, Is.EqualTo(6));
            Assert.That(opening.Power + carried, Is.EqualTo(resolved.State.Power));
        }

        [Test]
        public void ASecondStreamLaunchedMidFlightNeitherCancelsNorSwallowsTheFirst()
        {
            var flying = new List<OrbStream> { OrbStream.From(Site, 4) };
            var handed = 0;
            var carried = 0;
            var together = 0;

            for (var frame = 0; frame < 600 && flying.Count > 0; frame++)
            {
                if (frame == 120)
                {
                    flying.Add(OrbStream.From(Post, 9));
                }

                for (var index = flying.Count - 1; index >= 0; index--)
                {
                    var landed = flying[index].HasLanded;
                    var moved = flying[index].Advanced(Frame);
                    flying[index] = moved;

                    if (!landed && moved.HasLanded)
                    {
                        handed++;
                        carried += moved.Gain;
                    }

                    if (moved.IsSpent)
                    {
                        flying.RemoveAt(index);
                    }
                }

                together = flying.Count > together ? flying.Count : together;
            }

            Assert.That(together, Is.EqualTo(2));
            Assert.That(handed, Is.EqualTo(2));
            Assert.That(carried, Is.EqualTo(13));
            Assert.That(flying, Is.Empty);
        }

        [Test]
        public void ALongRunOfVictoriesAccumulatesNoDriftInWhenTheOrbsLandOrTheCountOpens()
        {
            var clock = 0d;
            var carried = 0f;
            var worstLanding = 0f;
            var worstCount = 0f;
            var spans = new List<float>();

            for (var fight = 0; fight < Consecutive; fight++)
            {
                var contact = clock - carried;
                var stream = OrbStream.From(Site, 5).Advanced(carried);
                var delta = Frame * (1f + 0.25f * (fight % 5 - 2));
                var landedAt = -1d;
                var countOpenedAt = -1d;

                for (var frame = 0; frame < 1200 && !stream.IsSpent; frame++)
                {
                    var landed = stream.HasLanded;
                    stream = stream.Advanced(delta);
                    clock += delta;

                    if (!landed && stream.HasLanded)
                    {
                        landedAt = clock;
                    }

                    if (countOpenedAt < 0d && stream.Stage == VictoryStage.Count)
                    {
                        countOpenedAt = clock - (stream.Elapsed - VictoryStages.OpensAt(VictoryStage.Count));
                    }
                }

                Assert.That(stream.IsSpent, Is.True, "fight " + fight);
                Assert.That(landedAt, Is.GreaterThanOrEqualTo(0d), "fight " + fight);
                Assert.That(countOpenedAt, Is.GreaterThanOrEqualTo(0d), "fight " + fight);

                worstLanding = Wider(
                    worstLanding,
                    landedAt - contact - VictoryStages.ClosesAt(VictoryStage.Burst));
                worstCount = Wider(
                    worstCount, countOpenedAt - contact - VictoryStages.OpensAt(VictoryStage.Count));

                carried = stream.Elapsed - VictoryStages.Seconds;
                spans.Add((float)(clock - carried - contact));
            }

            Assert.That(worstLanding, Is.LessThan(0.026f));
            Assert.That(worstCount, Is.LessThan(0.001f));

            var shortest = spans[0];
            var longest = spans[0];

            foreach (var span in spans)
            {
                shortest = span < shortest ? span : shortest;
                longest = span > longest ? span : longest;
            }

            Assert.That(spans.Count, Is.EqualTo(Consecutive));
            Assert.That(longest - shortest, Is.LessThan(0.001f));
            Assert.That(spans[0], Is.EqualTo(VictoryStages.Seconds).Within(0.001f));
            Assert.That(
                spans[spans.Count - 1], Is.EqualTo(VictoryStages.Seconds).Within(0.001f));
        }

        static LevelGraph Gauntlet()
        {
            var builder = new LevelGraphBuilder(20260828L, "ship");

            for (var x = 0; x < 7; x++)
            {
                builder.AddTile(At(x), regionId: 0);
            }

            builder.AddNode(At(0), NodeType.Start);
            builder.AddNode(At(2), NodeType.Enemy, 1);
            builder.AddNode(At(4), NodeType.Enemy, 2);
            builder.AddNode(At(6), NodeType.Enemy, 3);

            builder.Connect(At(0), At(2), new[] { At(1) });
            builder.Connect(At(2), At(4), new[] { At(3) });
            builder.Connect(At(4), At(6), new[] { At(5) });

            return builder.Build();
        }

        static TilePosition At(int x)
        {
            return new TilePosition(elevation: 0, x: x, y: 0);
        }

        static float Away(WorldPoint from, WorldPoint to)
        {
            var x = from.X - to.X;
            var y = from.Y - to.Y;
            var z = from.Z - to.Z;

            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        static float Wider(float worst, double slip)
        {
            var away = (float)Math.Abs(slip);

            return away > worst ? away : worst;
        }
    }
}
