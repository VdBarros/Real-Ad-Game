using System;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeSizingTests
    {
        const int StartingPower = 2;

        const float Tolerance = 1e-5f;

        [Test]
        public void TheCeilingIsEveryGainTakenBeforeEveryMultiplier()
        {
            var ceiling = PlayerPowerCeiling.Of(LevelGraphFixture.TwoFloors(), StartingPower);

            Assert.That(ceiling, Is.EqualTo((2 + 12 + 4 + 30) * 3));
        }

        [Test]
        public void NoRunCanOutgrowTheCeiling()
        {
            var graph = LevelGraphFixture.TwoFloors();
            var ceiling = PlayerPowerCeiling.Of(graph, StartingPower);
            var random = new Random(20250824);

            for (var attempt = 0; attempt < 200; attempt++)
            {
                var state = RunState.Begin(graph, StartingPower);

                for (var step = 0; step < graph.Decisions.Nodes.Count; step++)
                {
                    var reachable = state.ReachableNodes;
                    if (reachable.Count == 0 || state.IsLevelComplete)
                    {
                        break;
                    }

                    state = ActionResolver.Resolve(state, reachable[random.Next(reachable.Count)]).State;
                    Assert.That(state.Power, Is.LessThanOrEqualTo(ceiling));
                }
            }
        }

        [Test]
        public void CapacityHoldsTheWidestBadgeTheLevelCanShow()
        {
            var graph = LevelGraphFixture.TwoFloors();
            var plan = BadgePlan.For(graph, StartingPower);

            Assert.That(plan.Capacity, Is.EqualTo(3));
            Assert.That(BadgeText.Digits(plan.PowerCeiling), Is.LessThanOrEqualTo(plan.Capacity));

            foreach (var node in graph.Decisions.Nodes)
            {
                BadgeStyle style;
                if (!BadgeStyles.TryOf(node.Type, out style) || style == BadgeStyle.Player)
                {
                    continue;
                }

                Assert.That(BadgeText.Cells(style, node.Value), Is.LessThanOrEqualTo(plan.Capacity));
            }
        }

        [Test]
        public void AGeneratedShipLevelStaysInsideTheDesignSize()
        {
            var level = LevelGenerator.Generate(20250824L, MazePreset.Ship);
            var plan = BadgePlan.For(level.Graph, PowerTuning.Ship.StartingPower);

            Assert.That(plan.Capacity, Is.LessThanOrEqualTo(6));
            Assert.That(plan.PlayerWidth, Is.LessThanOrEqualTo(2f));
            Assert.That(plan.FontSize, Is.GreaterThan(0f));
        }

        [Test]
        public void TheTextIsSizedToFitTheCapacityItWasPlannedFor()
        {
            for (var capacity = 1; capacity <= 8; capacity++)
            {
                var width = BadgeMetrics.WidthFor(capacity);
                var fontSize = BadgeMetrics.FontSizeFor(capacity);
                var textWidth = capacity * BadgeMetrics.MonospaceEm * fontSize * BadgeMetrics.UnitsPerFontPoint;
                var textHeight = BadgeMetrics.CapHeightEm * fontSize * BadgeMetrics.UnitsPerFontPoint;

                Assert.That(textWidth, Is.LessThanOrEqualTo(width - 2f * BadgeMetrics.SidePadding + Tolerance));
                Assert.That(
                    textHeight,
                    Is.LessThanOrEqualTo(BadgeMetrics.Height - 2f * BadgeMetrics.VerticalPadding + Tolerance));
            }
        }

        [Test]
        public void NoShipSeedNeedsMoreThanTheDesignSize()
        {
            const int Seeds = 300;

            var byCapacity = new int[16];
            var widest = 0;
            var dearest = 0L;

            for (var seed = 0; seed < Seeds; seed++)
            {
                var level = LevelGenerator.Generate(seed, MazePreset.Ship);
                var plan = BadgePlan.For(level.Graph, PowerTuning.Ship.StartingPower);

                byCapacity[plan.Capacity]++;
                widest = Math.Max(widest, plan.Capacity);
                dearest = Math.Max(dearest, plan.PowerCeiling);

                foreach (var node in level.Graph.Decisions.Nodes)
                {
                    BadgeStyle style;
                    if (!BadgeStyles.TryOf(node.Type, out style) || style == BadgeStyle.Player)
                    {
                        continue;
                    }

                    Assert.That(
                        BadgeText.Cells(style, node.Value),
                        Is.LessThanOrEqualTo(plan.Capacity),
                        "Seed " + seed + " mints " + BadgeText.Of(style, node.Value));
                }
            }

            Console.WriteLine("ship, " + Seeds + " seeds");
            for (var capacity = 1; capacity < byCapacity.Length; capacity++)
            {
                if (byCapacity[capacity] > 0)
                {
                    Console.WriteLine(
                        "  " + capacity + " glyph cells        " + byCapacity[capacity]
                        + " seeds, " + BadgeMetrics.WidthFor(capacity).ToString("0.###") + " units wide");
                }
            }

            Console.WriteLine("  dearest power ceiling " + dearest);

            Assert.That(widest, Is.LessThanOrEqualTo(6));
        }

        [Test]
        public void AWiderCapacityOnlyEverWidensTheBadge()
        {
            var widths = Enumerable.Range(1, 8).Select(BadgeMetrics.WidthFor).ToList();

            Assert.That(widths, Is.Ordered.Ascending);
            Assert.That(Enumerable.Range(1, 8).Select(BadgeMetrics.FontSizeFor).Distinct().Count(), Is.EqualTo(1));
        }
    }
}
