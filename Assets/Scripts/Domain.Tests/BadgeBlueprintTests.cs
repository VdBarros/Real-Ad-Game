using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeBlueprintTests
    {
        const int StartingPower = 2;

        const float Tolerance = 1e-5f;

        [Test]
        public void EveryContentNodeWearsOneBadgeAndNothingElseDoes()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);

            var wearers = graph.Decisions.Nodes
                .Where(node => node.Type != NodeType.Empty)
                .Select(node => node.Id)
                .ToList();

            Assert.That(blueprint.Badges.Select(badge => badge.NodeId), Is.EqualTo(wearers));
            Assert.That(
                blueprint.Badges.Select(badge => badge.Name),
                Is.EqualTo(wearers.Select(PartNames.Badge).ToList()));
        }

        [Test]
        public void ThePrefixIsTheOnlyDifferenceBetweenAValueAndItsBadge()
        {
            var blueprint = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);

            Assert.That(TextOn(blueprint, NodeType.Additive), Is.EqualTo("+12"));
            Assert.That(TextOn(blueprint, NodeType.Multiplier), Is.EqualTo("x3"));
            Assert.That(TextOn(blueprint, NodeType.Enemy), Is.EqualTo("4"));
            Assert.That(TextOn(blueprint, NodeType.Boss), Is.EqualTo("30"));
            Assert.That(TextOn(blueprint, NodeType.Start), Is.EqualTo("2"));
        }

        [Test]
        public void EnemiesWearAPillAndEverythingElseARoundedRect()
        {
            var blueprint = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);

            foreach (var badge in blueprint.Badges)
            {
                var expected = badge.Style == BadgeStyle.Enemy || badge.Style == BadgeStyle.Boss
                    ? BadgeShape.Pill
                    : BadgeShape.RoundedRect;

                Assert.That(badge.Shape, Is.EqualTo(expected), badge.ToString());
            }
        }

        [Test]
        public void EveryBadgeCarriesTheCameraRotationRatherThanFacingTheCameraItself()
        {
            var blueprint = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);

            Assert.That(
                blueprint.Badges.Select(badge => badge.Rotation).Distinct().ToList(),
                Is.EqualTo(new[] { IsoProjection.CameraRotation }));
            Assert.That(IsoProjection.CameraRotation, Is.EqualTo(new WorldPoint(30f, 45f, 0f)));
        }

        [Test]
        public void ABadgeClearsTheTopOfItsOwnNode()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);

            foreach (var badge in blueprint.Badges)
            {
                WorldPart prop;
                Assert.That(LevelBlueprintBuilder.TryProp(graph.Decisions.Node(badge.NodeId), out prop), Is.True);

                var bottom = badge.Position.Y - blueprint.Plan.Height * 0.5f;

                Assert.That(
                    bottom - WorldParts.TopOf(prop),
                    Is.EqualTo(BadgeMetrics.Clearance).Within(Tolerance),
                    badge.ToString());
                Assert.That(badge.Position.X, Is.EqualTo(IsoProjection.Of(graph.Decisions.Node(badge.NodeId).Position).X));
                Assert.That(badge.Position.Z, Is.EqualTo(IsoProjection.Of(graph.Decisions.Node(badge.NodeId).Position).Z));
            }
        }

        [Test]
        public void ABadgeHangsOnTheTerraceItsNodeStandsOn()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);

            var elevations = new HashSet<int>();
            foreach (var badge in blueprint.Badges)
            {
                var node = graph.Decisions.Node(badge.NodeId);

                Assert.That(badge.Elevation, Is.EqualTo(node.Position.Elevation), badge.ToString());
                Assert.That(
                    badge.Position.Y,
                    Is.GreaterThan(IsoProjection.Of(node.Position).Y),
                    badge.ToString());
                elevations.Add(badge.Elevation);
            }

            Assert.That(elevations, Is.EquivalentTo(new[] { 0, 2 }));
        }

        [Test]
        public void OnlyThePlayerBadgeIsSizedForTheLargestValueTheLevelCanHold()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = BadgeBlueprintBuilder.Build(graph, StartingPower);

            foreach (var badge in blueprint.Badges)
            {
                if (badge.Style == BadgeStyle.Player)
                {
                    Assert.That(badge.Cells, Is.EqualTo(blueprint.Plan.Capacity), badge.ToString());
                    Assert.That(badge.Width, Is.EqualTo(blueprint.Plan.PlayerWidth), badge.ToString());
                    continue;
                }

                Assert.That(badge.Cells, Is.EqualTo(badge.Text.Length), badge.ToString());
                Assert.That(badge.Cells, Is.LessThanOrEqualTo(blueprint.Plan.Capacity), badge.ToString());
            }
        }

        [Test]
        public void RebuildingFromTheSameGraphIsIdentical()
        {
            var first = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);
            var second = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);

            Assert.That(second.Badges, Is.EqualTo(first.Badges));
        }

        [Test]
        public void BuildingFromAGraphAssembledBackwardsIsIdentical()
        {
            var forwards = BadgeBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces(), StartingPower);
            var backwards = BadgeBlueprintBuilder.Build(
                LevelGraphFixture.TwoTerracesAssembledBackwards(), StartingPower);

            Assert.That(backwards.Badges, Is.EqualTo(forwards.Badges));
        }

        static string TextOn(BadgeBlueprint blueprint, NodeType type)
        {
            BadgeStyle style;
            Assert.That(BadgeStyles.TryOf(type, out style), Is.True);

            var matches = blueprint.Badges.Where(badge => badge.Style == style).Select(badge => badge.Text).ToList();
            Assert.That(matches, Has.Count.EqualTo(1));
            return matches[0];
        }
    }
}
