using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SolvabilitySweepTests
    {
        static readonly Dictionary<string, List<PlacedLevel>> SweepByPreset =
            new Dictionary<string, List<PlacedLevel>>();

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        static List<PlacedLevel> Sweep(MazePreset preset)
        {
            List<PlacedLevel> sweep;
            if (SweepByPreset.TryGetValue(preset.Name, out sweep))
            {
                return sweep;
            }

            var seeds = preset == MazePreset.Ship ? 500 : 300;
            sweep = new List<PlacedLevel>(seeds);
            for (var seed = 1; seed <= seeds; seed++)
            {
                sweep.Add(LevelGenerator.Generate(seed, preset));
            }

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryAcceptedLevelIsSafe(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                var verdict = SolvabilityValidator.Validate(level.Graph, level.Tuning);

                Assert.That(verdict.IsSafe, Is.True, "Seed " + level.AttemptSeed + ": " + verdict);
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoPolicyOnThePanelStrandsAnAcceptedLevel(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                foreach (var policy in AdversaryPanel.Policies)
                {
                    Assert.That(
                        AdversaryPanel.Walk(level.Graph, level.Tuning, policy),
                        Is.Null,
                        "Seed " + level.AttemptSeed + " strands " + policy + ".");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void ThePanelAgreesWithTheVerdictItIsPartOf(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                Assert.That(
                    level.Verdict.Stall,
                    Is.Null,
                    "Seed " + level.AttemptSeed + " shipped carrying a stall report.");
                Assert.That(level.Verdict.BossNodeId, Is.EqualTo(level.BossNodeId));
                Assert.That(level.Verdict.Bound, Is.EqualTo(PowerBound.Of(level.Graph, level.Tuning)));
            }
        }

        [Test]
        public void InflatingOneEnemyStrandsTheLevelThePanelJustCleared()
        {
            var caught = 0;
            var mutated = 0;

            foreach (var level in Sweep(MazePreset.Tiny))
            {
                foreach (var node in level.Graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Enemy)
                    {
                        continue;
                    }

                    mutated++;
                    var broken = Inflated(level.Graph, node.Id, 50);
                    if (AdversaryPanel.FirstStall(broken, level.Tuning) != null)
                    {
                        caught++;
                    }
                }
            }

            Console.WriteLine("panel caught " + caught + " of " + mutated + " fifty-fold enemy inflations");

            Assert.That(mutated, Is.GreaterThan(0));
            Assert.That(
                caught,
                Is.GreaterThan((int)(mutated * 0.5)),
                "The panel let " + (mutated - caught) + " of " + mutated + " broken levels through.");
        }

        static LevelGraph Inflated(LevelGraph level, int nodeId, int factor)
        {
            var builder = new LevelGraphBuilder(level.Seed, level.Preset);

            foreach (var tile in level.Tiles.Tiles)
            {
                builder.AddTile(tile.Position, tile.RegionId);
            }

            foreach (var stair in level.Tiles.Stairs)
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            foreach (var node in level.Decisions.Nodes)
            {
                builder.AddNode(
                    node.Position, node.Type, node.Id == nodeId ? node.Value * factor : node.Value);
            }

            foreach (var corridor in level.Decisions.Corridors)
            {
                builder.Connect(
                    level.Decisions.Node(corridor.LowNodeId).Position,
                    level.Decisions.Node(corridor.HighNodeId).Position,
                    corridor.TilePath);
            }

            return builder.Build();
        }
    }
}
