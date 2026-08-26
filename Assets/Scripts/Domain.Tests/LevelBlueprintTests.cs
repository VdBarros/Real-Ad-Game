using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelBlueprintTests
    {
        const double QuadFacesNegativeZ = -1.0;

        const float Tolerance = 1e-5f;

        [Test]
        public void ProjectionPutsATileOnTheFixedLattice()
        {
            var world = IsoProjection.Of(new TilePosition(2, 3, 4));

            Assert.That(world.X, Is.EqualTo(3f));
            Assert.That(world.Y, Is.EqualTo(2f));
            Assert.That(world.Z, Is.EqualTo(4f));
        }

        [Test]
        public void TheCameraIsAPerPresetConstant()
        {
            Assert.That(IsoProjection.CameraPitch, Is.EqualTo(30f));
            Assert.That(IsoProjection.CameraYaw, Is.EqualTo(45f));
            Assert.That(IsoProjection.CameraRoll, Is.EqualTo(0f));
            Assert.That(IsoProjection.OrthographicSize, Is.EqualTo(9.5f));
        }

        [Test]
        public void RebuildingFromTheSameGraphIsIdentical()
        {
            var first = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
            var second = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            Assert.That(second.AllParts, Is.EqualTo(first.AllParts));
        }

        [Test]
        public void BuildingFromAGraphAssembledBackwardsIsIdentical()
        {
            var forwards = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
            var backwards = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerracesAssembledBackwards());

            Assert.That(backwards.AllParts, Is.EqualTo(forwards.AllParts));
        }

        [Test]
        public void TerracesRunFromTheGroundUp()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            Assert.That(blueprint.Terraces.Select(terrace => terrace.Elevation), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(
                blueprint.Terraces.Select(terrace => terrace.Name),
                Is.EqualTo(new[] { "Terrace_0", "Terrace_2" }));
        }

        [Test]
        public void EveryTileGetsOneFloorQuad()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            var quads = PartsStyled(blueprint, PartStyle.Floor);

            Assert.That(quads.Count, Is.EqualTo(graph.Tiles.Tiles.Count));
            Assert.That(
                quads.Select(part => part.Name),
                Is.EqualTo(graph.Tiles.Tiles.Select(tile => PartNames.Tile(tile.Position)).ToList()));
            Assert.That(quads.All(part => part.Shape == PartShape.Quad), Is.True);
        }

        [Test]
        public void FloorQuadsLieFlatOnTheirTile()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            var quad = PartsStyled(blueprint, PartStyle.Floor).First(part => part.Name == PartNames.Tile(new TilePosition(2, 6, 5)));

            Assert.That(quad.Position, Is.EqualTo(new WorldPoint(6f, 2f, 5f)));
            Assert.That(quad.Rotation, Is.EqualTo(new WorldPoint(90f, 0f, 0f)));
            Assert.That(quad.Scale, Is.EqualTo(new WorldPoint(1f, 1f, 1f)));
        }

        [Test]
        public void SiblingOrderIsTheElevationYXSweep()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var terrace in blueprint.Terraces)
            {
                var swept = graph.Tiles.Tiles
                    .Where(tile => Terraces.ElevationOf(
                        Terraces.TerraceUnder(tile.Position.Elevation)) == terrace.Elevation)
                    .Select(tile => PartNames.Tile(tile.Position))
                    .ToList();

                var built = terrace.Tiles
                    .Where(part => part.Style == PartStyle.Floor)
                    .Select(part => part.Name)
                    .ToList();

                Assert.That(built, Is.EqualTo(swept));
            }
        }

        [Test]
        public void NodePropsFollowTheirNodeIds()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            var expected = graph.Decisions.Nodes
                .Where(node => node.Type != NodeType.Empty)
                .Select(node => PartNames.Node(node.Id))
                .ToList();

            var built = blueprint.Terraces.SelectMany(terrace => terrace.Nodes).Select(part => part.Name).ToList();

            Assert.That(built, Is.EqualTo(expected));
        }

        [Test]
        public void WallsStandOnAbsentNeighboursAndNowhereElse()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            var walls = new HashSet<string>(PartsStyled(blueprint, PartStyle.Wall).Select(part => part.Name));
            var expected = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var neighbour = TileSides.Step(tile.Position, side);
                    var name = PartNames.Wall(tile.Position, side);

                    if (graph.Tiles.ContainsPlace(neighbour.X, neighbour.Y))
                    {
                        Assert.That(walls.Contains(name), Is.False, name + " walls off a tile that is there.");
                    }
                    else
                    {
                        Assert.That(walls.Contains(name), Is.True, name + " is missing.");
                        expected++;
                    }
                }
            }

            Assert.That(walls.Count, Is.EqualTo(expected));
        }

        [Test]
        public void AWallStandsHalfwayBetweenItsTileAndTheAbsentNeighbour()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            var wall = PartsStyled(blueprint, PartStyle.Wall)
                .First(part => part.Name == PartNames.Wall(new TilePosition(0, 1, 0), TileSide.South));

            Assert.That(wall.Shape, Is.EqualTo(PartShape.Quad));
            Assert.That(wall.Position, Is.EqualTo(new WorldPoint(1f, 0.5f, -0.5f)));
            Assert.That(wall.Scale, Is.EqualTo(new WorldPoint(1f, 1f, 1f)));
        }

        [Test]
        public void EveryWallShowsItsFaceToTheTileItBelongsTo()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        continue;
                    }

                    var name = PartNames.Wall(tile.Position, side);
                    var wall = PartsStyled(blueprint, PartStyle.Wall).First(part => part.Name == name);
                    var centre = IsoProjection.Of(tile.Position);
                    var facing = QuadNormal(wall.Rotation);

                    Assert.That(
                        facing.X * (centre.X - wall.Position.X) + facing.Z * (centre.Z - wall.Position.Z),
                        Is.GreaterThan(0f),
                        name + " turns its back on the tile it walls off.");
                }
            }
        }

        [Test]
        public void EveryFloorQuadShowsItsFaceUpwards()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            foreach (var quad in PartsStyled(blueprint, PartStyle.Floor))
            {
                Assert.That(QuadNormal(quad.Rotation).Y, Is.EqualTo(1f).Within(Tolerance), quad.Name);
            }
        }

        [Test]
        public void EmptyNodesInstantiateNothing()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            var emptyNames = graph.Decisions.Nodes
                .Where(node => node.Type == NodeType.Empty)
                .Select(node => PartNames.Node(node.Id))
                .ToList();

            Assert.That(emptyNames, Is.Not.Empty);
            Assert.That(blueprint.AllParts.Select(part => part.Name).Intersect(emptyNames), Is.Empty);
        }

        [Test]
        public void PropsFollowTheNodeTypeTable()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            AssertProp(graph, blueprint, NodeType.Start, PartShape.Capsule, PartStyle.Start);
            AssertProp(graph, blueprint, NodeType.Enemy, PartShape.Capsule, PartStyle.Enemy);
            AssertProp(graph, blueprint, NodeType.Boss, PartShape.Capsule, PartStyle.Boss);
            AssertProp(graph, blueprint, NodeType.Additive, PartShape.Cube, PartStyle.Additive);
            AssertProp(graph, blueprint, NodeType.Multiplier, PartShape.Cube, PartStyle.Multiplier);
        }

        [Test]
        public void TheBossStandsTallerThanAnEnemy()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            var boss = Prop(graph, blueprint, NodeType.Boss);
            var enemy = Prop(graph, blueprint, NodeType.Enemy);

            Assert.That(boss.Scale.Y, Is.GreaterThan(enemy.Scale.Y));
        }

        [Test]
        public void AMultiplierIsACubeTurnedFortyFiveDegrees()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            Assert.That(Prop(graph, blueprint, NodeType.Multiplier).Rotation, Is.EqualTo(new WorldPoint(0f, 45f, 0f)));
            Assert.That(Prop(graph, blueprint, NodeType.Additive).Rotation, Is.EqualTo(new WorldPoint(0f, 0f, 0f)));
        }

        [Test]
        public void EveryPropStandsOnTopOfItsTile()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Empty)
                {
                    continue;
                }

                var prop = blueprint.AllParts.First(part => part.Name == PartNames.Node(node.Id));
                var tile = IsoProjection.Of(node.Position);

                Assert.That(prop.Position.X, Is.EqualTo(tile.X));
                Assert.That(prop.Position.Z, Is.EqualTo(tile.Z));
                Assert.That(prop.Position.Y, Is.GreaterThan(tile.Y));
            }
        }

        [Test]
        public void EveryPartHangsUnderATerraceGroup()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            var grouped = blueprint.Terraces
                .SelectMany(terrace => terrace.Tiles.Concat(terrace.Nodes))
                .Count();

            Assert.That(grouped, Is.EqualTo(blueprint.AllParts.Count));
            Assert.That(blueprint.AllParts, Is.Not.Empty);
        }

        [Test]
        public void PartNamesAreUnique()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            var names = blueprint.AllParts.Select(part => part.Name).ToList();

            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
        }

        [Test]
        public void AShipLevelIsAboutSixtyQuadsAndAHundredAndTwentyWalls()
        {
            var level = LevelGenerator.Generate(20250824L, MazePreset.Ship);
            var blueprint = LevelBlueprintBuilder.Build(level.Graph);

            Assert.That(PartsStyled(blueprint, PartStyle.Floor).Count, Is.EqualTo(60).Within(15));
            Assert.That(PartsStyled(blueprint, PartStyle.Wall).Count, Is.EqualTo(120).Within(30));
        }

        [Test]
        public void NoTerraceCrowdsTheHeadroomAProfileAndItsBadgeNeedOnTheTerraceBelow()
        {
            var needed = HeadroomATallPropAndItsBadgeNeed();
            var tightest = double.MaxValue;
            var tightestWhere = string.Empty;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                for (var seed = 1L; seed <= 40L; seed++)
                {
                    var tiles = LevelGenerator.Generate(seed, preset).Graph.Tiles.Tiles;

                    foreach (var below in tiles)
                    {
                        foreach (var above in tiles)
                        {
                            if (!SharesAScreenColumn(below.Position, above.Position)
                                || !StandsOnAHigherTerrace(below.Position, above.Position))
                            {
                                continue;
                            }

                            var separation = ScreenUp(above.Position) - ScreenUp(below.Position);

                            Assert.That(
                                separation,
                                Is.GreaterThan(needed),
                                "On " + preset + " seed " + seed + ", the tile at " + above.Position
                                + " sits only " + separation + " above " + below.Position
                                + " on screen, which is inside the " + needed
                                + " a boss and its badge stand in.");

                            if (separation < tightest)
                            {
                                tightest = separation;
                                tightestWhere = preset + " seed " + seed + " " + below.Position
                                    + " under " + above.Position;
                            }
                        }
                    }
                }
            }

            Console.WriteLine(
                "terrace headroom: a boss and its badge stand " + needed
                + " up the screen; the tightest column anywhere leaves " + tightest + " at " + tightestWhere);
        }

        [Test]
        public void AShipLevelRebuildsIdentically()
        {
            var level = LevelGenerator.Generate(20250824L, MazePreset.Ship);

            Assert.That(
                LevelBlueprintBuilder.Build(level.Graph).AllParts,
                Is.EqualTo(LevelBlueprintBuilder.Build(level.Graph).AllParts));
        }

        static double HeadroomATallPropAndItsBadgeNeed()
        {
            WorldPart tallest;
            LevelBlueprintBuilder.TryProp(
                new DecisionNode(0, new TilePosition(0, 0, 0), NodeType.Boss, 1), out tallest);

            var badgeAnchor = BadgeMetrics.AnchorAbove(WorldParts.TopOf(tallest));

            return badgeAnchor * IsoProjection.CameraUp.Y
                + BadgeMetrics.Height * 0.5
                + IsoProjection.TileEdge * IsoProjection.CameraUp.X;
        }

        static bool SharesAScreenColumn(TilePosition below, TilePosition above)
        {
            return below.X - below.Y == above.X - above.Y;
        }

        static bool StandsOnAHigherTerrace(TilePosition below, TilePosition above)
        {
            return Terraces.IsTerrace(below.Elevation)
                && Terraces.IsTerrace(above.Elevation)
                && above.Elevation > below.Elevation;
        }

        static double ScreenUp(TilePosition position)
        {
            var point = IsoProjection.Of(position);

            return point.X * IsoProjection.CameraUp.X
                + point.Y * IsoProjection.CameraUp.Y
                + point.Z * IsoProjection.CameraUp.Z;
        }

        static void AssertProp(LevelGraph graph, LevelBlueprint blueprint, NodeType type, PartShape shape, PartStyle style)
        {
            var prop = Prop(graph, blueprint, type);

            Assert.That(prop.Shape, Is.EqualTo(shape), type.ToString());
            Assert.That(prop.Style, Is.EqualTo(style), type.ToString());
        }

        static WorldPart Prop(LevelGraph graph, LevelBlueprint blueprint, NodeType type)
        {
            var node = graph.Decisions.Nodes.First(candidate => candidate.Type == type);
            return blueprint.AllParts.First(part => part.Name == PartNames.Node(node.Id));
        }

        static IReadOnlyList<WorldPart> PartsStyled(LevelBlueprint blueprint, PartStyle style)
        {
            return blueprint.AllParts.Where(part => part.Style == style).ToList();
        }

        static WorldPoint QuadNormal(WorldPoint euler)
        {
            var pitch = euler.X * Math.PI / 180.0;
            var yaw = euler.Y * Math.PI / 180.0;

            var afterPitch = new[]
            {
                0.0,
                -QuadFacesNegativeZ * Math.Sin(pitch),
                QuadFacesNegativeZ * Math.Cos(pitch)
            };

            return new WorldPoint(
                (float)(afterPitch[0] * Math.Cos(yaw) + afterPitch[2] * Math.Sin(yaw)),
                (float)afterPitch[1],
                (float)(afterPitch[2] * Math.Cos(yaw) - afterPitch[0] * Math.Sin(yaw)));
        }
    }
}
