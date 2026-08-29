using System;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ModelPoseTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void EveryModelAnswersWhatPoseItWants()
        {
            var part = FirstFloor();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                var posed = new WorldPart(
                    part.Name, part.Shape, model, part.Style, part.Position, part.Rotation, part.Scale);

                Assert.That(() => ModelPose.PositionOf(posed), Throws.Nothing, model.ToString());
                Assert.That(() => ModelPose.RotationOf(posed), Throws.Nothing, model.ToString());
                Assert.That(() => ModelPose.ScaleOf(posed), Throws.Nothing, model.ToString());
            }
        }

        [Test]
        public void AFloorMeshLiesFlatWhereTheQuadHadToBeTilted()
        {
            var floor = FirstFloor();

            Assert.That(floor.Rotation, Is.EqualTo(new WorldPoint(90f, 0f, 0f)));
            Assert.That(ModelPose.RotationOf(floor), Is.EqualTo(new WorldPoint(0f, 0f, 0f)));
        }

        [Test]
        public void AFloorMeshSpansTheTileTheQuadSpanned()
        {
            var floor = FirstFloor();
            var scale = ModelPose.ScaleOf(floor);

            Assert.That(scale.X, Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(scale.Z, Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
        }

        [Test]
        public void AFloorMeshStaysWhereTheQuadWasBecauseBothPivotOnTheTileTop()
        {
            var floor = FirstFloor();

            Assert.That(ModelPose.PositionOf(floor), Is.EqualTo(floor.Position));
        }

        [Test]
        public void AFloorMeshKeepsWhateverYawItsPartAsksFor()
        {
            var floor = FirstFloor();
            var turned = new WorldPart(
                floor.Name,
                floor.Shape,
                floor.Model,
                floor.Style,
                floor.Position,
                new WorldPoint(90f, 270f, 0f),
                floor.Scale);

            Assert.That(ModelPose.RotationOf(turned), Is.EqualTo(new WorldPoint(0f, 270f, 0f)));
        }

        [Test]
        public void APartWithNoMeshIsPosedExactlyAsItsPrimitiveWas()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            foreach (var part in blueprint.AllParts.Where(candidate => candidate.Model == PartModel.None))
            {
                Assert.That(ModelPose.PositionOf(part), Is.EqualTo(part.Position), part.Name);
                Assert.That(ModelPose.RotationOf(part), Is.EqualTo(part.Rotation), part.Name);
                Assert.That(ModelPose.ScaleOf(part), Is.EqualTo(part.Scale), part.Name);
            }
        }

        [Test]
        public void TheImportScaleMapsThePacksGridOntoOneTile()
        {
            Assert.That(DungeonPack.GridUnits, Is.EqualTo(4f));
            Assert.That(
                DungeonPack.GridUnits * DungeonPack.ImportScale,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(
                DungeonPack.WallPanelWidth,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
        }

        [Test]
        public void AWallPanelDropsFromTheQuadsCentreOntoTheTileItGuards()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var walls = blueprint.AllParts.Where(part => part.Style == PartStyle.Wall).ToList();

            Assert.That(walls, Is.Not.Empty);

            foreach (var wall in walls)
            {
                var posed = ModelPose.PositionOf(wall);

                Assert.That(posed.X, Is.EqualTo(wall.Position.X).Within(Tolerance), wall.Name);
                Assert.That(posed.Z, Is.EqualTo(wall.Position.Z).Within(Tolerance), wall.Name);
                Assert.That(
                    posed.Y,
                    Is.EqualTo(wall.Position.Y - IsoProjection.WallHeight * 0.5f).Within(Tolerance),
                    wall.Name);
            }
        }

        [Test]
        public void AWallPanelStandsOnTheEdgeItsOwnTileTopSitsAt()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var tile in graph.Tiles.Tiles)
            {
                var top = IsoProjection.Of(tile.Position);

                foreach (var side in TileSides.All)
                {
                    var name = PartNames.Wall(tile.Position, side);
                    var wall = blueprint.AllParts.FirstOrDefault(part => part.Name == name);

                    if (wall.Name == null)
                    {
                        continue;
                    }

                    var posed = ModelPose.PositionOf(wall);
                    var beyond = IsoProjection.Of(TileSides.Step(tile.Position, side));

                    Assert.That(posed.Y, Is.EqualTo(top.Y).Within(Tolerance), name);
                    Assert.That(posed.X, Is.EqualTo((top.X + beyond.X) * 0.5f).Within(Tolerance), name);
                    Assert.That(posed.Z, Is.EqualTo((top.Z + beyond.Z) * 0.5f).Within(Tolerance), name);
                }
            }
        }

        [Test]
        public void AWallPanelKeepsTheInwardYawTheQuadWasGiven()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());

            foreach (var wall in blueprint.AllParts.Where(part => part.Style == PartStyle.Wall))
            {
                Assert.That(ModelPose.RotationOf(wall), Is.EqualTo(wall.Rotation), wall.Name);
            }
        }

        [Test]
        public void AWallPanelSpansItsTileEdgeAndKeepsTheParapetsOwnHeight()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces());
            var wall = blueprint.AllParts.First(part => part.Style == PartStyle.Wall);
            var scale = ModelPose.ScaleOf(wall);

            Assert.That(
                scale.X * DungeonPack.WallPanelWidth,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(scale.Y, Is.EqualTo(scale.X).Within(Tolerance));
            Assert.That(scale.Z, Is.EqualTo(scale.X).Within(Tolerance));
            Assert.That(
                scale.Y * DungeonPack.HeightOf(PartModel.WallPanel),
                Is.LessThan(IsoProjection.WallHeight));
        }

        [Test]
        public void AContentPropStandsOnTheTileRatherThanFloatingAtItsCubesCentre()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop) || prop.Model == PartModel.None)
                {
                    continue;
                }

                var top = IsoProjection.Of(node.Position);

                Assert.That(ModelPose.PositionOf(prop).Y, Is.EqualTo(top.Y).Within(Tolerance), prop.Name);
                Assert.That(prop.Position.Y, Is.GreaterThan(top.Y), prop.Name);
            }
        }

        [Test]
        public void EachRewardPropIsFittedToTheCubeItReplaces()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var blueprint = LevelBlueprintBuilder.Build(graph);

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!LevelBlueprintBuilder.TryProp(node, out prop)
                    || prop.Model == PartModel.None
                    || CharacterCast.IsRole(prop.Style))
                {
                    continue;
                }

                var scale = ModelPose.ScaleOf(prop);

                Assert.That(
                    scale.Y * DungeonPack.HeightOf(prop.Model),
                    Is.EqualTo(LevelBlueprintBuilder.PickupScale).Within(Tolerance),
                    prop.Name);
            }
        }

        [Test]
        public void AChestIsFittedToItsCubeAndAGateIsPosedExactlyAsTheBlueprintDrewIt()
        {
            var chest = Pickup(PartStyle.Additive);
            var gate = Pickup(PartStyle.Multiplier);

            Assert.That(chest.Model, Is.EqualTo(PartModel.Chest));
            Assert.That(
                ModelPose.ScaleOf(chest).Y * DungeonPack.HeightOf(PartModel.Chest),
                Is.EqualTo(LevelBlueprintBuilder.PickupScale).Within(Tolerance));

            Assert.That(gate.Model, Is.EqualTo(PartModel.None));
            Assert.That(gate.Shape, Is.EqualTo(PartShape.Gate));
            Assert.That(ModelPose.PositionOf(gate), Is.EqualTo(gate.Position));
            Assert.That(ModelPose.RotationOf(gate), Is.EqualTo(gate.Rotation));
            Assert.That(ModelPose.ScaleOf(gate), Is.EqualTo(gate.Scale));
        }

        [Test]
        public void ARewardPropIsSquareOnEveryAxisSoItsLidSwingsInsteadOfShearing()
        {
            var chest = Pickup(PartStyle.Additive);
            var square = ModelPose.ScaleOf(chest);

            Assert.That(square.X, Is.EqualTo(square.Y).Within(Tolerance));
            Assert.That(square.Z, Is.EqualTo(square.Y).Within(Tolerance));

            var splayed = new WorldPart(
                chest.Name,
                chest.Shape,
                chest.Model,
                chest.Style,
                chest.Position,
                chest.Rotation,
                new WorldPoint(chest.Scale.X * 1.24f, chest.Scale.Y * 0.2f, chest.Scale.Z * 1.24f));
            var refused = ModelPose.ScaleOf(splayed);

            Assert.That(refused.X, Is.EqualTo(refused.Y).Within(Tolerance));
            Assert.That(refused.Z, Is.EqualTo(refused.Y).Within(Tolerance));
        }

        [Test]
        public void AChestTurnsItsFrontOutOfTheCamerasBlindSide()
        {
            var chest = Pickup(PartStyle.Additive);

            Assert.That(ModelPose.ChestFacing, Is.EqualTo(180f));
            Assert.That(
                ModelPose.RotationOf(chest).Y,
                Is.EqualTo(chest.Rotation.Y + ModelPose.ChestFacing).Within(Tolerance));
            Assert.That(ModelPose.RotationOf(chest).X, Is.EqualTo(chest.Rotation.X).Within(Tolerance));
            Assert.That(ModelPose.RotationOf(chest).Z, Is.EqualTo(chest.Rotation.Z).Within(Tolerance));
        }

        [Test]
        public void EveryMeshTheTableNamesHasAMeasuredPackHeight()
        {
            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None)
                {
                    Assert.That(
                        () => ArtPacks.PackHeightOf(model),
                        Throws.InstanceOf<ArgumentOutOfRangeException>());
                    continue;
                }

                Assert.That(ArtPacks.PackHeightOf(model), Is.GreaterThan(0f), model.ToString());
                Assert.That(
                    ArtPacks.HeightOf(model),
                    Is.EqualTo(ArtPacks.PackHeightOf(model) * ArtPacks.ImportScaleFor(model)).Within(Tolerance),
                    model.ToString());
            }
        }

        [Test]
        public void ASolidWallOfOneWallHeightHidesTheTileBehindItAtTheCamerasPitch()
        {
            Assert.That(IsoProjection.SightReach(0f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(
                IsoProjection.SightReach(IsoProjection.WallHeight),
                Is.EqualTo(1.224745f).Within(1e-4f));
            Assert.That(
                IsoProjection.SightReach(IsoProjection.WallHeight),
                Is.GreaterThan(IsoProjection.TileEdge));
            Assert.That(
                IsoProjection.SightReach(IsoProjection.WallHeight * 2f),
                Is.EqualTo(IsoProjection.SightReach(IsoProjection.WallHeight) * 2f).Within(Tolerance));
        }

        [Test]
        public void TheParapetLeavesMostOfTheTileBehindItInSight()
        {
            var standing = DungeonPack.HeightOf(PartModel.WallPanel);

            Assert.That(standing, Is.LessThan(IsoProjection.WallHeight));
            Assert.That(IsoProjection.SightReach(standing), Is.EqualTo(0.336805f).Within(1e-4f));
            Assert.That(
                IsoProjection.SightReach(standing),
                Is.LessThan(IsoProjection.TileEdge * 0.5f));
            Assert.That(
                IsoProjection.SightReach(standing),
                Is.LessThan(IsoProjection.SightReach(IsoProjection.WallHeight)));
        }

        [Test]
        public void AStaircaseRisesExactlyTheStepTheTerracesClimbBy()
        {
            var stair = FirstStaircase();
            var scale = ModelPose.ScaleOf(stair);

            Assert.That(
                DungeonPack.StaircaseTread * scale.Y,
                Is.EqualTo(IsoProjection.StepHeight).Within(Tolerance));
            Assert.That(
                DungeonPack.HeightOf(PartModel.Staircase) * scale.Y,
                Is.EqualTo(IsoProjection.StepHeight + DungeonPack.StaircaseParapet).Within(Tolerance));
            Assert.That(
                DungeonPack.StaircaseWidth * scale.X,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(
                DungeonPack.StaircaseRun * scale.Z,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
        }

        [Test]
        public void AStaircaseNeedsNoStretchingAcrossItsTileBecauseThePackCutItToTheGrid()
        {
            Assert.That(DungeonPack.StaircaseWidth, Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(DungeonPack.StaircaseRun, Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));

            var scale = ModelPose.ScaleOf(FirstStaircase());

            Assert.That(scale.X, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(scale.Z, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AFoundationIsStretchedToItsTileOnPurposeAndStillSpillsNothingOffIt()
        {
            var plinth = FirstPlinth();
            var scale = ModelPose.ScaleOf(plinth);
            var footed = ModelPose.PositionOf(plinth);

            Assert.That(DungeonPack.FoundationWidth, Is.LessThan(IsoProjection.TileEdge));
            Assert.That(DungeonPack.FoundationRun, Is.LessThan(IsoProjection.TileEdge));
            Assert.That(
                DungeonPack.HeightOf(PartModel.Foundation), Is.LessThan(IsoProjection.StepHeight));

            Assert.That(scale.X, Is.GreaterThan(1f));
            Assert.That(scale.Y, Is.GreaterThan(1f));
            Assert.That(scale.Z, Is.GreaterThan(1f));

            Assert.That(
                DungeonPack.FoundationWidth * scale.X,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(
                DungeonPack.FoundationRun * scale.Z,
                Is.EqualTo(IsoProjection.TileEdge).Within(Tolerance));
            Assert.That(
                DungeonPack.HeightOf(PartModel.Foundation) * scale.Y,
                Is.EqualTo(IsoProjection.StepHeight).Within(Tolerance));

            Assert.That(
                footed.Y,
                Is.EqualTo(plinth.Position.Y - IsoProjection.StepHeight * 0.5f).Within(Tolerance));
            Assert.That(footed.X, Is.EqualTo(plinth.Position.X).Within(Tolerance));
            Assert.That(footed.Z, Is.EqualTo(plinth.Position.Z).Within(Tolerance));
            Assert.That(ModelPose.RotationOf(plinth), Is.EqualTo(plinth.Rotation));
        }

        [Test]
        public void AFoundationFillsTheDropAStaircaseFlightOnlyNotches()
        {
            Assert.That(
                StaircaseFlight.PackCrestAtItsFarEnd / StaircaseFlight.PackCrestAtItsOrigin,
                Is.LessThan(0.5f));
            Assert.That(
                ModelPose.ScaleOf(FirstPlinth()).Y * DungeonPack.HeightOf(PartModel.Foundation),
                Is.EqualTo(IsoProjection.StepHeight).Within(Tolerance));
        }

        [Test]
        public void AStaircaseSetsItsCrestDownOnTheEdgeTheClimbEndsAt()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var stair = FirstStaircase();
            var head = TileSides.Toward(
                TileFootings.AscentOf(graph.Tiles, StairFixture.TileUnder(graph, stair)));
            var crest = StaircaseFlight.CrestOf(stair);
            var sunk = StaircaseFlight.SinkOf(stair);
            var ground = stair.Position.Y - IsoProjection.StepHeight * 0.5f;

            Assert.That(crest, Is.EqualTo(ModelPose.PositionOf(stair)));
            Assert.That(crest.Y, Is.EqualTo(ground).Within(Tolerance));
            Assert.That(sunk.Y, Is.EqualTo(ground).Within(Tolerance));
            Assert.That(
                crest.X,
                Is.EqualTo(stair.Position.X + head.X * IsoProjection.TileEdge * 0.5f).Within(Tolerance));
            Assert.That(
                crest.Z,
                Is.EqualTo(stair.Position.Z + head.Z * IsoProjection.TileEdge * 0.5f).Within(Tolerance));
            Assert.That(
                sunk.X,
                Is.EqualTo(stair.Position.X - head.X * IsoProjection.TileEdge * 0.5f).Within(Tolerance));
            Assert.That(
                sunk.Z,
                Is.EqualTo(stair.Position.Z - head.Z * IsoProjection.TileEdge * 0.5f).Within(Tolerance));
        }

        [Test]
        public void AStaircaseRisesNoHigherOverItsLandingThanTheParapetsBesideIt()
        {
            var stair = FirstStaircase();
            var scale = ModelPose.ScaleOf(stair);
            var footed = ModelPose.PositionOf(stair);
            var landing = stair.Position.Y + IsoProjection.StepHeight * 0.5f;
            var tread = footed.Y + DungeonPack.StaircaseTread * scale.Y;
            var top = footed.Y + DungeonPack.HeightOf(PartModel.Staircase) * scale.Y;

            Assert.That(tread, Is.EqualTo(landing).Within(Tolerance));
            Assert.That(
                top - landing,
                Is.EqualTo(DungeonPack.HeightOf(PartModel.WallPanel)).Within(Tolerance));
            Assert.That(
                IsoProjection.SightReach(top - landing),
                Is.LessThan(IsoProjection.OcclusionBound));
            Assert.That(IsoProjection.SightReach(0f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AStaircaseIsLaidSoThePacksFlightDescendsTheWayItsOwnClimbFalls()
        {
            var graph = LevelGraphFixture.TwoTerraces();
            var stair = FirstStaircase();
            var ascent = TileFootings.AscentOf(graph.Tiles, StairFixture.TileUnder(graph, stair));

            Assert.That(StaircaseFlight.PackCrestAtItsOrigin, Is.GreaterThan(StaircaseFlight.PackCrestAtItsFarEnd));
            Assert.That(StaircaseFlight.LaidAgainst(ascent), Is.EqualTo(TileSides.Opposite(ascent)));
            Assert.That(TileSides.OfInwardYaw(stair.Rotation.Y), Is.EqualTo(StaircaseFlight.LaidAgainst(ascent)));
            Assert.That(ModelPose.RotationOf(stair), Is.EqualTo(stair.Rotation));
        }

        [Test]
        public void ThePinnedFlightProfileIsThePackMeshTheCheckMeasures()
        {
            Assert.That(StaircaseFlight.PackCrestFromItsOriginOnward, Is.Not.Empty);
            Assert.That(
                StaircaseFlight.PackCrestAtItsOrigin,
                Is.EqualTo(DungeonPack.StaircasePackHeight).Within(Tolerance));
            Assert.That(StaircaseFlight.PackCrestAtItsFarEnd, Is.LessThan(StaircaseFlight.PackCrestAtItsOrigin));

            foreach (var slice in StaircaseFlight.PackCrestFromItsOriginOnward)
            {
                Assert.That(slice, Is.GreaterThan(0f));
                Assert.That(slice, Is.LessThanOrEqualTo(DungeonPack.StaircasePackHeight));
            }
        }

        static WorldPart FirstStaircase()
        {
            return LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces())
                .AllParts
                .First(part => part.Model == PartModel.Staircase);
        }

        static WorldPart FirstPlinth()
        {
            return LevelBlueprintBuilder.Build(LevelGenerator.Generate(20250824L, MazePreset.Ship).Graph)
                .AllParts
                .First(part => part.Model == PartModel.Foundation);
        }

        static WorldPart Pickup(PartStyle style)
        {
            var graph = LevelGraphFixture.TwoTerraces();

            return LevelBlueprintBuilder.Build(graph)
                .AllParts
                .First(part => part.Style == style);
        }

        static WorldPart FirstFloor()
        {
            return LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces())
                .AllParts
                .First(part => part.Style == PartStyle.Floor);
        }
    }
}
