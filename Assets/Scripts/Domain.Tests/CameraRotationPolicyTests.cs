using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraRotationPolicyTests
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

        const string RigSource = "CameraRig.cs";

        const string Factory = "public static CameraRig Raise()";

        const string Write = "transform.rotation";

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
        public void TheRigOrientsItsCameraInItsFactoryAndNowhereElse()
        {
            var path = Path.Combine(ScriptsRoot(), "Presentation", RigSource);

            Assert.That(File.Exists(path), Is.True, "The rig moved out of " + path + " and this guard went blind.");

            var source = File.ReadAllText(path);
            var factory = source.IndexOf(Factory, StringComparison.Ordinal);

            Assert.That(factory, Is.GreaterThanOrEqualTo(0), RigSource + " no longer declares " + Factory + ".");
            Assert.That(
                source.Split(new[] { Write }, StringSplitOptions.None).Length - 1,
                Is.EqualTo(1),
                RigSource + " writes rotation more than once, so a playing camera can turn.");
            Assert.That(
                source.IndexOf(Write, StringComparison.Ordinal),
                Is.GreaterThan(factory),
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
    }
}
