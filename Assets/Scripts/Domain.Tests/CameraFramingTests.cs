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
        public void ATighterFramingPutsMorePixelsOnAMetre()
        {
            Assert.That(
                ScreenFrame.PixelsPerMetre(LevelFraming.OpeningSize),
                Is.GreaterThan(ScreenFrame.PixelsPerMetre(IsoProjection.OrthographicSize)));
            Assert.That(
                ScreenFrame.PixelsPerMetre(IsoProjection.OrthographicSize) * IsoProjection.OrthographicSize * 2f,
                Is.EqualTo((float)ScreenFrame.Height).Within(1e-3f));
        }
    }
}
