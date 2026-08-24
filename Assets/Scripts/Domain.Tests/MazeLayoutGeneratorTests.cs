using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class MazeLayoutGeneratorTests
    {
        const long Seed = 20250824L;

        [Test]
        public void TheSameSeedProducesByteIdenticalOutput()
        {
            var first = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);
            var second = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            Assert.That(LevelGraphWriter.Write(second.Graph), Is.EqualTo(LevelGraphWriter.Write(first.Graph)));
        }

        [Test]
        public void TheAcceptedAttemptSeedReproducesTheLayoutOnItsOwn()
        {
            var retried = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            MazeLayout direct;
            LayoutRejection rejection;
            Assert.That(
                MazeLayoutGenerator.TryGenerate(retried.AttemptSeed, MazePreset.Ship, out direct, out rejection),
                Is.True);
            Assert.That(LevelGraphWriter.Write(direct.Graph), Is.EqualTo(LevelGraphWriter.Write(retried.Graph)));
        }

        [Test]
        public void TheGraphIsStampedWithTheSeedThatReproducesIt()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            Assert.That(layout.Graph.Seed, Is.EqualTo(layout.AttemptSeed));
            Assert.That(layout.Graph.Preset, Is.EqualTo(MazePreset.Ship.Name));
        }

        [Test]
        public void GenerationReportsHowManyAttemptsItSpentAndOnWhat()
        {
            MazeGenerationReport report;
            MazeLayoutGenerator.Generate(Seed, MazePreset.Ship, out report);

            Assert.That(report.Preset, Is.SameAs(MazePreset.Ship));
            Assert.That(report.Attempts, Is.GreaterThanOrEqualTo(1));
            Assert.That(report.Rejections, Is.EqualTo(report.Attempts - 1));
        }

        [Test]
        public void LayoutEmitsOnlyStartEmptyAndUnassigned()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            foreach (var node in layout.Graph.Decisions.Nodes)
            {
                Assert.That(
                    node.Type == NodeType.Start || node.Type == NodeType.Empty || node.Type == NodeType.Unassigned,
                    Is.True,
                    "Layout decides shape only, but node " + node + " already carries content.");
                Assert.That(node.Value, Is.EqualTo(0));
            }
        }

        [Test]
        public void EverySlotIsAnUnassignedNodeAndTheirCountMatchesThePreset()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            Assert.That(layout.SlotNodeIds.Count, Is.EqualTo(MazePreset.Ship.ContentSlots));
            foreach (var slotId in layout.SlotNodeIds)
            {
                Assert.That(layout.Graph.Decisions.Node(slotId).Type, Is.EqualTo(NodeType.Unassigned));
            }
        }

        [Test]
        public void TheStartIsADeadEndOnTheGroundFloor()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);
            var start = layout.Graph.Decisions.Node(layout.StartNodeId);

            Assert.That(start.Type, Is.EqualTo(NodeType.Start));
            Assert.That(start.Position.Floor, Is.EqualTo(0));
            Assert.That(layout.Graph.Tiles.Neighbours(start.Position).Count, Is.EqualTo(1));
        }

        [Test]
        public void EveryDeadEndHoldsAContentSlot()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            foreach (var node in layout.Graph.Decisions.Nodes)
            {
                if (node.Id == layout.StartNodeId || layout.Graph.Tiles.Neighbours(node.Position).Count != 1)
                {
                    continue;
                }

                Assert.That(
                    node.Type,
                    Is.EqualTo(NodeType.Unassigned),
                    "A dead end with no content is a corridor to nowhere: " + node + ".");
            }
        }

        [Test]
        public void ATinyLevelIsOneFloorWithNoStairs()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Tiny);

            Assert.That(layout.Graph.Tiles.Stairs.Count, Is.EqualTo(0));
            foreach (var tile in layout.Graph.Tiles.Tiles)
            {
                Assert.That(tile.Position.Floor, Is.EqualTo(0));
            }
        }

        [Test]
        public void AStressLevelStitchesThreeFloorsWithoutStackingTwoStairsOnOneTile()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Stress);

            var floors = new HashSet<int>();
            foreach (var tile in layout.Graph.Tiles.Tiles)
            {
                floors.Add(tile.Position.Floor);
            }

            Assert.That(floors.Count, Is.EqualTo(3));
            Assert.That(layout.DistanceFromStart.ReachedCount, Is.EqualTo(layout.Graph.Tiles.Tiles.Count));
        }

        [Test]
        public void TheDeepestSlotSitsAtLeastTheMinimumBossDepthFromTheStart()
        {
            var layout = MazeLayoutGenerator.Generate(Seed, MazePreset.Ship);

            Assert.That(layout.Metrics.BossDepth, Is.GreaterThanOrEqualTo(MazePreset.Ship.MinimumBossDepth));
            Assert.That(
                layout.Metrics.OffPathSlotCount,
                Is.GreaterThanOrEqualTo(MazePreset.Ship.MinimumOffPathSlots));
        }

        [Test]
        public void ASeedTooShallowToMeetTheDepthFloorIsRejectedRatherThanReturned()
        {
            var unreachableDepth = new MazePreset(
                "unreachable-depth", 5, 3, 2, 4, 0.25, 2, 24, minimumBossDepth: 500, minimumOffPathSlots: 8);

            MazeLayout layout;
            LayoutRejection rejection;
            var generated = MazeLayoutGenerator.TryGenerate(Seed, unreachableDepth, out layout, out rejection);

            Assert.That(generated, Is.False);
            Assert.That(layout, Is.Null);
            Assert.That(rejection, Is.EqualTo(LayoutRejection.BossTooShallow));
        }

        [Test]
        public void MoreDeadEndsThanContentSlotsIsAPocketOverflow()
        {
            var starved = new MazePreset(
                "starved", 5, 3, 2, 4, 0.25, 2, contentSlots: 1, minimumBossDepth: 1, minimumOffPathSlots: 0);

            MazeLayout layout;
            LayoutRejection rejection;
            var generated = MazeLayoutGenerator.TryGenerate(Seed, starved, out layout, out rejection);

            Assert.That(generated, Is.False);
            Assert.That(rejection, Is.EqualTo(LayoutRejection.PocketOverflow));
        }

        [Test]
        public void AskingForMoreSlotsThanTheCarveOffersIsAShortfallNotAThinnerLevel()
        {
            var greedy = new MazePreset(
                "greedy", 5, 3, 2, 4, 0.25, 2, contentSlots: 200, minimumBossDepth: 1, minimumOffPathSlots: 0);

            MazeLayout layout;
            LayoutRejection rejection;
            var generated = MazeLayoutGenerator.TryGenerate(Seed, greedy, out layout, out rejection);

            Assert.That(generated, Is.False);
            Assert.That(rejection, Is.EqualTo(LayoutRejection.SlotShortfall));
        }

        [Test]
        public void GenerationGivesUpAfterFiftyAttemptsAndReportsWhy()
        {
            var impossible = new MazePreset(
                "impossible", 5, 3, 2, 4, 0.25, 2, 24, minimumBossDepth: 500, minimumOffPathSlots: 8);

            var thrown = Assert.Throws<MazeGenerationException>(
                () => MazeLayoutGenerator.Generate(Seed, impossible));

            Assert.That(thrown.Attempts, Is.EqualTo(MazeLayoutGenerator.MaximumAttempts));
            Assert.That(thrown.CountOf(LayoutRejection.BossTooShallow), Is.EqualTo(MazeLayoutGenerator.MaximumAttempts));
            Assert.That(thrown.Message, Does.Contain("BossTooShallow"));
        }

        [Test]
        public void APresetMustBeGivenALatticeItCanCarve()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MazePreset("bad", 0, 3, 1, 2, 0.25, 0, 12, 8, 3));
        }

        [Test]
        public void APresetAboveTheGroundFloorMustCarryAStair()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MazePreset("stairless", 5, 3, 2, 4, 0.25, 0, 24, 16, 8));
        }
    }
}
