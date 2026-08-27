using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FastLoopCeilingTests
    {
        const string TestSourceGlob = "$(ScriptsRoot)Domain.Tests\\**\\*.cs";

        [Test]
        public void TheConformanceCompileUsesTheEditorsNUnitAndNotTheFastLoops()
        {
            Assert.That(
                Conformance(),
                Does.Contain("<PackageReference Include=\"NUnit\" Version=\"$(UnityNUnitVersion)\" />"),
                "Game.Domain.Tests.Conformance must compile against Unity's NUnit, or it stops catching an API the Editor lacks.");

            Assert.That(
                Conformance(),
                Does.Not.Contain("FastLoopNUnitVersion"),
                "Game.Domain.Tests.Conformance must not borrow the runnable NUnit the fast loop executes against.");
        }

        [Test]
        public void TheConformanceCompileTargetsTheFrameworkUnityCompiles()
        {
            Assert.That(
                Conformance(),
                Does.Contain("<TargetFramework>$(UnityTargetFramework)</TargetFramework>"));
        }

        [Test]
        public void TheConformanceCompileSeesEveryTestSourceTheFastLoopRuns()
        {
            Assert.That(Conformance(), Does.Contain(TestSourceGlob));
            Assert.That(FastLoop(), Does.Contain(TestSourceGlob));
        }

        [Test]
        public void TheFastLoopBuildsTheConformanceCompileWithoutLinkingIt()
        {
            Assert.That(
                OneLine(FastLoop()),
                Does.Contain(
                    "<ProjectReference Include=\"..\\Game.Domain.Tests.Conformance\\Game.Domain.Tests.Conformance.csproj\" " +
                    "ReferenceOutputAssembly=\"false\" />"),
                "Without this reference `dotnet test dotnet/Game.Domain.Tests` never builds the conformance compile, so the guard goes silent.");
        }

        [Test]
        public void CiAssertsTheNUnitApiProbeStillFailsToCompile()
        {
            var workflow = OneLine(Read(".github", "workflows", "domain-tests.yml"));

            Assert.That(workflow, Does.Contain("probe-must-fail.sh dotnet/NUnitApiProbe CS1503"));
            Assert.That(workflow, Does.Contain("probe-must-fail.sh dotnet/LangVersionProbe CS8773"));
        }

        [Test]
        public void TheNUnitApiProbeIsPinnedToTheEditorsNUnit()
        {
            Assert.That(
                Read("dotnet", "NUnitApiProbe", "NUnitApiProbe.csproj"),
                Does.Contain("<PackageReference Include=\"NUnit\" Version=\"$(UnityNUnitVersion)\" />"));
        }

        static string Conformance()
        {
            return Read("dotnet", "Game.Domain.Tests.Conformance", "Game.Domain.Tests.Conformance.csproj");
        }

        static string FastLoop()
        {
            return Read("dotnet", "Game.Domain.Tests", "Game.Domain.Tests.csproj");
        }

        static string OneLine(string text)
        {
            var withoutLineContinuations = Regex.Replace(text, "\\\\\\r?\\n", " ");

            return Regex.Replace(withoutLineContinuations, "\\s+", " ");
        }

        static string Read(params string[] pathParts)
        {
            var path = Path.Combine(RepositoryRoot(), Path.Combine(pathParts));

            Assert.That(File.Exists(path), Is.True, "No file at " + path + ", so this guard went blind.");

            return File.ReadAllText(path);
        }

        static string RepositoryRoot()
        {
            return Directory.GetParent(SourceTree.Root()).Parent.FullName;
        }
    }
}
