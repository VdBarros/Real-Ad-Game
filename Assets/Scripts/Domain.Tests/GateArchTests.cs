using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class GateArchTests
    {
        const int StartingPower = 2;

        const float Tolerance = 1e-5f;

        [Test]
        public void AMultiplierEmitsAnArchAndNoBadge()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var node = Gate(graph);

            WorldPart prop;
            Assert.That(LevelBlueprintBuilder.TryProp(node, out prop), Is.True);
            Assert.That(prop.Shape, Is.EqualTo(PartShape.Gate));
            Assert.That(prop.Style, Is.EqualTo(PartStyle.Multiplier));
            Assert.That(prop.Model, Is.EqualTo(PartModel.None));

            var badges = BadgeBlueprintBuilder.Build(graph, StartingPower);

            Assert.That(badges.Badges.Select(badge => badge.NodeId), Has.No.Member(node.Id));
        }

        [Test]
        public void AnArchIsThePostsAndLintelThePlayerWalksThrough()
        {
            var pieces = GateArch.Pieces(3);
            var names = pieces.Select(piece => piece.Name).ToList();

            Assert.That(names, Contains.Item(PartNames.GateLeftPost));
            Assert.That(names, Contains.Item(PartNames.GateRightPost));
            Assert.That(names, Contains.Item(PartNames.GateLintel));
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
            Assert.That(pieces.All(piece => piece.Shape == PartShape.Cube), Is.True);
            Assert.That(pieces.All(piece => piece.Style == PartStyle.Multiplier), Is.True);
        }

        [Test]
        public void TheGapBetweenThePostsIsWiderThanTheFigureWalkingThroughIt()
        {
            Assert.That(GateArch.Walkway, Is.GreaterThan(LevelBlueprintBuilder.FigureScale));
            Assert.That(GateArch.Walkway, Is.GreaterThan(LevelBlueprintBuilder.BossScale));
            Assert.That(GateArch.Span, Is.LessThanOrEqualTo(IsoProjection.TileEdge));
        }

        [Test]
        public void TheLintelClearsTheHeadOfThePlayerWalkingUnderIt()
        {
            Assert.That(GateArch.PostHeight, Is.GreaterThan(LevelBlueprintBuilder.FigureScale * 2f));
            Assert.That(GateArch.Height, Is.GreaterThan(LevelBlueprintBuilder.PickupScale));
        }

        [Test]
        public void AnArchCountsItsFactorInPips()
        {
            for (var factor = GateArch.SmallestFactor; factor <= GateArch.MostPips; factor++)
            {
                var pieces = GateArch.Pieces(factor);

                Assert.That(GateArch.PipsOn(pieces), Is.EqualTo(factor), factor.ToString());
                Assert.That(
                    pieces.Count,
                    Is.EqualTo(factor + 3),
                    "two posts, a lintel and one pip per factor");
            }
        }

        [Test]
        public void EveryFactorTheGeneratorCanDealIsAnArchTheBuilderCanDraw()
        {
            foreach (var factor in PowerTuning.MultiplierLadder)
            {
                Assert.That(() => GateArch.Pieces(factor), Throws.Nothing, factor.ToString());
                Assert.That(() => GateLook.Of(factor), Throws.Nothing, factor.ToString());
            }
        }

        [Test]
        public void AFactorTooSmallOrTooLargeToShowIsRefusedRatherThanDrawnWrong()
        {
            Assert.That(
                () => GateArch.Pieces(GateArch.SmallestFactor - 1),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => GateArch.Pieces(GateArch.MostPips + 1),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryPipSitsOnTheLintelInOneEvenRowThatFitsTheArch()
        {
            for (var factor = GateArch.SmallestFactor; factor <= GateArch.MostPips; factor++)
            {
                var pips = GateArch.Pieces(factor).Where(piece => PartNames.IsGatePip(piece.Name)).ToList();
                var lintel = GateArch.Pieces(factor).First(piece => piece.Name == PartNames.GateLintel);
                var top = lintel.Position.Y + lintel.Scale.Y * 0.5f;

                Assert.That(GateArch.PipRowFor(factor), Is.LessThanOrEqualTo(GateArch.Span));

                for (var slot = 0; slot < pips.Count; slot++)
                {
                    Assert.That(
                        pips[slot].Position.Y - pips[slot].Scale.Y * 0.5f,
                        Is.EqualTo(top).Within(Tolerance),
                        pips[slot].Name);
                    Assert.That(
                        Math.Abs(pips[slot].Position.X) + pips[slot].Scale.X * 0.5f,
                        Is.LessThanOrEqualTo(GateArch.Span * 0.5f + Tolerance),
                        pips[slot].Name);

                    if (slot > 0)
                    {
                        Assert.That(
                            pips[slot].Position.X - pips[slot - 1].Position.X,
                            Is.EqualTo(GateArch.PipSize + GateArch.PipGap).Within(Tolerance));
                    }
                }

                Assert.That(
                    pips.Sum(pip => pip.Position.X),
                    Is.EqualTo(0f).Within(Tolerance),
                    "the row is centred on the arch");
            }
        }

        [Test]
        public void EveryPieceStandsBetweenTheFloorAndTheTopOfTheArch()
        {
            var pieces = GateArch.Pieces(4);

            foreach (var piece in pieces)
            {
                Assert.That(
                    piece.Position.Y - piece.Scale.Y * 0.5f,
                    Is.GreaterThanOrEqualTo(-GateArch.Height * 0.5f - Tolerance),
                    piece.Name);
                Assert.That(
                    piece.Position.Y + piece.Scale.Y * 0.5f,
                    Is.LessThanOrEqualTo(GateArch.Height * 0.5f + Tolerance),
                    piece.Name);
            }
        }

        [Test]
        public void AGateStandsOnItsOwnTileAndReachesTheHeightItsPiecesAddUpTo()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var node = Gate(graph);
            var tile = IsoProjection.Of(node.Position);

            WorldPart prop;
            LevelBlueprintBuilder.TryProp(node, out prop);

            Assert.That(prop.Position.X, Is.EqualTo(tile.X));
            Assert.That(prop.Position.Z, Is.EqualTo(tile.Z));
            Assert.That(prop.Position.Y - GateArch.Height * 0.5f, Is.EqualTo(tile.Y).Within(Tolerance));
            Assert.That(WorldParts.TopOf(prop), Is.EqualTo(tile.Y + GateArch.Height).Within(Tolerance));
            Assert.That(WorldParts.WidthOf(prop), Is.EqualTo(GateArch.Span).Within(Tolerance));
        }

        [Test]
        public void EveryFactorGlowsInAColourNoOtherFactorUses()
        {
            var seen = new List<Tint>();

            for (var factor = GateArch.SmallestFactor; factor <= GateArch.MostPips; factor++)
            {
                var tint = GateLook.Of(factor);

                Assert.That(seen, Has.No.Member(tint), factor.ToString());
                seen.Add(tint);
            }
        }

        [Test]
        public void AGateGlowsInNoneOfTheColoursABadgeUses()
        {
            for (var factor = GateArch.SmallestFactor; factor <= GateArch.MostPips; factor++)
            {
                foreach (BadgeStyle style in Enum.GetValues(typeof(BadgeStyle)))
                {
                    Assert.That(GateLook.Of(factor), Is.Not.EqualTo(BadgeTints.Of(style)));
                }
            }
        }

        [Test]
        public void AnUnreachableGateDimsButStaysLit()
        {
            var look = TargetMarks.Look(TargetMark.Unreachable);
            var plain = GateLook.Of(3);
            var dimmed = GateLook.Washed(3, look);

            Assert.That(GateLook.WashShare, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(dimmed, Is.Not.EqualTo(plain), "an unreachable arch reads dimmer");
            Assert.That(Brightness(dimmed), Is.LessThan(Brightness(plain)));
            Assert.That(
                Brightness(dimmed),
                Is.GreaterThan(Brightness(plain) * look.Opacity),
                "an arch is scenery lit by the world, so it takes only a share of the fade a badge plate takes");
        }

        [Test]
        public void AFadedGateHoldsTheHueTheFactorGaveIt()
        {
            foreach (var mark in new[] { TargetMark.Idle, TargetMark.Aside, TargetMark.Unreachable })
            {
                var look = TargetMarks.Look(mark);

                for (var factor = GateArch.SmallestFactor; factor <= GateArch.MostPips; factor++)
                {
                    var plain = GateLook.Of(factor);
                    var dimmed = GateLook.Washed(factor, look);
                    var dimming = GateLook.Dimming(look);

                    Assert.That(dimmed.Red, Is.EqualTo(plain.Red * dimming).Within(1e-5f), mark + " " + factor);
                    Assert.That(dimmed.Green, Is.EqualTo(plain.Green * dimming).Within(1e-5f), mark + " " + factor);
                    Assert.That(dimmed.Blue, Is.EqualTo(plain.Blue * dimming).Within(1e-5f), mark + " " + factor);
                }
            }
        }

        [Test]
        public void AnIdleGateWearsNothingButItsOwnColour()
        {
            foreach (var factor in PowerTuning.MultiplierLadder)
            {
                Assert.That(
                    GateLook.Washed(factor, TargetMarks.Look(TargetMark.Idle)),
                    Is.EqualTo(GateLook.Of(factor)),
                    factor.ToString());
            }
        }

        [Test]
        public void AnArchIsRebuiltPieceForPieceEveryTime()
        {
            Assert.That(GateArch.Pieces(3), Is.EqualTo(GateArch.Pieces(3)));
        }

        static float Brightness(Tint tint)
        {
            return tint.Red + tint.Green + tint.Blue;
        }

        static DecisionNode Gate(LevelGraph graph)
        {
            return graph.Decisions.Nodes.First(node => node.Type == NodeType.Multiplier);
        }
    }
}
