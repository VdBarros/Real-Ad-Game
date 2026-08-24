using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ContentPlacementSweepTests
    {
        const int ShipSeeds = 500;

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

            var seeds = preset == MazePreset.Ship ? ShipSeeds : 300;
            sweep = new List<PlacedLevel>(seeds);
            for (var seed = 1; seed <= seeds; seed++)
            {
                sweep.Add(LevelGenerator.Generate(seed, preset));
            }

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryRegionHoldsAnEnemyAtOrBelowItsOwnCheapestWayIn(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                foreach (var region in level.Envelope.Regions)
                {
                    if (!region.HoldsAnEnemy)
                    {
                        continue;
                    }

                    Assert.That(
                        region.CheapestEnemy,
                        Is.LessThanOrEqualTo(region.Minimum),
                        "Seed " + level.AttemptSeed + " left nothing edible in " + region + ".");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void OnlyARegionWhoseSoleContentIsTheBossGoesWithoutAnEnemy(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                foreach (var region in level.Envelope.Regions)
                {
                    if (region.HoldsAnEnemy)
                    {
                        continue;
                    }

                    Assert.That(
                        ContentNodesIn(level, region.RegionId),
                        Is.EqualTo(1),
                        "Seed " + level.AttemptSeed + " left " + region + " with treasure and no fight.");
                    Assert.That(
                        level.Graph.RegionOf(level.BossNodeId),
                        Is.EqualTo(region.RegionId),
                        "Seed " + level.AttemptSeed + " left " + region + " with nothing to fight.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void TheWallsOfEveryRegionStayInOrder(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                foreach (var region in level.Envelope.Regions)
                {
                    Assert.That(
                        region.Minimum,
                        Is.LessThanOrEqualTo(region.Maximum),
                        "Seed " + level.AttemptSeed + " inverted the envelope in " + region + ".");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NothingIsGatedBehindTheBoss(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                var decisions = level.Graph.Decisions;
                var seen = new bool[decisions.Nodes.Count];
                var queue = new List<int> { StartOf(level) };
                seen[queue[0]] = true;

                for (var head = 0; head < queue.Count; head++)
                {
                    if (queue[head] == level.BossNodeId)
                    {
                        continue;
                    }

                    foreach (var neighbour in decisions.NeighboursOf(queue[head]))
                    {
                        if (seen[neighbour])
                        {
                            continue;
                        }

                        seen[neighbour] = true;
                        queue.Add(neighbour);
                    }
                }

                foreach (var node in decisions.Nodes)
                {
                    if (node.Type != NodeType.Enemy
                        && node.Type != NodeType.Additive
                        && node.Type != NodeType.Multiplier)
                    {
                        continue;
                    }

                    Assert.That(
                        seen[node.Id],
                        Is.True,
                        "Seed " + level.AttemptSeed + " walled " + node + " off behind the boss.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoSlotSurvivesUnfilledAndTheRecipeIsHonouredExactly(MazePreset preset)
        {
            var recipe = ContentRecipe.For(preset);

            foreach (var level in Sweep(preset))
            {
                var bosses = 0;
                var enemies = 0;
                var additives = 0;
                var multipliers = 0;

                foreach (var node in level.Graph.Decisions.Nodes)
                {
                    Assert.That(
                        node.Type,
                        Is.Not.EqualTo(NodeType.Unassigned),
                        "Seed " + level.AttemptSeed + " shipped an empty slot: " + node + ".");

                    switch (node.Type)
                    {
                        case NodeType.Boss:
                            bosses++;
                            break;
                        case NodeType.Enemy:
                            enemies++;
                            break;
                        case NodeType.Additive:
                            additives++;
                            break;
                        case NodeType.Multiplier:
                            multipliers++;
                            break;
                    }
                }

                Assert.That(bosses, Is.EqualTo(recipe.Bosses));
                Assert.That(enemies, Is.EqualTo(recipe.Enemies));
                Assert.That(additives, Is.EqualTo(recipe.Additives));
                Assert.That(multipliers, Is.EqualTo(recipe.Multipliers));
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoNodeIsMovedAddedOrRemovedByPlacement(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                var before = level.Layout.Graph.Decisions;
                var after = level.Graph.Decisions;

                Assert.That(after.Nodes.Count, Is.EqualTo(before.Nodes.Count));
                Assert.That(after.Corridors.Count, Is.EqualTo(before.Corridors.Count));

                for (var nodeId = 0; nodeId < after.Nodes.Count; nodeId++)
                {
                    Assert.That(after.Nodes[nodeId].Position, Is.EqualTo(before.Nodes[nodeId].Position));
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void InvariantsBAndCHoldOnEveryAcceptedLevel(MazePreset preset)
        {
            foreach (var level in Sweep(preset))
            {
                Assert.That(
                    level.BossPower,
                    Is.LessThan(level.InvariantBBound),
                    "Seed " + level.AttemptSeed + " minted a boss the level cannot ever beat.");
                Assert.That(
                    level.BossPower,
                    Is.GreaterThan(level.ShortestPathPower),
                    "Seed " + level.AttemptSeed + " minted a boss a beeline already beats.");
            }
        }

        [Test]
        public void TheSkillSpreadDoesNotCollapse()
        {
            var spreads = new List<double>();
            foreach (var level in Sweep(MazePreset.Ship))
            {
                foreach (var region in level.Envelope.Regions)
                {
                    if (region.Minimum > 0)
                    {
                        spreads.Add(region.Spread);
                    }
                }
            }

            Assert.That(
                Quantile(spreads, 0.5),
                Is.GreaterThan(8.0),
                "Median spread was " + Quantile(spreads, 0.5) + ".");
            Assert.That(
                Quantile(spreads, 0.9),
                Is.GreaterThan(50.0),
                "p90 spread was " + Quantile(spreads, 0.9) + ".");
            Assert.That(
                Quantile(spreads, 0.1),
                Is.GreaterThanOrEqualTo(1.0),
                "p10 spread was " + Quantile(spreads, 0.1) + ".");
        }

        [Test]
        public void ContentPlacementRejectsFewEnoughSeedsToKeepGenerationCheap()
        {
            var accepted = 0;
            var contentRejections = 0;

            for (var seed = 1; seed <= ShipSeeds; seed++)
            {
                PlacedLevel level;
                LayoutRejection layoutRejection;
                ContentRejection contentRejection;
                if (LevelGenerator.TryGenerate(
                        seed,
                        MazePreset.Ship,
                        ContentRecipe.Ship,
                        PowerTuning.Ship,
                        out level,
                        out layoutRejection,
                        out contentRejection))
                {
                    accepted++;
                }
                else if (layoutRejection == LayoutRejection.None)
                {
                    contentRejections++;
                }
            }

            Console.WriteLine(
                "ship first-attempt acceptance " + accepted + "/" + ShipSeeds
                + ", of the rejections " + contentRejections + " came from content placement");

            Assert.That(
                accepted,
                Is.GreaterThanOrEqualTo((int)(ShipSeeds * 0.85)),
                "Only " + accepted + " of " + ShipSeeds + " seeds survived the first attempt.");
            Assert.That(
                contentRejections,
                Is.LessThanOrEqualTo((int)(ShipSeeds * 0.05)),
                "Content placement alone rejected " + contentRejections + " seeds.");
        }

        [Test]
        public void ReportsTheDistributionsTheEnvelopeWasTunedAgainst()
        {
            var sweep = Sweep(MazePreset.Ship);
            var boss = new List<double>();
            var shortestPath = new List<double>();
            var spreads = new List<double>();
            var values = new List<double>();
            var passes = new List<double>();
            var ones = 0;
            var contentNodes = 0;

            foreach (var level in sweep)
            {
                boss.Add(level.BossPower);
                shortestPath.Add(level.ShortestPathPower);
                passes.Add(level.FloorRepairPasses);

                foreach (var region in level.Envelope.Regions)
                {
                    if (region.Minimum > 0)
                    {
                        spreads.Add(region.Spread);
                    }
                }

                foreach (var node in level.Graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Enemy && node.Type != NodeType.Additive)
                    {
                        continue;
                    }

                    contentNodes++;
                    values.Add(node.Value);
                    if (node.Value == 1)
                    {
                        ones++;
                    }
                }
            }

            Console.WriteLine("ship, " + sweep.Count + " seeds");
            Console.WriteLine("  boss power p10/50/90    " + Band(boss));
            Console.WriteLine("  shortest-path power     " + Band(shortestPath));
            Console.WriteLine("  P_max/P_min spread      " + Band(spreads));
            Console.WriteLine("  minted value            " + Band(values));
            Console.WriteLine("  floor repair passes     " + Band(passes));
            var enemylessRegions = 0;
            foreach (var level in sweep)
            {
                foreach (var region in level.Envelope.Regions)
                {
                    if (!region.HoldsAnEnemy)
                    {
                        enemylessRegions++;
                    }
                }
            }

            Console.WriteLine("  regions holding only the boss  " + enemylessRegions);
            Console.WriteLine("  share of values that are 1  "
                + (100.0 * ones / contentNodes).ToString("F1") + "%");

            Assert.That(sweep.Count, Is.EqualTo(ShipSeeds));
        }

        static int ContentNodesIn(PlacedLevel level, int regionId)
        {
            var held = 0;
            foreach (var node in level.Graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start
                    || node.Type == NodeType.Empty
                    || level.Graph.RegionOf(node.Id) != regionId)
                {
                    continue;
                }

                held++;
            }

            return held;
        }

        static int StartOf(PlacedLevel level)
        {
            foreach (var node in level.Graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return node.Id;
                }
            }

            throw new InvalidOperationException("A level has a start to walk out of.");
        }

        static string Band(List<double> samples)
        {
            return Quantile(samples, 0.1).ToString("F2")
                + " / " + Quantile(samples, 0.5).ToString("F2")
                + " / " + Quantile(samples, 0.9).ToString("F2");
        }

        static double Quantile(List<double> samples, double share)
        {
            if (samples.Count == 0)
            {
                return 0.0;
            }

            var sorted = new List<double>(samples);
            sorted.Sort();
            return sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * share))];
        }
    }
}
