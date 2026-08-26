using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FloorSweepTests
    {
        const float Frame = 1f / 60f;

        [Test]
        public void TheWholeSweepIsOverInsideASecond()
        {
            Assert.That(FloorSweep.Seconds, Is.EqualTo(FloorSweep.SpreadSeconds + FloorSweep.FadeSeconds));
            Assert.That(FloorSweep.Seconds, Is.LessThan(1f));
        }

        [Test]
        public void NothingHasFlippedBeforeTheSweepStarts()
        {
            for (var rank = 0; rank <= 6; rank++)
            {
                Assert.That(FloorSweep.Blend(rank, 6, 0f), Is.EqualTo(0f));
                Assert.That(FloorSweep.Blend(rank, 6, -1f), Is.EqualTo(0f));
            }
        }

        [Test]
        public void EveryTileIsFullyFlippedWhenTheSweepEnds()
        {
            for (var deepest = 0; deepest <= 20; deepest++)
            {
                for (var rank = 0; rank <= deepest; rank++)
                {
                    Assert.That(
                        FloorSweep.Blend(rank, deepest, FloorSweep.Seconds),
                        Is.EqualTo(1f),
                        "Rank " + rank + " of " + deepest + " was still mid-flip when the sweep ended.");
                }
            }
        }

        [Test]
        public void ALoneTileStartsFlippingAtOnce()
        {
            Assert.That(FloorSweep.Blend(0, 0, Frame), Is.GreaterThan(0f));
            Assert.That(FloorSweep.Blend(0, 0, FloorSweep.FadeSeconds), Is.EqualTo(1f));
        }

        [Test]
        public void TheNearestTileNeverWaits()
        {
            Assert.That(FloorSweep.Blend(0, 9, Frame), Is.GreaterThan(0f));
        }

        [Test]
        public void BlendOnlyEverRises()
        {
            for (var rank = 0; rank <= 5; rank++)
            {
                var previous = 0f;

                for (var elapsed = 0f; elapsed <= FloorSweep.Seconds + Frame; elapsed += Frame)
                {
                    var blend = FloorSweep.Blend(rank, 5, elapsed);

                    Assert.That(blend, Is.GreaterThanOrEqualTo(previous));
                    Assert.That(blend, Is.InRange(0f, 1f));
                    previous = blend;
                }
            }
        }

        [Test]
        public void DeeperTilesNeverFlipAheadOfNearerOnes()
        {
            for (var elapsed = 0f; elapsed <= FloorSweep.Seconds; elapsed += Frame)
            {
                for (var rank = 1; rank <= 7; rank++)
                {
                    Assert.That(
                        FloorSweep.Blend(rank, 7, elapsed),
                        Is.LessThanOrEqualTo(FloorSweep.Blend(rank - 1, 7, elapsed)),
                        "Rank " + rank + " overtook rank " + (rank - 1) + " at " + elapsed + "s.");
                }
            }
        }

        [Test]
        public void TheSweepRunsOutwardFromTheGroundTheWalkArrivedThrough()
        {
            var opening = RunFixture.Begin(3);
            var before = FloorReading.Of(opening);
            var after = FloorReading.Of(ActionResolver.Resolve(opening, RunFixture.GateEnemy).State);
            var flipping = after.Since(before);
            var ranks = FloorSweep.Ranks(opening.Level.Tiles, flipping, before);

            Assert.That(flipping, Is.EqualTo(new[] { At(5, 0), At(6, 0) }));
            Assert.That(
                ranks,
                Is.EqualTo(new[] { 0, 1 }),
                "The enemy tile flips first, then the corridor it was standing in front of.");
        }

        [Test]
        public void TheFirstReadingOfALevelFlipsInOneWave()
        {
            var opening = RunFixture.Begin(3);
            var reading = FloorReading.Of(opening);
            var ranks = FloorSweep.Ranks(opening.Level.Tiles, reading.Cleared, FloorReading.Nothing);

            Assert.That(ranks, Is.All.EqualTo(0));
        }

        [Test]
        public void EveryFlippingTileIsRankedBehindOneThatCameBefore()
        {
            foreach (var run in FloorReadingTests.Runs(seeds: 12))
            {
                var before = FloorReading.Of(run[0]);

                for (var step = 1; step < run.Count; step++)
                {
                    var after = FloorReading.Of(run[step]);
                    var flipping = after.Since(before);
                    var ranks = FloorSweep.Ranks(run[step].Level.Tiles, flipping, before);

                    Assert.That(ranks.Count, Is.EqualTo(flipping.Count));

                    for (var index = 0; index < flipping.Count; index++)
                    {
                        Assert.That(
                            ranks[index],
                            Is.EqualTo(StepsFromTheOldGround(run[step].Level.Tiles, flipping, before, flipping[index])),
                            flipping[index] + " was ranked off the wave that reaches it.");
                    }

                    before = after;
                }
            }
        }

        static int StepsFromTheOldGround(
            TileGrid grid,
            IReadOnlyList<TilePosition> flipping,
            FloorReading before,
            TilePosition target)
        {
            var pending = new HashSet<TilePosition>(flipping);
            var wave = new List<TilePosition>();

            foreach (var position in flipping)
            {
                foreach (var neighbour in grid.Neighbours(position))
                {
                    if (!before.IsCleared(neighbour))
                    {
                        continue;
                    }

                    wave.Add(position);
                    pending.Remove(position);
                    break;
                }
            }

            for (var rank = 0; wave.Count > 0; rank++)
            {
                var next = new List<TilePosition>();

                foreach (var position in wave)
                {
                    if (position.Equals(target))
                    {
                        return rank;
                    }

                    foreach (var neighbour in grid.Neighbours(position))
                    {
                        if (pending.Remove(neighbour))
                        {
                            next.Add(neighbour);
                        }
                    }
                }

                wave = next;
            }

            throw new AssertionException("No wave reaches " + target + ".");
        }

        static TilePosition At(int x, int y)
        {
            return new TilePosition(elevation: 0, x: x, y: y);
        }
    }
}
