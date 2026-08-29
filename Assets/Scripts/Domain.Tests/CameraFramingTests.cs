using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void ACameraSitsBackAlongItsOwnForwardAxisByAConstant()
        {
            var framing = new CameraFraming(new WorldPoint(3f, 2f, 5f), 9.5f);
            var forward = IsoProjection.CameraForward;

            Assert.That(
                framing.Position.X + forward.X * IsoProjection.CameraBack,
                Is.EqualTo(framing.Target.X).Within(1e-4f));
            Assert.That(
                framing.Position.Y + forward.Y * IsoProjection.CameraBack,
                Is.EqualTo(framing.Target.Y).Within(1e-4f));
            Assert.That(
                framing.Position.Z + forward.Z * IsoProjection.CameraBack,
                Is.EqualTo(framing.Target.Z).Within(1e-4f));
        }

        [Test]
        public void WhatTheCameraLooksAtSitsTheBackOffsetAheadOfIt()
        {
            var framing = new CameraFraming(new WorldPoint(3f, 2f, 5f), 9.5f);

            Assert.That(framing.DepthOf(framing.Target), Is.EqualTo(IsoProjection.CameraBack).Within(1e-4f));
            Assert.That(framing.DepthOf(framing.Position), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TheCameraBasisIsOrthonormalAndMatchesThePitchAndYawTheBadgesCopy()
        {
            var forward = IsoProjection.CameraForward;
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;

            Assert.That(WorldPoint.Dot(forward, forward), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(WorldPoint.Dot(right, right), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(WorldPoint.Dot(up, up), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(WorldPoint.Dot(forward, right), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(WorldPoint.Dot(forward, up), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(WorldPoint.Dot(right, up), Is.EqualTo(0f).Within(1e-5f));

            Assert.That(forward.Y, Is.EqualTo(-0.5f).Within(1e-5f));
            Assert.That(right.Y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void AFramingIsBoundedAtBothEndsOfAnInterpolation()
        {
            var from = new CameraFraming(new WorldPoint(0f, 0f, 0f), 4.2f);
            var to = new CameraFraming(new WorldPoint(4f, 1f, 6f), 9.5f);

            Assert.That(CameraFraming.Between(from, to, 0f), Is.EqualTo(from));
            Assert.That(CameraFraming.Between(from, to, -1f), Is.EqualTo(from));
            Assert.That(CameraFraming.Between(from, to, 1f), Is.EqualTo(to));
            Assert.That(CameraFraming.Between(from, to, 2f), Is.EqualTo(to));
        }

        [Test]
        public void AFramingWithoutASizeShowsNothingAndIsRefused()
        {
            Assert.That(
                () => new CameraFraming(new WorldPoint(0f, 0f, 0f), 0f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ThePlayFramingIsSetByTheShareOfScreenTheFigureFillsRatherThanASize()
        {
            Assert.That(LevelFraming.FigureHeightFraction, Is.EqualTo(0.07f));
            Assert.That(
                LevelFraming.ShareOfScreen(LevelFraming.FigureHeight, LevelFraming.PlaySize),
                Is.EqualTo(LevelFraming.FigureHeightFraction).Within(1e-5f));
        }

        [Test]
        public void TheFigureTheFramingIsBuiltRoundIsThePlayersOwn()
        {
            Assert.That(
                LevelFraming.FigureHeight,
                Is.EqualTo(
                    FigureFit.StandingHeight(
                        CharacterCast.MeshOf(PartStyle.Start), LevelBlueprintBuilder.FigureScale))
                    .Within(1e-5f));
            Assert.That(LevelFraming.FigureHeight, Is.GreaterThan(0f));
        }

        [Test]
        public void TheFramingClosedInFromTheOneThatShowedEverythingAtOnce()
        {
            const float WasShowingTheWholeLevel = 9.5f;

            Assert.That(
                LevelFraming.ShareOfScreen(LevelFraming.FigureHeight, WasShowingTheWholeLevel),
                Is.EqualTo(0.034f).Within(0.001f));
            Assert.That(LevelFraming.PlaySize, Is.LessThan(WasShowingTheWholeLevel));
            Assert.That(LevelFraming.PlaySize, Is.EqualTo(4.571f).Within(0.001f));
        }

        [Test]
        public void ASizeAndTheShareItShowsAreEachOthersInverse()
        {
            Assert.That(
                LevelFraming.SizeShowing(LevelFraming.FigureHeight, LevelFraming.FigureHeightFraction),
                Is.EqualTo(LevelFraming.PlaySize).Within(1e-5f));
            Assert.That(
                () => LevelFraming.SizeShowing(0f, 0.07f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => LevelFraming.SizeShowing(1f, 0f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => LevelFraming.SizeShowing(1f, 1.5f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => LevelFraming.ShareOfScreen(1f, 0f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheCloseUpIsARatioOfThePlayFramingAndNotASizeOfItsOwn()
        {
            Assert.That(
                LevelFraming.CloseUpSize,
                Is.EqualTo(LevelFraming.PlaySize / ZoomBeat.Punch).Within(1e-5f));
            Assert.That(
                LevelFraming.PlaySize / LevelFraming.CloseUpSize,
                Is.EqualTo(ZoomBeat.Punch).Within(1e-4f));
            Assert.That(
                LevelFraming.ShareOfScreen(LevelFraming.FigureHeight, LevelFraming.CloseUpSize),
                Is.EqualTo(LevelFraming.FigureHeightFraction * ZoomBeat.Punch).Within(1e-5f));
        }

        [Test]
        public void ATighterFramingPutsMorePixelsOnAMetre()
        {
            Assert.That(
                ScreenFrame.PixelsPerMetre(LevelFraming.OpeningSize),
                Is.GreaterThan(ScreenFrame.PixelsPerMetre(LevelFraming.PlaySize)));
            Assert.That(
                ScreenFrame.PixelsPerMetre(LevelFraming.PlaySize) * LevelFraming.PlaySize * 2f,
                Is.EqualTo((float)ScreenFrame.Height).Within(1e-3f));
        }

        [Test]
        public void ATileCoversLessScreenThanItsEdgeSquaredBecauseTheCameraLooksAcrossIt()
        {
            var perMetre = ScreenFrame.PixelsPerMetre(LevelFraming.PlaySize);
            var ground = ScreenFrame.TileGroundPixels(LevelFraming.PlaySize);

            Assert.That(ground, Is.LessThan(perMetre * perMetre));
            Assert.That(
                ground,
                Is.EqualTo(perMetre * perMetre * (float)Math.Sin(IsoProjection.CameraPitch * Math.PI / 180.0))
                    .Within(1e-2f));
        }

        [Test]
        public void ATighterFramingPutsMoreScreenOnATile()
        {
            Assert.That(
                ScreenFrame.TileGroundPixels(LevelFraming.OpeningSize),
                Is.GreaterThan(ScreenFrame.TileGroundPixels(LevelFraming.PlaySize)));
            Assert.That(
                () => ScreenFrame.TileGroundPixels(0f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
