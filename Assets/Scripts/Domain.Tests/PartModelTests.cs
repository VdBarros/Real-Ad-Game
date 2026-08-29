using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PartModelTests
    {
        [Test]
        public void EveryStyleNamesTheModelItWants()
        {
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                Assert.That(() => PartModels.Of(style), Throws.Nothing, style.ToString());
            }
        }

        [Test]
        public void FloorTilesWantTheFloorTileModel()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
            var floors = PartsStyled(blueprint, PartStyle.Floor);

            Assert.That(floors, Is.Not.Empty);
            Assert.That(floors.All(part => part.Model == PartModel.FloorTile), Is.True);
        }

        [Test]
        public void AClearedFloorTileWantsTheSameModelAsACursedOne()
        {
            Assert.That(PartModels.Of(PartStyle.Cleared), Is.EqualTo(PartModel.FloorTile));
            Assert.That(PartModels.Of(PartStyle.Cleared), Is.EqualTo(PartModels.Of(PartStyle.Floor)));
        }

        [Test]
        public void WallsWantTheWallPanelModel()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
            var walls = PartsStyled(blueprint, PartStyle.Wall);

            Assert.That(walls, Is.Not.Empty);
            Assert.That(walls.All(part => part.Model == PartModel.WallPanel), Is.True);
        }

        [Test]
        public void AnAdditiveNodeWantsAChestAndAMultiplierWantsNoMeshAtAll()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            Assert.That(Prop(graph, blueprint, NodeType.Additive).Model, Is.EqualTo(PartModel.Chest));
            Assert.That(Prop(graph, blueprint, NodeType.Multiplier).Model, Is.EqualTo(PartModel.None));
        }

        [Test]
        public void TheTwoRewardKindsWantDifferentModels()
        {
            Assert.That(PartModels.Of(PartStyle.Additive), Is.Not.EqualTo(PartModels.Of(PartStyle.Multiplier)));
            Assert.That(PartModels.Of(PartStyle.Multiplier), Is.EqualTo(PartModel.None));
        }

        [Test]
        public void ThePlayerWantsTheCastMeshAndKeepsTheCapsuleAsItsFallbackShape()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var player = Prop(graph, blueprint, NodeType.Start);

            Assert.That(player.Model, Is.EqualTo(CharacterCast.MeshOf(PartStyle.Start)));
            Assert.That(player.Model, Is.Not.EqualTo(PartModel.None));
            Assert.That(player.Shape, Is.EqualTo(PartShape.Capsule));
        }

        [Test]
        public void AdversariesWantACastMeshAndKeepTheCapsuleAsTheirFallbackShape()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var type in new[] { NodeType.Enemy, NodeType.Boss })
            {
                var node = graph.Decisions.Nodes.First(candidate => candidate.Type == type);
                var figure = Prop(graph, blueprint, type);
                var style = type == NodeType.Boss ? PartStyle.Boss : PartStyle.Enemy;

                Assert.That(figure.Model, Is.Not.EqualTo(PartModel.None), type.ToString());
                Assert.That(
                    figure.Model, Is.EqualTo(CharacterCast.MeshOf(style, node.Value)), type.ToString());
                Assert.That(figure.Shape, Is.EqualTo(PartShape.Capsule), type.ToString());
            }
        }

        [Test]
        public void EffectsWantNoModel()
        {
            Assert.That(PartModels.Of(PartStyle.Trail), Is.EqualTo(PartModel.None));
            Assert.That(PartModels.Of(PartStyle.Spark), Is.EqualTo(PartModel.None));
        }

        [Test]
        public void EveryPartTheBlueprintEmitsCarriesTheModelItsStyleWants()
        {
            var graph = LevelGenerator.Generate(20250824L, MazePreset.Ship).Graph;
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var valueByName = new Dictionary<string, int>();

            foreach (var node in graph.Decisions.Nodes)
            {
                valueByName[PartNames.Node(node.Id)] = node.Value;
            }

            Assert.That(blueprint.AllParts, Is.Not.Empty);

            foreach (var part in blueprint.AllParts)
            {
                int value;
                var known = valueByName.TryGetValue(part.Name, out value);

                Assert.That(
                    part.Model,
                    Is.EqualTo(known ? PartModels.Of(part.Style, value) : PartModels.Of(part.Style)),
                    part.Name);
                Assert.That(
                    CharacterCast.IsRole(part.Style) || part.Model == PartModels.Of(part.Style),
                    Is.True,
                    part.Name + ": only a cast role's mesh is read off its own number");
            }
        }

        [Test]
        public void CarryingAModelLeavesEveryTileWhereItWas()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var floors = PartsStyled(blueprint, PartStyle.Floor).ToDictionary(part => part.Name);

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                {
                    Assert.That(floors.ContainsKey(PartNames.Tile(tile.Position)), Is.False);
                    continue;
                }

                var floor = floors[PartNames.Tile(tile.Position)];

                Assert.That(floor.Position, Is.EqualTo(IsoProjection.Of(tile.Position)), floor.Name);
                Assert.That(floor.Rotation, Is.EqualTo(new WorldPoint(90f, 0f, 0f)), floor.Name);
                Assert.That(
                    floor.Scale,
                    Is.EqualTo(new WorldPoint(IsoProjection.TileEdge, IsoProjection.TileEdge, 1f)),
                    floor.Name);
            }
        }

        [Test]
        public void CarryingAModelLeavesOneWallOnEveryTileSideThatFacesOutsideTheGrid()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var walls = PartsStyled(blueprint, PartStyle.Wall).ToDictionary(part => part.Name);
            var outward = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    var name = PartNames.Wall(tile.Position, side);

                    var footing = TileFootings.Under(graph.Tiles, tile.Position);
                    var railed = footing == TileFooting.Flight
                        && StaircaseFlight.RailsItsOwn(
                            side, TileFootings.AscentOf(graph.Tiles, tile.Position));

                    if (graph.Tiles.ContainsPlace(beyond.X, beyond.Y) || railed)
                    {
                        Assert.That(walls.ContainsKey(name), Is.False, name);
                        continue;
                    }

                    Assert.That(walls.ContainsKey(name), Is.True, name);
                    outward++;

                    var wall = walls[name];
                    var here = IsoProjection.Of(tile.Position);
                    var there = IsoProjection.Of(beyond);
                    var standing = StaircaseFlight.HandsOverAt(graph.Tiles, tile.Position, side);

                    Assert.That(
                        wall.Position,
                        Is.EqualTo(new WorldPoint(
                            (here.X + there.X) * 0.5f,
                            standing + IsoProjection.WallHeight * 0.5f,
                            (here.Z + there.Z) * 0.5f)),
                        name);
                    Assert.That(
                        wall.Rotation,
                        Is.EqualTo(new WorldPoint(0f, TileSides.InwardYaw(side), 0f)),
                        name);
                    Assert.That(
                        wall.Scale,
                        Is.EqualTo(new WorldPoint(
                            IsoProjection.TileEdge, IsoProjection.WallHeight, 1f)),
                        name);
                }
            }

            Assert.That(walls.Count, Is.EqualTo(outward));
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
    }
}
