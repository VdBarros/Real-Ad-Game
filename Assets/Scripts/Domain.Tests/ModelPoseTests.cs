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
        }

        static WorldPart FirstFloor()
        {
            return LevelBlueprintBuilder.Build(LevelGraphFixture.TwoTerraces())
                .AllParts
                .First(part => part.Style == PartStyle.Floor);
        }
    }
}
