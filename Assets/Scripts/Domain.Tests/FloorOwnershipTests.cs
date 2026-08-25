using System;
using System.Linq;
using System.Reflection;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FloorOwnershipTests
    {
        static readonly Type[] StructuralTypes =
        {
            typeof(Corridor),
            typeof(DecisionGraph),
            typeof(DecisionNode),
            typeof(Tile),
            typeof(TileGrid),
            typeof(StairLink)
        };

        static readonly string[] OwningWords = { "owner", "owned", "owns", "guard", "cleared", "cursed" };

        static readonly string[] ReadingSources = { "FloorReading.cs", "FloorSweep.cs", "FloorState.cs" };

        [Test]
        public void NothingStructuralNamesAnOwningEnemyOrAFloorState()
        {
            foreach (var type in StructuralTypes)
            {
                foreach (var member in type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    var name = member.Name.ToLowerInvariant();

                    Assert.That(
                        OwningWords.Any(word => name.Contains(word)),
                        Is.False,
                        type.Name + "." + member.Name + " reads as stored floor state. Cleared is a reading of the "
                        + "consumed set, so nothing in the level structure may hold it or the enemy behind it.");
                }
            }
        }

        [Test]
        public void ACorridorCarriesItsTwoEndpointsAndItsTilesAndNothingElse()
        {
            var fields = typeof(Corridor)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(
                fields,
                Is.EqualTo(new[] { "<HighNodeId>k__BackingField", "<LowNodeId>k__BackingField", "tilePath" }),
                "A corridor grew a field. The one it must never grow is the enemy that clears it.");
        }

        [Test]
        public void TheFloorReadingNeverConsultsACorridor()
        {
            foreach (var source in ReadingSources)
            {
                Assert.That(
                    SourceTree.Read(File(source)),
                    Does.Not.Contain("Corridor"),
                    source + " reaches for a corridor. The reading runs over tiles and the consumed set alone.");
            }
        }

        [Test]
        public void TheFloorNeverAsksWhereTheCameraIs()
        {
            foreach (var source in ReadingSources)
            {
                foreach (var word in new[] { "Camera", "CameraRig", "Framing" })
                {
                    Assert.That(
                        SourceTree.Read(File(source)),
                        Does.Not.Contain(word),
                        source + " reads the camera. Floor state is a reading of the run, so a cut cannot disturb it.");
                }
            }
        }

        [Test]
        public void TheBuilderNeverRaisesAPartAlreadyCleared()
        {
            var blueprint = LevelBlueprintBuilder.Build(LevelGraphFixture.TwoFloors());

            Assert.That(
                blueprint.AllParts.Any(part => part.Style == PartStyle.Cleared),
                Is.False,
                "Cleared names a material the floor state swaps in, never a part the builder emits.");
        }

        static string[] File(string source)
        {
            return new[] { Folder(source), source };
        }

        static string Folder(string source)
        {
            return source == "FloorState.cs" ? "Presentation" : "Presentation.Pure";
        }
    }
}
