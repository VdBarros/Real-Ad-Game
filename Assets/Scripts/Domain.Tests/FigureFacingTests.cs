using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FigureFacingTests
    {
        const float Tolerance = 1e-3f;

        static readonly WorldPoint North = new WorldPoint(0f, 0f, 1f);

        static readonly WorldPoint East = new WorldPoint(1f, 0f, 0f);

        static readonly WorldPoint South = new WorldPoint(0f, 0f, -1f);

        static readonly WorldPoint West = new WorldPoint(-1f, 0f, 0f);

        static readonly WorldPoint[] Cardinals = { North, East, South, West };

        static readonly float[] CardinalYaws = { 0f, 90f, 180f, 270f };

        static void Aimed(float yaw, float wanted, string note)
        {
            Assert.That(
                Math.Abs(FigureFacing.Shortest(yaw, wanted)),
                Is.LessThanOrEqualTo(Tolerance),
                note + ": " + yaw + " against " + wanted);
        }

        [Test]
        public void EachCardinalHeadingHasTheYawUnityTurnsAMeshBy()
        {
            for (var quarter = 0; quarter < Cardinals.Length; quarter++)
            {
                Aimed(
                    FigureFacing.YawOf(Cardinals[quarter]),
                    CardinalYaws[quarter],
                    Cardinals[quarter].ToString());
            }
        }

        [Test]
        public void AYawAndItsHeadingAreTheSameFactTwice()
        {
            for (var yaw = 0f; yaw < 360f; yaw += 7.5f)
            {
                Aimed(FigureFacing.YawOf(FigureFacing.HeadingOf(yaw)), yaw, yaw.ToString());
            }
        }

        [Test]
        public void AHeadingOfNoLengthPointsNowhereToFace()
        {
            Assert.That(FigureFacing.IsAimed(default(WorldPoint)), Is.False);
            Assert.That(FigureFacing.IsAimed(new WorldPoint(0f, 1f, 0f)), Is.False);
            Assert.That(FigureFacing.IsAimed(North), Is.True);
            Assert.That(
                () => FigureFacing.YawOf(default(WorldPoint)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheRestHeadingIsTheLineBackAlongWhichTheCameraLooks()
        {
            var forward = IsoProjection.CameraForward;
            var rest = FigureFacing.Rest;

            Assert.That(rest.Y, Is.EqualTo(0f));
            Assert.That(rest.X * rest.X + rest.Z * rest.Z, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(rest.X, Is.LessThan(0f));
            Assert.That(rest.Z, Is.LessThan(0f));
            Assert.That(rest.X * forward.X + rest.Z * forward.Z, Is.LessThan(0f));
            Assert.That(FigureFacing.Swing(rest), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void ThePackOffsetIsTheYawTheRestHeadingAlreadyWanted()
        {
            Assert.That(
                FigureFacing.RestYaw, Is.EqualTo(AdventurerPack.Facing).Within(Tolerance));
            Assert.That(
                FigureFacing.RestYaw, Is.EqualTo(SkeletonPack.Facing).Within(Tolerance));
        }

        [Test]
        public void AFigureAimedAtTheRestHeadingKeepsThePoseTheImportGaveIt()
        {
            foreach (var baked in new[] { 0f, 90f, 137.5f, 225f, 359.9f })
            {
                Aimed(FigureFacing.Composed(baked, FigureFacing.Rest), baked, baked.ToString());
            }
        }

        [Test]
        public void ThePackOffsetComposesWithTheLiveYawRatherThanReplacingEither()
        {
            foreach (var offset in new[] { 0f, 45f, 137.5f, 300f })
            {
                foreach (var heading in Cardinals)
                {
                    Aimed(
                        FigureFacing.Composed(FigureFacing.RestYaw + offset, heading),
                        FigureFacing.Composed(FigureFacing.RestYaw, heading) + offset,
                        offset + " " + heading);
                }
            }
        }

        [Test]
        public void ComposingThePinnedOffsetWithAHeadingLandsOnThatHeadingsOwnYaw()
        {
            foreach (var model in new[] { PartModel.Knight, PartModel.SkeletonWarrior })
            {
                for (var quarter = 0; quarter < Cardinals.Length; quarter++)
                {
                    Aimed(
                        FigureFacing.Of(model, Cardinals[quarter]),
                        CardinalYaws[quarter],
                        model + " " + Cardinals[quarter]);
                }
            }
        }

        [Test]
        public void NoCardinalLeavesTheMeshMirroredOrSideOn()
        {
            foreach (var heading in Cardinals)
            {
                var pointed = FigureFacing.HeadingOf(FigureFacing.Of(PartModel.Knight, heading));

                Assert.That(
                    pointed.X * heading.X + pointed.Z * heading.Z,
                    Is.EqualTo(1f).Within(Tolerance),
                    heading.ToString());
            }
        }

        [Test]
        public void AHeadingBetweenTwoSpotsIgnoresHowFarApartAndHowHighTheyAre()
        {
            var from = new WorldPoint(3f, 1f, 2f);

            Assert.That(
                FigureFacing.Between(from, new WorldPoint(3f, 9f, 7f)),
                Is.EqualTo(North));
            Assert.That(
                FigureFacing.Between(from, new WorldPoint(1f, 0f, 2f)),
                Is.EqualTo(West));
            Assert.That(
                FigureFacing.IsAimed(FigureFacing.Between(from, new WorldPoint(3f, 4f, 2f))),
                Is.False);
        }

        [Test]
        public void ReversingAHeadingTurnsItHalfACircle()
        {
            foreach (var heading in Cardinals)
            {
                Assert.That(
                    Math.Abs(
                        FigureFacing.Shortest(
                            FigureFacing.YawOf(heading),
                            FigureFacing.YawOf(FigureFacing.Reversed(heading)))),
                    Is.EqualTo(FigureFacing.HalfTurn).Within(Tolerance),
                    heading.ToString());
            }
        }

        [Test]
        public void EveryAngleIsCarriedIntoTheOneCircleTheMeshTurnsThrough()
        {
            Assert.That(FigureFacing.Normalised(0f), Is.EqualTo(0f));
            Assert.That(FigureFacing.Normalised(360f), Is.EqualTo(0f));
            Assert.That(FigureFacing.Normalised(-90f), Is.EqualTo(270f));
            Assert.That(FigureFacing.Normalised(450f), Is.EqualTo(90f));
            Assert.That(FigureFacing.Normalised(-450f), Is.EqualTo(270f));
        }

        [Test]
        public void TheSwingBetweenTwoYawsIsTheShortWayRound()
        {
            Assert.That(FigureFacing.Shortest(350f, 10f), Is.EqualTo(20f).Within(Tolerance));
            Assert.That(FigureFacing.Shortest(10f, 350f), Is.EqualTo(-20f).Within(Tolerance));
            Assert.That(FigureFacing.Shortest(0f, 90f), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(FigureFacing.Shortest(0f, 270f), Is.EqualTo(-90f).Within(Tolerance));
            Assert.That(
                Math.Abs(FigureFacing.Shortest(0f, 180f)),
                Is.EqualTo(180f).Within(Tolerance));

            for (var from = 0f; from < 360f; from += 11f)
            {
                for (var to = 0f; to < 360f; to += 13f)
                {
                    var swing = FigureFacing.Shortest(from, to);

                    Assert.That(Math.Abs(swing), Is.LessThanOrEqualTo(180f + Tolerance));
                    Assert.That(
                        FigureFacing.Normalised(from + swing),
                        Is.EqualTo(FigureFacing.Normalised(to)).Within(Tolerance));
                }
            }
        }
    }
}
