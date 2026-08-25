using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class AssemblyBoundaryTests
    {
        static readonly string[] ArchitecturalAssemblies =
        {
            "Game.Domain",
            "Game.Domain.Tests",
            "Game.Presentation.Pure",
            "Game.Presentation",
            "Game.Interaction",
            "Game.Flow"
        };

        static readonly string[] EngineFreeAssemblies =
        {
            "Game.Domain",
            "Game.Presentation.Pure"
        };

        [Test]
        public void EveryArchitecturalAssemblyExists()
        {
            foreach (var assembly in ArchitecturalAssemblies)
            {
                Assert.That(AsmdefPath(assembly), Is.Not.Null, $"No asmdef found for {assembly}.");
            }
        }

        [Test]
        public void EngineFreeAssembliesDeclareNoEngineReferences()
        {
            foreach (var assembly in EngineFreeAssemblies)
            {
                Assert.That(
                    Regex.IsMatch(Asmdef(assembly), "\"noEngineReferences\"\\s*:\\s*true"),
                    Is.True,
                    $"{assembly} must set noEngineReferences to true.");
            }
        }

        [Test]
        public void EngineFreeAssembliesContainNoUnityEngineUsage()
        {
            foreach (var assembly in EngineFreeAssemblies)
            {
                var folder = Path.GetDirectoryName(AsmdefPath(assembly));
                foreach (var source in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
                {
                    Assert.That(
                        File.ReadAllText(source),
                        Does.Not.Contain("UnityEngine"),
                        $"{source} references UnityEngine but belongs to engine-free {assembly}.");
                }
            }
        }

        [Test]
        public void DomainReferencesNothing()
        {
            Assert.That(References("Game.Domain"), Is.Empty);
        }

        [Test]
        public void PresentationPureReferencesOnlyDomain()
        {
            Assert.That(References("Game.Presentation.Pure"), Is.EqualTo(new[] { "Game.Domain" }));
        }

        static IReadOnlyList<string> References(string assembly)
        {
            var body = Regex.Match(Asmdef(assembly), "\"references\"\\s*:\\s*\\[(?<body>[^\\]]*)\\]");
            Assert.That(body.Success, Is.True, $"{assembly} has no references array.");

            return Regex.Matches(body.Groups["body"].Value, "\"(?<name>[^\"]+)\"")
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .ToList();
        }

        static string Asmdef(string assembly)
        {
            return File.ReadAllText(AsmdefPath(assembly));
        }

        static string AsmdefPath(string assembly)
        {
            return SourceTree.PathTo(assembly + ".asmdef");
        }
    }
}
