using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeSizingTests
    {
        const int StartingPower = 2;

        const float Tolerance = 1e-5f;

        static readonly string[] BadgeSizingSources =
        {
            "BadgeMetrics.cs",
            "BadgeFit.cs",
            "BadgeSize.cs",
            "BadgePart.cs",
            "BadgePlan.cs",
            "BadgeBlueprintBuilder.cs",
            "BadgeText.cs",
            "CountUp.cs"
        };

        [Test]
        public void TheCeilingIsEveryGainTakenBeforeEveryMultiplier()
        {
            var ceiling = PowerCeiling.Of(LevelGraphFixture.TwoTerraces(), StartingPower);

            Assert.That(ceiling, Is.EqualTo((2 + 12 + 4 + 30) * 3));
        }

        [Test]
        public void NoRunCanOutgrowTheCeiling()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var ceiling = PowerCeiling.Of(graph, StartingPower);
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
        public void NoBadgeSizingSourceSoMuchAsMentionsThePowerCeiling()
        {
            foreach (var source in BadgeSizingSources)
            {
                Assert.That(
                    SourceTree.Read("Presentation.Pure", source),
                    Does.Not.Contain("PowerCeiling"),
                    source + " sizes a badge from the power ceiling.");
            }
        }

        [Test]
        public void CapacityHoldsTheWidestBadgeTheLevelActuallyShows()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);

            Assert.That(blueprint.Plan.Capacity, Is.EqualTo(3));
            Assert.That(
                blueprint.Badges.Max(badge => badge.Cells),
                Is.EqualTo(blueprint.Plan.Capacity));
        }

        [Test]
        public void ThePlayerBadgeIsSizedForTheNumberItShowsNotTheWidestOnTheLevel()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);
            var player = blueprint.Badges.Single(badge => badge.Style == BadgeStyle.Player);

            Assert.That(player.Cells, Is.EqualTo(BadgeText.Digits(StartingPower)));
            Assert.That(player.Cells, Is.LessThan(blueprint.Plan.Capacity));
            Assert.That(player.Width, Is.LessThan(BadgeMetrics.WidthFor(blueprint.Plan.Capacity)));
        }

        [Test]
        public void NoBadgeOnAGeneratedLevelIsWiderThanTheThingItLabels()
        {
            const int Seeds = 60;

            for (var seed = 0; seed < Seeds; seed++)
            {
                var level = LevelGenerator.Generate(seed, MazePreset.Ship);
                var blueprint = BadgeBlueprintBuilder.Build(level.Graph, PowerTuning.Ship.StartingPower);

                foreach (var badge in blueprint.Badges)
                {
                    WorldPart prop;
                    Assert.That(
                        LevelBlueprintBuilder.TryProp(level.Graph.Decisions.Node(badge.NodeId), out prop),
                        Is.True);

                    Assert.That(
                        badge.Width,
                        Is.LessThanOrEqualTo(WorldParts.WidthOf(prop) + Tolerance),
                        "Seed " + seed + " " + badge);
                    Assert.That(badge.Size.Cells, Is.GreaterThanOrEqualTo(BadgeMetrics.MinimumCells), badge.ToString());
                    Assert.That(badge.Size.FontSize, Is.GreaterThan(0f), badge.ToString());
                }
            }
        }

        [Test]
        public void ThePlayerBadgeStartsNoWiderThanTheCharacterItSitsOver()
        {
            var level = LevelGenerator.Generate(20250824L, MazePreset.Ship);
            var blueprint = BadgeBlueprintBuilder.Build(level.Graph, PowerTuning.Ship.StartingPower);
            var player = blueprint.Badges.Single(badge => badge.Style == BadgeStyle.Player);

            Assert.That(player.Width, Is.LessThanOrEqualTo(player.SubjectWidth + Tolerance));
            Assert.That(player.SubjectWidth, Is.GreaterThan(0f));
            Assert.That(blueprint.Plan.FontSize, Is.GreaterThan(0f));

            Console.WriteLine(
                "ship 20250824: player badge " + player.Width.ToString("0.###")
                + " over a character " + player.SubjectWidth.ToString("0.###")
                + " wide, was " + BadgeMetrics.WidthFor(blueprint.Plan.Capacity).ToString("0.###"));
        }

        [Test]
        public void TheTextIsSizedToFitTheCapacityItWasPlannedFor()
        {
            for (var capacity = 1; capacity <= 8; capacity++)
            {
                var width = BadgeMetrics.WidthFor(capacity);
                var fontSize = BadgeMetrics.FontSize;
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

            for (var seed = 0; seed < Seeds; seed++)
            {
                var level = LevelGenerator.Generate(seed, MazePreset.Ship);
                var plan = BadgePlan.For(level.Graph, PowerTuning.Ship.StartingPower);

                byCapacity[plan.Capacity]++;
                widest = Math.Max(widest, plan.Capacity);

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

            Assert.That(widest, Is.LessThanOrEqualTo(5));
        }

        [Test]
        public void AWiderCapacityOnlyEverWidensTheBadge()
        {
            var widths = Enumerable.Range(1, 8).Select(cells => BadgeMetrics.WidthFor(cells)).ToList();

            Assert.That(widths, Is.Ordered.Ascending);
        }

        [Test]
        public void TheNineSliceBordersFitInsideTheNarrowestBadgeTheyAreDrawnOn()
        {
            var narrowest = BadgeMetrics.MinimumWidth;

            foreach (var shape in new[] { BadgeShape.RoundedRect, BadgeShape.Pill })
            {
                var border = BadgeShapeField.BorderOf(shape) / BadgeShapeField.PixelsPerUnit;

                Assert.That(2f * border, Is.LessThan(BadgeMetrics.Height), shape.ToString());
                Assert.That(2f * border, Is.LessThan(narrowest), shape.ToString());
            }
        }

        [Test]
        public void ThePlayerBadgeWidensWithEveryDigitItGainsAndNeverJittersOnTheWay()
        {
            var beat = PowerBeat.Begin(2);
            var widths = new List<float>();
            var biggestStep = 0f;
            var previous = WidthOf(beat);

            widths.Add(previous);

            foreach (var target in new[] { 42, 640, 5000 })
            {
                beat = beat.Toward(target);

                var before = previous;
                Assert.That(
                    Math.Abs(WidthOf(beat) - before),
                    Is.LessThan(Tolerance),
                    "Retargeting to " + target + " snapped the width.");

                for (var frame = 0; frame < 600 && !beat.IsSettled; frame++)
                {
                    var counting = !beat.HasLanded;
                    beat = beat.Advanced(1f / 60f);

                    var width = WidthOf(beat);
                    var step = width - previous;

                    if (counting)
                    {
                        Assert.That(
                            step, Is.GreaterThanOrEqualTo(-Tolerance), "The width jittered mid-count at " + beat);
                    }

                    Assert.That(
                        width, Is.GreaterThanOrEqualTo(before - Tolerance), "The width fell back at " + beat);
                    biggestStep = step > biggestStep ? step : biggestStep;
                    previous = width;
                }

                Assert.That(beat.IsSettled, Is.True, target.ToString());
                widths.Add(previous);
            }

            for (var step = 1; step < widths.Count; step++)
            {
                Assert.That(widths[step], Is.GreaterThanOrEqualTo(widths[step - 1] - Tolerance), step.ToString());
            }

            Assert.That(widths[1], Is.GreaterThan(widths[0]));
            Assert.That(biggestStep, Is.LessThan(BadgeMetrics.CellWidth * 0.5f));
        }

        [Test]
        public void TheCharacterHoldsItsSizeWhileTheNumberIsMidCountSoNothingClampingTheWidthMoves()
        {
            var beat = PowerBeat.Begin(2).Toward(640);
            var held = Character(beat);
            var frames = 0;

            while (!beat.HasLanded && frames < 600)
            {
                beat = beat.Advanced(1f / 60f);
                frames++;

                if (!beat.HasLanded)
                {
                    Assert.That(Character(beat), Is.EqualTo(held), beat.ToString());
                }
            }

            Assert.That(frames, Is.GreaterThan(1));
            Assert.That(beat.HasLanded, Is.True);
        }

        [Test]
        public void ASettledPlayerBadgeIsExactlyTheWidthOfTheDigitsOnIt()
        {
            const float Roomy = 10f;

            var beat = PowerBeat.Begin(2);

            foreach (var target in new[] { 7, 55, 900 })
            {
                beat = beat.Toward(target);
                for (var frame = 0; frame < 600 && !beat.IsSettled; frame++)
                {
                    beat = beat.Advanced(1f / 60f);
                }

                Assert.That(beat.Digits, Is.EqualTo(BadgeText.Digits(target)));
                Assert.That(
                    BadgeFit.Of(beat.Digits, Roomy).Width,
                    Is.EqualTo(BadgeMetrics.WidthFor(BadgeText.Digits(target))).Within(Tolerance),
                    target.ToString());
            }
        }

        static float WidthOf(PowerBeat beat)
        {
            return BadgeFit.Of(beat.Digits, Character(beat)).Width;
        }

        static float Character(PowerBeat beat)
        {
            return FigureFit.WidthOf(PartModel.Knight, beat.Scale);
        }
    }
}
