using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ContentPlacementTests
    {
        const long Seed = 20250824L;

        static PlacedLevel Ship()
        {
            return LevelGenerator.Generate(Seed, MazePreset.Ship);
        }

        [Test]
        public void TheSameSeedProducesByteIdenticalContent()
        {
            var first = LevelGenerator.Generate(Seed, MazePreset.Ship);
            var second = LevelGenerator.Generate(Seed, MazePreset.Ship);

            Assert.That(LevelGraphWriter.Write(second.Graph), Is.EqualTo(LevelGraphWriter.Write(first.Graph)));
        }

        [Test]
        public void TheAcceptedAttemptSeedReproducesTheLevelOnItsOwn()
        {
            var retried = Ship();

            PlacedLevel direct;
            LayoutRejection layoutRejection;
            ContentRejection contentRejection;
            Assert.That(
                LevelGenerator.TryGenerate(
                    retried.AttemptSeed,
                    MazePreset.Ship,
                    ContentRecipe.Ship,
                    PowerTuning.Ship,
                    out direct,
                    out layoutRejection,
                    out contentRejection),
                Is.True);
            Assert.That(LevelGraphWriter.Write(direct.Graph), Is.EqualTo(LevelGraphWriter.Write(retried.Graph)));
        }

        [Test]
        public void PlacementFillsTheRecipeAndNothingElse()
        {
            var level = Ship();
            var counts = new Dictionary<NodeType, int>();

            foreach (var node in level.Graph.Decisions.Nodes)
            {
                int held;
                counts.TryGetValue(node.Type, out held);
                counts[node.Type] = held + 1;
            }

            Assert.That(Count(counts, NodeType.Boss), Is.EqualTo(ContentRecipe.Ship.Bosses));
            Assert.That(Count(counts, NodeType.Multiplier), Is.EqualTo(ContentRecipe.Ship.Multipliers));
            Assert.That(Count(counts, NodeType.Enemy), Is.EqualTo(ContentRecipe.Ship.Enemies));
            Assert.That(Count(counts, NodeType.Additive), Is.EqualTo(ContentRecipe.Ship.Additives));
            Assert.That(Count(counts, NodeType.Start), Is.EqualTo(1));
            Assert.That(Count(counts, NodeType.Unassigned), Is.EqualTo(0));
        }

        [Test]
        public void PlacementMovesAddsAndRemovesNothing()
        {
            var level = Ship();
            var layout = level.Layout.Graph;

            Assert.That(level.Graph.Decisions.Nodes.Count, Is.EqualTo(layout.Decisions.Nodes.Count));
            Assert.That(level.Graph.Decisions.Corridors.Count, Is.EqualTo(layout.Decisions.Corridors.Count));
            Assert.That(level.Graph.Tiles.Equals(layout.Tiles), Is.True);

            foreach (var node in level.Graph.Decisions.Nodes)
            {
                var before = layout.Decisions.Node(node.Id);
                Assert.That(node.Position, Is.EqualTo(before.Position), "Node " + node.Id + " moved.");
                Assert.That(
                    before.Type == NodeType.Unassigned || before.Type == node.Type,
                    Is.True,
                    "Placement rewrote the geometric node " + before + ".");
            }

            for (var index = 0; index < level.Graph.Decisions.Corridors.Count; index++)
            {
                Assert.That(
                    level.Graph.Decisions.Corridors[index],
                    Is.EqualTo(layout.Decisions.Corridors[index]));
            }
        }

        [Test]
        public void TheBossIsTheDeepestSlotAndSitsAtTheEndOfTheDetour()
        {
            var level = Ship();
            var boss = level.Graph.Decisions.Node(level.BossNodeId);

            Assert.That(boss.Type, Is.EqualTo(NodeType.Boss));
            Assert.That(
                level.DistanceFromStart.DistanceTo(boss.Position),
                Is.EqualTo(level.Metrics.BossDepth));
            Assert.That(level.BossPower, Is.GreaterThan(level.ShortestPathPower));
            Assert.That(level.BossPower, Is.LessThan(level.InvariantBBound));
        }

        [Test]
        public void ARecipeThatDoesNotMatchTheCarvesSlotCountIsRejectedRatherThanSquashed()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            PlacedLevel placed;
            ContentRejection rejection;
            var filled = ContentPlacer.TryPlace(
                layout, ContentRecipe.Tiny, PowerTuning.Ship, out placed, out rejection);

            Assert.That(filled, Is.False);
            Assert.That(placed, Is.Null);
            Assert.That(rejection, Is.EqualTo(ContentRejection.RecipeSlotMismatch));
        }

        [Test]
        public void EveryPresetsRecipeAsksForExactlyTheSlotsItsCarveOffers()
        {
            Assert.That(ContentRecipe.Tiny.Slots, Is.EqualTo(MazePreset.Tiny.ContentSlots));
            Assert.That(ContentRecipe.Ship.Slots, Is.EqualTo(MazePreset.Ship.ContentSlots));
            Assert.That(ContentRecipe.Stress.Slots, Is.EqualTo(MazePreset.Stress.ContentSlots));
        }

        [Test]
        public void GenerationGivesUpAfterFiftyAttemptsAndReportsWhy()
        {
            var impossible = new MazePreset(
                "impossible", 5, 3, 2, 4, 0.25, 2, 24, minimumBossDepth: 500, minimumOffPathSlots: 8);

            var thrown = Assert.Throws<LevelGenerationException>(
                () => LevelGenerator.Generate(Seed, impossible, ContentRecipe.Ship, PowerTuning.Ship, out _));

            Assert.That(thrown.Attempts, Is.EqualTo(LevelGenerator.MaximumAttempts));
            Assert.That(
                thrown.CountOf(LayoutRejection.BossTooShallow),
                Is.EqualTo(LevelGenerator.MaximumAttempts));
            Assert.That(thrown.Message, Does.Contain("BossTooShallow"));
        }

        [Test]
        public void ASolvabilityReasonFillsTheHistogramTheCapThrowsWith()
        {
            var greedy = new PowerTuning(2, 600, 0.6, 0.2, 0.9999, 0.8, 0.7, 0.0);

            var thrown = Assert.Throws<LevelGenerationException>(
                () => LevelGenerator.Generate(Seed, MazePreset.Ship, ContentRecipe.Ship, greedy, out _));

            Assert.That(thrown.Attempts, Is.EqualTo(LevelGenerator.MaximumAttempts));
            Assert.That(thrown.Report.Rejections, Is.EqualTo(LevelGenerator.MaximumAttempts));
            Assert.That(
                thrown.CountOf(ContentRejection.BossBeyondBound),
                Is.GreaterThan(0),
                "The histogram lost the reason the validator gave: " + thrown.Report);
            Assert.That(thrown.Message, Does.Contain("BossBeyondBound"));
        }

        [Test]
        public void APresetWithNoFiledRecipeIsAnArgumentErrorRatherThanAGuess()
        {
            var unknown = new MazePreset("unknown", 5, 3, 1, 2, 0.25, 0, 11, 1, 0);

            Assert.Throws<ArgumentException>(() => ContentRecipe.For(unknown));
            Assert.Throws<ArgumentException>(() => PowerTuning.For(unknown));
        }

        [Test]
        public void TheStressPresetFillsNinetySlotsAcrossThreeTerraces()
        {
            var level = LevelGenerator.Generate(Seed, MazePreset.Stress);
            var content = 0;

            foreach (var node in level.Graph.Decisions.Nodes)
            {
                Assert.That(node.Type, Is.Not.EqualTo(NodeType.Unassigned));
                if (node.Type != NodeType.Start && node.Type != NodeType.Empty)
                {
                    content++;
                }
            }

            Assert.That(content, Is.EqualTo(ContentRecipe.Stress.Slots));
            Assert.That(level.Envelope.Regions.Count, Is.EqualTo(MazePreset.Stress.RegionsPerTerrace * 3));
            Assert.That(level.BossPower, Is.GreaterThan(level.ShortestPathPower));
            Assert.That(level.BossPower, Is.LessThan(level.InvariantBBound));
        }

        [Test]
        public void EveryValueOnTheBoardIsPositiveAndOnlyContentCarriesOne()
        {
            foreach (var node in Ship().Graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start || node.Type == NodeType.Empty)
                {
                    Assert.That(node.Value, Is.EqualTo(0), "Geometry carries no number: " + node + ".");
                    continue;
                }

                Assert.That(node.Value, Is.GreaterThan(0), "Content with no number: " + node + ".");
            }
        }

        [Test]
        public void AFilledLevelSurvivesADocumentRoundTrip()
        {
            var level = Ship();
            var document = LevelGraphWriter.Write(level.Graph);

            Assert.That(LevelGraphWriter.Write(LevelGraphReader.Read(document)), Is.EqualTo(document));
        }

        [Test]
        public void MultipliersComeFromTheLadderTheAdShowed()
        {
            foreach (var node in Ship().Graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Multiplier)
                {
                    continue;
                }

                Assert.That(PowerTuning.MultiplierLadder, Does.Contain(node.Value));
            }
        }

        static int Count(Dictionary<NodeType, int> counts, NodeType type)
        {
            int held;
            return counts.TryGetValue(type, out held) ? held : 0;
        }
    }
}
