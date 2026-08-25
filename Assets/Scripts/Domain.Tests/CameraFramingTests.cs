using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraFramingTests
    {
        static readonly Type[] RigTypes =
        {
            typeof(CameraFraming),
            typeof(CameraFlight),
            typeof(ZoomBeat),
            typeof(CameraStaging),
            typeof(LevelFraming)
        };

        static readonly string[] TurningWords = { "rotation", "pitch", "yaw", "roll", "euler", "tilt" };

        [Test]
        public void TheRigExposesNoFieldForRotation()
        {
            foreach (var type in RigTypes)
            {
                foreach (var member in type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    var name = member.Name.ToLowerInvariant();
                    Assert.That(
                        TurningWords.Any(word => name.Contains(word)),
                        Is.False,
                        type.Name + "." + member.Name + " names an orientation. The rig never writes rotation, "
                        + "so the constant stays in IsoProjection where nothing can animate it.");
                }
            }
        }

        [Test]
        public void TheRigOrientsItsCameraOnceAtConstructionAndNeverAgain()
        {
            var source = File.ReadAllText(Path.Combine(ScriptsRoot(), "Presentation", "CameraRig.cs"));
            var writes = source.Split(new[] { "transform.rotation" }, StringSplitOptions.None).Length - 1;

            Assert.That(writes, Is.EqualTo(1), "The rig writes rotation somewhere other than its factory.");
            Assert.That(
                source.IndexOf("transform.rotation", StringComparison.Ordinal),
                Is.LessThan(source.IndexOf("public void Begin(", StringComparison.Ordinal)),
                "The one rotation write must be the constant the factory stamps, not a frame of playing camera.");
        }

        static string ScriptsRoot([CallerFilePath] string sourceFile = "")
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFile));
            while (directory != null && directory.Name != "Scripts")
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "No Scripts folder above " + sourceFile + ".");
            return directory.FullName;
        }

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
        public void TheCameraBasisIsOrthonormalAndMatchesThePitchAndYawTheBadgesCopy()
        {
            var forward = IsoProjection.CameraForward;
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;

            Assert.That(CameraGeometry.Dot(forward, forward), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CameraGeometry.Dot(right, right), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CameraGeometry.Dot(up, up), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CameraGeometry.Dot(forward, right), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(CameraGeometry.Dot(forward, up), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(CameraGeometry.Dot(right, up), Is.EqualTo(0f).Within(1e-5f));

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
    }
}
