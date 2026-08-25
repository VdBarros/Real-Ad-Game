using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class EnemyBandTests
    {
        const long Seed = 20250824L;

        [Test]
        public void AnEnemyLooksBeatableExactlyWhenThePlayerIsStrictlyStronger()
        {
            for (var power = 1; power <= 200; power++)
            {
                for (var value = 1; value <= 200; value++)
                {
                    Assert.That(
                        EnemyBands.IsBeatable(EnemyBands.Of(value, power)),
                        Is.EqualTo(power > value),
                        $"power {power} against enemy {value}");
                }
            }
        }

        [Test]
        public void ATieSitsInTheBandBelowEdibleBecauseATieLeavesTheCorridorShut()
        {
            Assert.That(EnemyBands.Of(40, 40), Is.EqualTo(EnemyBand.Close));
            Assert.That(EnemyBands.Of(39, 40), Is.EqualTo(EnemyBand.Edible));
        }

        [Test]
        public void HalfThePlayersPowerIsTriviallyWeakAndAHairMoreIsMerelyEdible()
        {
            Assert.That(EnemyBands.Of(20, 40), Is.EqualTo(EnemyBand.Trivial));
            Assert.That(EnemyBands.Of(21, 40), Is.EqualTo(EnemyBand.Edible));
        }

        [Test]
        public void TwiceThePlayersPowerIsStillCloseAndAHairMoreIsOutOfReach()
        {
            Assert.That(EnemyBands.Of(80, 40), Is.EqualTo(EnemyBand.Close));
            Assert.That(EnemyBands.Of(81, 40), Is.EqualTo(EnemyBand.OutOfReach));
        }

        [Test]
        public void ABandNeverWeakensAsTheEnemyGrowsOrAsThePlayerShrinks()
        {
            var previous = EnemyBand.Trivial;
            for (var value = 1; value <= 300; value++)
            {
                var band = EnemyBands.Of(value, 50);
                Assert.That((int)band, Is.GreaterThanOrEqualTo((int)previous));
                previous = band;
            }

            previous = EnemyBand.OutOfReach;
            for (var power = 1; power <= 300; power++)
            {
                var band = EnemyBands.Of(50, power);
                Assert.That((int)band, Is.LessThanOrEqualTo((int)previous));
                previous = band;
            }
        }

        [Test]
        public void APowerThatWouldOverflowAnIntStillBands()
        {
            Assert.That(EnemyBands.Of(1, int.MaxValue), Is.EqualTo(EnemyBand.Trivial));
            Assert.That(EnemyBands.Of(int.MaxValue, 1), Is.EqualTo(EnemyBand.OutOfReach));
        }

        [Test]
        public void ABandOnlyExistsForAnEnemyAndAPlayerThatBothHoldPower()
        {
            Assert.That(() => EnemyBands.Of(0, 10), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => EnemyBands.Of(10, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryBandHasItsOwnLookAndTheOrderReadsAsRisingThreat()
        {
            var bands = (EnemyBand[])Enum.GetValues(typeof(EnemyBand));
            var tints = bands.Select(EnemyBands.TintOf).ToList();
            var scales = bands.Select(EnemyBands.ScaleOf).ToList();

            Assert.That(tints.Distinct().Count(), Is.EqualTo(bands.Length));

            for (var index = 1; index < bands.Length; index++)
            {
                Assert.That(scales[index], Is.GreaterThan(scales[index - 1]));
            }
        }

        [Test]
        public void TheBoardRereadsAsThePlayerGrowsRatherThanCollapsingIntoOneLook()
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var values = graph.Decisions.Nodes
                .Where(node => node.Type == NodeType.Enemy || node.Type == NodeType.Boss)
                .Select(node => node.Value)
                .ToList();

            Assert.That(values, Is.Not.Empty);

            var seen = new HashSet<string>();
            foreach (var power in new[] { PowerTuning.Ship.StartingPower, 20, 80, 240, 600 })
            {
                seen.Add(string.Join(",", values.Select(value => (int)EnemyBands.Of(value, power))));
            }

            Assert.That(seen.Count, Is.EqualTo(5));
        }

        [Test]
        public void OnceThePlayerHasOutgrownTheLevelNothingIsLeftOutOfReach()
        {
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var ceiling = (int)PowerCeiling.Of(graph, PowerTuning.Ship.StartingPower);

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Enemy && node.Type != NodeType.Boss)
                {
                    continue;
                }

                Assert.That(EnemyBands.IsBeatable(EnemyBands.Of(node.Value, ceiling)), Is.True);
            }
        }
    }
}
