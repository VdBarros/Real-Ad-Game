using System;
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
            var surfaces = blueprint.AllParts
                .Where(part => LevelBlueprintBuilder.IsWalkingSurface(part.Style))
                .ToDictionary(part => part.Name);

            Assert.That(surfaces.Count, Is.EqualTo(graph.Tiles.Tiles.Count));

            foreach (var tile in graph.Tiles.Tiles)
            {
                var surface = surfaces[LevelBlueprintBuilder.WalkingSurfaceOf(graph.Tiles, tile.Position)];

                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                {
                    Assert.That(surface.Model, Is.EqualTo(PartModel.Staircase), surface.Name);
                    continue;
                }

                Assert.That(surface.Model, Is.EqualTo(PartModel.FloorTile), surface.Name);
                Assert.That(surface.Position, Is.EqualTo(IsoProjection.Of(tile.Position)), surface.Name);
                Assert.That(surface.Rotation, Is.EqualTo(new WorldPoint(90f, 0f, 0f)), surface.Name);
                Assert.That(
                    surface.Scale,
                    Is.EqualTo(new WorldPoint(IsoProjection.TileEdge, IsoProjection.TileEdge, 1f)),
                    surface.Name);
            }
        }

        [Test]
        public void AddingStaircasesLeavesEveryWallWhereItWasExceptWhereAFlightRailsItself()
        {
            var graph = Ship();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var walls = blueprint.AllParts.Where(part => part.Style == PartStyle.Wall).ToList();
            var outward = 0;
            var railed = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        continue;
                    }

                    if (RailedByItsOwnFlight(graph, tile.Position, side))
                    {
                        railed++;
                        continue;
                    }

                    outward++;
                }
            }

            Assert.That(railed, Is.GreaterThan(0));
            Assert.That(walls.Count, Is.EqualTo(outward));
        }

        [Test]
        public void NoPanelIsLeftHangingOverTheSlopeOfTheFlightBesideIt()
        {
            var graph = Ship();
            var walls = LevelBlueprintBuilder.Build(graph).AllParts
                .Where(part => part.Style == PartStyle.Wall)
                .ToDictionary(part => part.Name);
            var dropped = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                foreach (var side in TileSides.All)
                {
                    var name = PartNames.Wall(tile.Position, side);
                    WorldPart wall;

                    if (!walls.TryGetValue(name, out wall))
                    {
                        if (RailedByItsOwnFlight(graph, tile.Position, side))
                        {
                            dropped++;
                        }

                        continue;
                    }

                    Assert.That(RailedByItsOwnFlight(graph, tile.Position, side), Is.False, name);
                    Assert.That(
                        wall.Position.Y - IsoProjection.WallHeight * 0.5f,
                        Is.EqualTo(StaircaseFlight.HandsOverAt(graph.Tiles, tile.Position, side))
                            .Within(Tolerance),
                        name);
                }
            }

            Assert.That(dropped, Is.GreaterThan(0));
        }

        [Test]
        public void APanelBesideAFlightStandsOnTheFlightRatherThanOverIt()
        {
            foreach (var ascent in TileSides.All)
            {
                var tiles = ARunClimbing(ascent);
                var flight = Standing(tiles, TileSides.Step(new TilePosition(0, 0, 0), ascent));
                var ground = IsoProjection.Of(flight).Y;

                Assert.That(TileFootings.Under(tiles, flight), Is.EqualTo(TileFooting.Flight));
                Assert.That(TileFootings.AscentOf(tiles, flight), Is.EqualTo(ascent));

                foreach (var side in TileSides.All)
                {
                    var standing = StaircaseFlight.HandsOverAt(tiles, flight, side);
                    var rails = StaircaseFlight.RailsItsOwn(side, ascent);
                    WorldPart wall;

                    Assert.That(
                        LevelBlueprintBuilder.TryWall(tiles, flight, side, out wall),
                        Is.EqualTo(!rails),
                        ascent + " railed on " + side);

                    if (side == ascent)
                    {
                        Assert.That(standing, Is.EqualTo(ground).Within(Tolerance), side.ToString());
                    }
                    else if (rails)
                    {
                        Assert.That(
                            standing,
                            Is.EqualTo(ground - IsoProjection.StepHeight * 0.5f).Within(Tolerance),
                            side.ToString());
                        continue;
                    }
                    else
                    {
                        Assert.That(
                            standing,
                            Is.EqualTo(ground - IsoProjection.StepHeight).Within(Tolerance),
                            side.ToString());
                    }

                    Assert.That(
                        wall.Position.Y - IsoProjection.WallHeight * 0.5f,
                        Is.EqualTo(standing).Within(Tolerance),
                        wall.Name);
                }
            }
        }

        [Test]
        public void TheFirstGroundOfTheUpperTerraceIsTheLastGroundOfTheTopStep()
        {
            foreach (var ascent in TileSides.All)
            {
                var tiles = ARunClimbing(ascent);
                var joins = 0;

                foreach (var tile in tiles.Tiles)
                {
                    foreach (var neighbour in tiles.Neighbours(tile.Position))
                    {
                        var side = TileSides.Between(tile.Position, neighbour);

                        joins++;
                        Assert.That(
                            StaircaseFlight.HandsOverAt(tiles, tile.Position, side),
                            Is.EqualTo(StaircaseFlight.HandsOverAt(
                                tiles, neighbour, TileSides.Opposite(side))).Within(Tolerance),
                            ascent + ": " + tile.Position + " hands over to " + neighbour);
                    }
                }

                Assert.That(joins, Is.EqualTo(6));
            }
        }

        [Test]
        public void TheTopStepEndsOnTheEdgeTheUpperFloorStartsAt()
        {
            foreach (var ascent in TileSides.All)
            {
                var tiles = ARunClimbing(ascent);
                var flights = 0;

                foreach (var tile in tiles.Tiles)
                {
                    if (TileFootings.Under(tiles, tile.Position) != TileFooting.Flight)
                    {
                        continue;
                    }

                    flights++;
                    var part = LevelBlueprintBuilder.WalkingSurface(tiles, tile.Position);
                    var ground = IsoProjection.Of(tile.Position);
                    var above = Standing(tiles, TileSides.Step(tile.Position, ascent));
                    var beyond = LevelBlueprintBuilder.WalkingSurface(tiles, above);
                    var crest = StaircaseFlight.CrestOf(part);
                    var tread = crest.Y + DungeonPack.StaircaseTread * ModelPose.ScaleOf(part).Y;

                    Assert.That(
                        StaircaseFlight.ReachAlong(crest, ground, ascent),
                        Is.EqualTo(IsoProjection.TileEdge * 0.5f).Within(Tolerance),
                        part.Name);
                    Assert.That(
                        StaircaseFlight.ReachAlong(crest, beyond.Position, ascent),
                        Is.EqualTo(IsoProjection.TileEdge * -0.5f).Within(Tolerance),
                        part.Name);
                    Assert.That(
                        StaircaseFlight.ReachAlong(StaircaseFlight.SinkOf(part), ground, ascent),
                        Is.EqualTo(IsoProjection.TileEdge * -0.5f).Within(Tolerance),
                        part.Name);
                    Assert.That(tread, Is.EqualTo(ground.Y).Within(Tolerance), part.Name);
                    Assert.That(
                        tread,
                        Is.EqualTo(StaircaseFlight.HandsOverAt(
                            tiles, above, TileSides.Opposite(ascent))).Within(Tolerance),
                        part.Name);
                    Assert.That(
                        crest.Y,
                        Is.EqualTo(ground.Y - IsoProjection.StepHeight).Within(Tolerance),
                        part.Name);
                }

                Assert.That(flights, Is.EqualTo(2));
            }
        }

        [Test]
        public void NoFloorQuadIsLaidOverTheTileAFlightClimbsAcross()
        {
            foreach (var ascent in TileSides.All)
            {
                var tiles = ARunClimbing(ascent);
                var surfaces = new List<WorldPart>();

                foreach (var tile in tiles.Tiles)
                {
                    surfaces.Add(LevelBlueprintBuilder.WalkingSurface(tiles, tile.Position));
                }

                foreach (var tile in tiles.Tiles)
                {
                    var flight = TileFootings.Under(tiles, tile.Position) == TileFooting.Flight;
                    var ground = IsoProjection.Of(tile.Position);

                    Assert.That(
                        surfaces.Any(part => part.Name == PartNames.Tile(tile.Position)),
                        Is.EqualTo(!flight),
                        PartNames.Tile(tile.Position));

                    if (!flight)
                    {
                        continue;
                    }

                    foreach (var part in surfaces)
                    {
                        if (part.Style != PartStyle.Floor)
                        {
                            continue;
                        }

                        Assert.That(
                            Math.Abs(part.Position.X - ground.X) + Math.Abs(part.Position.Z - ground.Z),
                            Is.GreaterThanOrEqualTo(IsoProjection.TileEdge - Tolerance),
                            part.Name + " over " + PartNames.Stair(tile.Position));
                    }
                }
            }
        }

        static TileGrid ARunClimbing(TileSide ascent)
        {
            var tiles = new List<Tile>();
            var place = new TilePosition(0, 0, 0);

            for (var step = 0; step < 4; step++)
            {
                tiles.Add(new Tile(new TilePosition(Math.Min(step, 2), place.X, place.Y), regionId: 0));
                place = TileSides.Step(place, ascent);
            }

            return new TileGrid(tiles);
        }

        static TilePosition Standing(TileGrid tiles, TilePosition place)
        {
            foreach (var tile in tiles.Tiles)
            {
                if (tile.Position.X == place.X && tile.Position.Y == place.Y)
                {
                    return tile.Position;
                }
            }

            throw new InvalidOperationException("No tile stands where " + place + " does.");
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

        static bool RailedByItsOwnFlight(LevelGraph graph, TilePosition position, TileSide side)
        {
            return TileFootings.Under(graph.Tiles, position) == TileFooting.Flight
                && StaircaseFlight.RailsItsOwn(side, TileFootings.AscentOf(graph.Tiles, position));
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
