using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class StaircaseTests
    {
        const long Seed = 20250824L;

        const float Tolerance = 1e-5f;

        [Test]
        public void EveryTileAStepAboveALowerNeighbourCarriesTheStaircaseModel()
        {
            var graph = Ship();
            var stairs = StairsByName(graph);
            var standing = graph.Tiles.Tiles
                .Where(tile => TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                .ToList();

            Assert.That(standing, Is.Not.Empty);

            foreach (var tile in standing)
            {
                var name = PartNames.Stair(tile.Position);

                Assert.That(stairs.ContainsKey(name), Is.True, name);
                Assert.That(stairs[name].Model, Is.EqualTo(PartModel.Staircase), name);
                Assert.That(stairs[name].Style, Is.EqualTo(PartStyle.Staircase), name);
            }

            Assert.That(stairs.Count, Is.EqualTo(standing.Count));
        }

        [Test]
        public void EveryClimbingTileLevelWithItsNeighboursCarriesTheFoundationModel()
        {
            var graph = Ship();
            var plinths = PlinthsByName(graph);
            var level = graph.Tiles.Tiles
                .Where(tile => TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Plinth)
                .ToList();

            Assert.That(level, Is.Not.Empty);

            foreach (var tile in level)
            {
                var name = PartNames.Footing(tile.Position);

                Assert.That(plinths.ContainsKey(name), Is.True, name);
                Assert.That(plinths[name].Model, Is.EqualTo(PartModel.Foundation), name);
                Assert.That(plinths[name].Style, Is.EqualTo(PartStyle.Foundation), name);
                Assert.That(StaircaseClimb.Climbs(tile.Position), Is.True, name);
            }

            Assert.That(plinths.Count, Is.EqualTo(level.Count));
        }

        [Test]
        public void ATileLevelWithOrBelowEveryNeighbourCarriesNoFlightUnderneath()
        {
            var graph = Ship();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var bare = 0;

            foreach (var part in blueprint.AllParts)
            {
                if (part.Model != PartModel.Staircase)
                {
                    continue;
                }

                Assert.That(
                    TileFootings.Under(graph.Tiles, StairFixture.TileUnder(graph, part)),
                    Is.EqualTo(TileFooting.Flight),
                    part.Name);
            }

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Nothing)
                {
                    continue;
                }

                bare++;
                Assert.That(
                    blueprint.AllParts.Any(part => part.Name == PartNames.Stair(tile.Position)),
                    Is.False,
                    PartNames.Stair(tile.Position));
                Assert.That(
                    blueprint.AllParts.Any(part => part.Name == PartNames.Footing(tile.Position)),
                    Is.False,
                    PartNames.Footing(tile.Position));
            }

            Assert.That(bare, Is.GreaterThan(0));
        }

        [Test]
        public void ATerraceTileAtTheHeadOfAClimbCarriesTheFlightThatTopsTheClimbOut()
        {
            var graph = Ship();
            var stairs = StairsByName(graph);
            var heads = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (StaircaseClimb.Climbs(tile.Position)
                    || TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Flight)
                {
                    continue;
                }

                heads++;
                var name = PartNames.Stair(tile.Position);

                Assert.That(stairs.ContainsKey(name), Is.True, name);
                Assert.That(Terraces.IsTerrace(tile.Position.Elevation), Is.True, name);
            }

            Assert.That(heads, Is.GreaterThan(0));
        }

        [Test]
        public void AFoundationFillsTheWholeDropUnderALevelTile()
        {
            var graph = Ship();
            var counted = 0;

            foreach (var part in PlinthsByName(graph).Values)
            {
                var tile = FootingFixture.TileUnder(graph, part);
                var ground = IsoProjection.Of(tile);

                counted++;
                Assert.That(part.Scale.Y, Is.EqualTo(IsoProjection.StepHeight), part.Name);
                Assert.That(part.Scale.X, Is.EqualTo(IsoProjection.TileEdge), part.Name);
                Assert.That(part.Scale.Z, Is.EqualTo(IsoProjection.TileEdge), part.Name);
                Assert.That(part.Position.X, Is.EqualTo(ground.X), part.Name);
                Assert.That(part.Position.Z, Is.EqualTo(ground.Z), part.Name);
                Assert.That(
                    part.Position.Y, Is.EqualTo(ground.Y - IsoProjection.StepHeight * 0.5f), part.Name);
                Assert.That(part.Rotation, Is.EqualTo(new WorldPoint(0f, 0f, 0f)), part.Name);
            }

            Assert.That(counted, Is.GreaterThan(0));
        }

        [Test]
        public void AStaircaseTileIsTheOneTheDomainCallsAClimb()
        {
            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var graph = LevelGenerator.Generate(Seed, preset).Graph;

                foreach (var tile in graph.Tiles.Tiles)
                {
                    Assert.That(
                        StaircaseClimb.Climbs(tile.Position),
                        Is.EqualTo(!Terraces.IsTerrace(tile.Position.Elevation)),
                        tile.Position.ToString());
                }
            }
        }

        [Test]
        public void AStaircaseStandsOneStepAboveTheTerraceItRisesFrom()
        {
            var graph = Ship();

            foreach (var part in StairsByName(graph).Values)
            {
                var tile = StairFixture.TileUnder(graph, part);
                var ground = IsoProjection.Of(tile);

                Assert.That(part.Scale.Y, Is.EqualTo(IsoProjection.StepHeight), part.Name);
                Assert.That(part.Position.Y, Is.EqualTo(ground.Y - IsoProjection.StepHeight * 0.5f), part.Name);
                Assert.That(part.Position.X, Is.EqualTo(ground.X), part.Name);
                Assert.That(part.Position.Z, Is.EqualTo(ground.Z), part.Name);
                Assert.That(part.Scale.X, Is.EqualTo(IsoProjection.TileEdge), part.Name);
                Assert.That(part.Scale.Z, Is.EqualTo(IsoProjection.TileEdge), part.Name);
            }
        }

        [Test]
        public void EveryFlightCrestsAtTheHeadOfItsOwnClimbAndSinksAtItsFoot()
        {
            var graph = Ship();
            var counted = 0;

            Assert.That(StaircaseFlight.PackCrestAtItsOrigin, Is.GreaterThan(StaircaseFlight.PackCrestAtItsFarEnd));

            foreach (var part in StairsByName(graph).Values)
            {
                var tile = StairFixture.TileUnder(graph, part);
                var ascent = TileFootings.AscentOf(graph.Tiles, tile);
                var ground = IsoProjection.Of(tile);
                var crest = StaircaseFlight.ReachAlong(StaircaseFlight.CrestOf(part), ground, ascent);
                var sunk = StaircaseFlight.ReachAlong(StaircaseFlight.SinkOf(part), ground, ascent);

                counted++;
                Assert.That(crest, Is.GreaterThan(sunk), part.Name);
                Assert.That(crest, Is.EqualTo(IsoProjection.TileEdge * 0.5f).Within(Tolerance), part.Name);
                Assert.That(sunk, Is.EqualTo(IsoProjection.TileEdge * -0.5f).Within(Tolerance), part.Name);
            }

            Assert.That(counted, Is.GreaterThan(0));
        }

        [Test]
        public void AClimbLeavingATerraceAscendsAwayFromIt()
        {
            var graph = Ship();
            var feet = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                foreach (var neighbour in graph.Tiles.Neighbours(tile.Position))
                {
                    if (neighbour.Elevation >= tile.Position.Elevation)
                    {
                        continue;
                    }

                    feet++;
                    Assert.That(
                        StaircaseClimb.AscentOf(graph.Tiles, tile.Position),
                        Is.EqualTo(TileSides.Opposite(TileSides.Between(tile.Position, neighbour))),
                        tile.Position.ToString());
                }
            }

            Assert.That(feet, Is.GreaterThan(0));
        }

        [Test]
        public void AClimbReachingATerraceAscendsTowardsIt()
        {
            var graph = Ship();
            var heads = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                foreach (var neighbour in graph.Tiles.Neighbours(tile.Position))
                {
                    if (neighbour.Elevation <= tile.Position.Elevation)
                    {
                        continue;
                    }

                    heads++;
                    Assert.That(
                        StaircaseClimb.AscentOf(graph.Tiles, tile.Position),
                        Is.EqualTo(TileSides.Between(tile.Position, neighbour)),
                        tile.Position.ToString());
                }
            }

            Assert.That(heads, Is.GreaterThan(0));
        }

        [Test]
        public void ARunBendingIntoAnLTurnsWhereItBends()
        {
            var graph = Ship();
            var yaws = new HashSet<float>();
            var bends = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                var here = StaircaseClimb.AscentOf(graph.Tiles, tile.Position);
                yaws.Add(TileSides.InwardYaw(here));

                foreach (var neighbour in graph.Tiles.Neighbours(tile.Position))
                {
                    if (neighbour.Elevation != tile.Position.Elevation)
                    {
                        continue;
                    }

                    if (StaircaseClimb.AscentOf(graph.Tiles, neighbour) != here)
                    {
                        bends++;
                    }
                }
            }

            Assert.That(bends, Is.GreaterThan(0));
            Assert.That(yaws.Count, Is.GreaterThan(1));
        }

        [Test]
        public void EveryStaircaseInEveryPresetAscendsAlongItsOwnRun()
        {
            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var graph = LevelGenerator.Generate(Seed, preset).Graph;

                foreach (var tile in graph.Tiles.Tiles)
                {
                    if (!StaircaseClimb.Climbs(tile.Position))
                    {
                        continue;
                    }

                    var ascent = StaircaseClimb.AscentOf(graph.Tiles, tile.Position);
                    var onward = TileSides.Step(tile.Position, ascent);
                    var back = TileSides.Step(tile.Position, TileSides.Opposite(ascent));

                    Assert.That(
                        graph.Tiles.ContainsPlace(onward.X, onward.Y)
                        || graph.Tiles.ContainsPlace(back.X, back.Y),
                        Is.True,
                        tile.Position.ToString());
                }
            }
        }

        [Test]
        public void AClimbAsksTheDomainRatherThanTheTerraceItSitsBetween()
        {
            Assert.That(StaircaseClimb.Climbs(new TilePosition(0, 3, 3)), Is.False);
            Assert.That(StaircaseClimb.Climbs(new TilePosition(1, 3, 3)), Is.True);
            Assert.That(StaircaseClimb.Climbs(new TilePosition(2, 3, 3)), Is.False);
            Assert.That(StaircaseClimb.Climbs(new TilePosition(3, 3, 3)), Is.True);
        }

        [Test]
        public void AskingATerraceTileWhereItClimbsIsARefusal()
        {
            var graph = Ship();
            var terrace = graph.Tiles.Tiles.First(tile => !StaircaseClimb.Climbs(tile.Position)).Position;

            Assert.That(
                () => StaircaseClimb.AscentOf(graph.Tiles, terrace),
                Throws.ArgumentException);
            Assert.That(
                () => StaircaseClimb.AscentOf(null, new TilePosition(1, 0, 0)),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void TheFloorUnderAStaircaseIsStillOrdinaryWalkableGround()
        {
            var graph = Ship();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var floors = blueprint.AllParts
                .Where(part => part.Style == PartStyle.Floor)
                .ToDictionary(part => part.Name);

            Assert.That(floors.Count, Is.EqualTo(graph.Tiles.Tiles.Count));

            foreach (var tile in graph.Tiles.Tiles)
            {
                var floor = floors[PartNames.Tile(tile.Position)];

                Assert.That(floor.Model, Is.EqualTo(PartModel.FloorTile), floor.Name);
                Assert.That(floor.Position, Is.EqualTo(IsoProjection.Of(tile.Position)), floor.Name);
                Assert.That(floor.Rotation, Is.EqualTo(new WorldPoint(90f, 0f, 0f)), floor.Name);
                Assert.That(
                    floor.Scale,
                    Is.EqualTo(new WorldPoint(IsoProjection.TileEdge, IsoProjection.TileEdge, 1f)),
                    floor.Name);
            }
        }

        [Test]
        public void AddingStaircasesLeavesEveryWallWhereItWas()
        {
            var graph = Ship();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var walls = blueprint.AllParts.Where(part => part.Style == PartStyle.Wall).ToList();
            var outward = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (!graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        outward++;
                    }
                }
            }

            Assert.That(walls.Count, Is.EqualTo(outward));
        }

        [Test]
        public void AStaircaseNamesEveryTileItStandsOnApart()
        {
            var graph = Ship();
            var names = new HashSet<string>();

            foreach (var part in LevelBlueprintBuilder.Build(graph).AllParts)
            {
                Assert.That(names.Add(part.Name), Is.True, part.Name);
            }
        }

        static LevelGraph Ship()
        {
            return LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
        }

        static IDictionary<string, WorldPart> StairsByName(LevelGraph graph)
        {
            return LevelBlueprintBuilder.Build(graph).AllParts
                .Where(part => part.Model == PartModel.Staircase)
                .ToDictionary(part => part.Name);
        }

        static IDictionary<string, WorldPart> PlinthsByName(LevelGraph graph)
        {
            return LevelBlueprintBuilder.Build(graph).AllParts
                .Where(part => part.Model == PartModel.Foundation)
                .ToDictionary(part => part.Name);
        }

    }
}
