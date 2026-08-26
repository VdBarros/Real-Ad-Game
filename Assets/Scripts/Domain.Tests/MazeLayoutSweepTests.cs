using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class MazeLayoutSweepTests
    {
        const int ShipSeeds = 1000;

        static readonly Dictionary<string, List<MazeLayout>> SweepByPreset =
            new Dictionary<string, List<MazeLayout>>();

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
            yield return MazePreset.Stress;
        }

        static List<MazeLayout> Sweep(MazePreset preset)
        {
            List<MazeLayout> sweep;
            if (SweepByPreset.TryGetValue(preset.Name, out sweep))
            {
                return sweep;
            }

            var seeds = preset == MazePreset.Ship ? ShipSeeds : preset == MazePreset.Tiny ? 300 : 100;
            sweep = new List<MazeLayout>(seeds);
            for (var seed = 1; seed <= seeds; seed++)
            {
                sweep.Add(MazeLayoutGenerator.Generate(seed, preset));
            }

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoTwoTilesEverStandInTheSamePlace(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                var occupant = new Dictionary<long, TilePosition>();

                foreach (var tile in layout.Graph.Tiles.Tiles)
                {
                    TilePosition already;
                    if (occupant.TryGetValue(Place(tile.Position), out already))
                    {
                        Assert.Fail(
                            "Seed " + layout.AttemptSeed + " put " + tile.Position + " and " + already
                            + " in the same place, so one of them is hidden under the other.");
                    }

                    occupant.Add(Place(tile.Position), tile.Position);
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EverySeedProducesAGraphConnectedAcrossEveryTerrace(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                Assert.That(
                    layout.DistanceFromStart.ReachedCount,
                    Is.EqualTo(layout.Graph.Tiles.Tiles.Count),
                    "Seed " + layout.AttemptSeed + " left tiles the start cannot walk to.");

                var elevations = new HashSet<int>();
                foreach (var tile in layout.Graph.Tiles.Tiles)
                {
                    if (Terraces.IsTerrace(tile.Position.Elevation))
                    {
                        elevations.Add(tile.Position.Elevation);
                    }
                }

                Assert.That(
                    elevations.Count,
                    Is.EqualTo(preset.Terraces),
                    "Seed " + layout.AttemptSeed + " lost a terrace.");
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EverySeedIsByteIdenticalWhenGeneratedAgain(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                MazeLayout again;
                LayoutRejection rejection;
                Assert.That(
                    MazeLayoutGenerator.TryGenerate(layout.AttemptSeed, preset, out again, out rejection),
                    Is.True);
                Assert.That(LevelGraphWriter.Write(again.Graph), Is.EqualTo(LevelGraphWriter.Write(layout.Graph)));
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void RegionsAreContiguousTotalAndNeverSpanATerrace(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                var grid = layout.Graph.Tiles;
                var elevationOfRegion = new Dictionary<int, int>();
                var membersOfRegion = new Dictionary<int, List<TilePosition>>();

                foreach (var tile in grid.Tiles)
                {
                    if (!membersOfRegion.ContainsKey(tile.RegionId))
                    {
                        membersOfRegion.Add(tile.RegionId, new List<TilePosition>());
                    }

                    membersOfRegion[tile.RegionId].Add(tile.Position);

                    if (!Terraces.IsTerrace(tile.Position.Elevation))
                    {
                        continue;
                    }

                    int elevation;
                    if (elevationOfRegion.TryGetValue(tile.RegionId, out elevation))
                    {
                        Assert.That(
                            elevation,
                            Is.EqualTo(tile.Position.Elevation),
                            "Seed " + layout.AttemptSeed + " region " + tile.RegionId + " spans two terraces.");
                    }
                    else
                    {
                        elevationOfRegion.Add(tile.RegionId, tile.Position.Elevation);
                    }
                }

                Assert.That(
                    membersOfRegion.Count,
                    Is.EqualTo(preset.RegionsPerTerrace * preset.Terraces),
                    "Seed " + layout.AttemptSeed + " did not paint every region.");

                foreach (var region in membersOfRegion)
                {
                    Assert.That(
                        IsFourConnected(grid, region.Value),
                        Is.True,
                        "Seed " + layout.AttemptSeed + " region " + region.Key + " is not contiguous.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryRegionHoldsAtLeastOneSlot(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                var regionsHoldingASlot = new HashSet<int>();
                foreach (var slotId in layout.SlotNodeIds)
                {
                    regionsHoldingASlot.Add(layout.Graph.RegionOf(slotId));
                }

                Assert.That(
                    regionsHoldingASlot.Count,
                    Is.EqualTo(preset.RegionsPerTerrace * preset.Terraces),
                    "Seed " + layout.AttemptSeed + " left a region with nothing in it to scale.");
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryAcceptedSeedClearsTheGeometricProxyForInvariantC(MazePreset preset)
        {
            foreach (var layout in Sweep(preset))
            {
                Assert.That(layout.Metrics.BossDepth, Is.GreaterThanOrEqualTo(preset.MinimumBossDepth));
                Assert.That(layout.Metrics.OffPathSlotCount, Is.GreaterThanOrEqualTo(preset.MinimumOffPathSlots));
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void AStaircaseBelongsToTheRegionOfTheTerraceItLeaves(MazePreset preset)
        {
            var climbed = 0;

            foreach (var layout in Sweep(preset))
            {
                var terraceOfRegion = TerraceOfEveryRegion(layout);

                foreach (var tile in layout.Graph.Tiles.Tiles)
                {
                    if (Terraces.IsTerrace(tile.Position.Elevation))
                    {
                        continue;
                    }

                    Assert.That(
                        terraceOfRegion[tile.RegionId],
                        Is.EqualTo(tile.Position.Elevation - 1),
                        "Seed " + layout.AttemptSeed + " put the staircase tile at " + tile.Position
                        + " in a region of the terrace at elevation " + terraceOfRegion[tile.RegionId]
                        + " rather than the one it leaves.");
                    climbed++;
                }
            }

            Assert.That(
                climbed > 0,
                Is.EqualTo(preset.Terraces > 1),
                "Seed sweep of " + preset + " did not carve the ways up the preset asks for.");
        }

        [Test]
        public void NoCorridorJoinsTwoEmptyNodesWithNothingToWalkBetweenThem()
        {
            var climbs = 0;

            foreach (var layout in Sweep(MazePreset.Ship))
            {
                foreach (var corridor in layout.Graph.Decisions.Corridors)
                {
                    var low = layout.Graph.Decisions.Node(corridor.LowNodeId);
                    var high = layout.Graph.Decisions.Node(corridor.HighNodeId);

                    if (low.Position.Elevation != high.Position.Elevation)
                    {
                        Assert.That(
                            corridor.TilePath.Count,
                            Is.GreaterThan(0),
                            "Seed " + layout.AttemptSeed + " climbs from " + low.Position + " to "
                            + high.Position + " with no ground in between.");
                        climbs++;
                    }

                    if (corridor.TilePath.Count != 0)
                    {
                        continue;
                    }

                    Assert.That(
                        low.Type == NodeType.Empty && high.Type == NodeType.Empty,
                        Is.False,
                        "Seed " + layout.AttemptSeed + " joined two Empty nodes with a zero-length corridor.");
                }
            }

            Assert.That(climbs, Is.GreaterThan(0), "No seed ever climbed, so nothing was exercised.");
        }

        [Test]
        public void TwoRoutesBetweenTheSameNodesAreSplitByAnEmptyNodeMidCorridor()
        {
            var split = 0;

            foreach (var layout in Sweep(MazePreset.Ship))
            {
                foreach (var node in layout.Graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Empty
                        || layout.Graph.Tiles.Neighbours(node.Position).Count != 2)
                    {
                        continue;
                    }

                    split++;
                }
            }

            Assert.That(
                split,
                Is.GreaterThan(0),
                "No corridor was ever split, so nothing proves the graph model's refusal of "
                + "parallel corridors is being honoured by construction rather than by luck.");
        }

        [Test]
        public void TheShipLevelKeepsTheShapeTheBraidWasMeasuredAt()
        {
            var sweep = Sweep(MazePreset.Ship);
            var nodes = 0.0;
            var corridors = 0.0;
            var empties = 0.0;
            var gateRatio = 0.0;
            var pockets = 0.0;

            foreach (var layout in sweep)
            {
                Assert.That(TerraceTilesOf(layout), Is.EqualTo(60));
                Assert.That(layout.Metrics.TileCount, Is.GreaterThan(60));
                Assert.That(layout.Metrics.SlotCount, Is.EqualTo(MazePreset.Ship.ContentSlots));
                Assert.That(
                    layout.Metrics.NodeCount,
                    Is.EqualTo(layout.Metrics.SlotCount + layout.Metrics.EmptyCount + 1));

                nodes += layout.Metrics.NodeCount;
                corridors += layout.Metrics.CorridorCount;
                empties += layout.Metrics.EmptyCount;
                gateRatio += layout.Metrics.GateRatio;
                pockets += layout.Metrics.PocketSlotCount;
            }

            nodes /= sweep.Count;
            corridors /= sweep.Count;
            empties /= sweep.Count;
            gateRatio /= sweep.Count;
            pockets /= sweep.Count;

            Assert.That(nodes, Is.EqualTo(28.0).Within(1.0), "Mean node count was " + nodes + ".");
            Assert.That(corridors, Is.EqualTo(30.0).Within(1.5), "Mean corridor count was " + corridors + ".");
            Assert.That(empties, Is.EqualTo(3.0).Within(1.0), "Mean Empty count was " + empties + ".");
            Assert.That(gateRatio, Is.EqualTo(0.30).Within(0.06), "Mean gate ratio was " + gateRatio + ".");
            Assert.That(pockets, Is.EqualTo(2.3).Within(0.6), "Mean pocket count was " + pockets + ".");
        }

        [Test]
        public void RejectionRatesStayInsideTheBandTheBraidWasTunedAgainst()
        {
            Assert.That(AcceptedOf(MazePreset.Ship, ShipSeeds), Is.GreaterThanOrEqualTo(850));
            Assert.That(AcceptedOf(MazePreset.Tiny, ShipSeeds), Is.GreaterThanOrEqualTo(600));
        }

        static int AcceptedOf(MazePreset preset, int seeds)
        {
            var accepted = 0;
            for (var seed = 1; seed <= seeds; seed++)
            {
                MazeLayout layout;
                LayoutRejection rejection;
                if (MazeLayoutGenerator.TryGenerate(seed, preset, out layout, out rejection))
                {
                    accepted++;
                }
            }

            return accepted;
        }

        static Dictionary<int, int> TerraceOfEveryRegion(MazeLayout layout)
        {
            var terraceOfRegion = new Dictionary<int, int>();

            foreach (var tile in layout.Graph.Tiles.Tiles)
            {
                if (Terraces.IsTerrace(tile.Position.Elevation))
                {
                    terraceOfRegion[tile.RegionId] = tile.Position.Elevation;
                }
            }

            return terraceOfRegion;
        }

        static int TerraceTilesOf(MazeLayout layout)
        {
            var standing = 0;
            foreach (var tile in layout.Graph.Tiles.Tiles)
            {
                if (Terraces.IsTerrace(tile.Position.Elevation))
                {
                    standing++;
                }
            }

            return standing;
        }

        static long Place(TilePosition position)
        {
            return ((long)position.X << 32) ^ (uint)position.Y;
        }

        static bool IsFourConnected(TileGrid grid, IReadOnlyList<TilePosition> members)
        {
            var inside = new HashSet<TilePosition>(members);
            var seen = new HashSet<TilePosition> { members[0] };
            var queue = new List<TilePosition> { members[0] };

            for (var head = 0; head < queue.Count; head++)
            {
                foreach (var neighbour in grid.Neighbours(queue[head]))
                {
                    if (!inside.Contains(neighbour) || !seen.Add(neighbour))
                    {
                        continue;
                    }

                    queue.Add(neighbour);
                }
            }

            return seen.Count == members.Count;
        }
    }
}
