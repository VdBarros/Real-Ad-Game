using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class MazeLayoutSweepTests
    {
        const int Seeds = 1000;

        static readonly List<MazeLayout> Sweep = new List<MazeLayout>(Seeds);

        [OneTimeSetUp]
        public void GenerateTheSweep()
        {
            if (Sweep.Count > 0)
            {
                return;
            }

            for (var seed = 1; seed <= Seeds; seed++)
            {
                Sweep.Add(MazeLayoutGenerator.Generate(seed, MazePreset.Ship));
            }
        }

        [Test]
        public void EverySeedProducesAGraphConnectedAcrossBothFloors()
        {
            foreach (var layout in Sweep)
            {
                Assert.That(
                    layout.DistanceFromStart.ReachedCount,
                    Is.EqualTo(layout.Graph.Tiles.Tiles.Count),
                    "Seed " + layout.Seed + " left tiles the start cannot walk to.");

                var floors = new HashSet<int>();
                foreach (var tile in layout.Graph.Tiles.Tiles)
                {
                    floors.Add(tile.Position.Floor);
                }

                Assert.That(floors.Count, Is.EqualTo(2), "Seed " + layout.Seed + " lost a floor.");
            }
        }

        [Test]
        public void EverySeedIsByteIdenticalWhenGeneratedAgain()
        {
            foreach (var layout in Sweep)
            {
                var again = MazeLayoutGenerator.Generate(layout.Seed, MazePreset.Ship);
                Assert.That(LevelGraphWriter.Write(again.Graph), Is.EqualTo(LevelGraphWriter.Write(layout.Graph)));
            }
        }

        [Test]
        public void RegionsAreContiguousTotalAndNeverSpanAFloor()
        {
            foreach (var layout in Sweep)
            {
                var grid = layout.Graph.Tiles;
                var floorOfRegion = new Dictionary<int, int>();
                var membersOfRegion = new Dictionary<int, List<TilePosition>>();

                foreach (var tile in grid.Tiles)
                {
                    int floor;
                    if (floorOfRegion.TryGetValue(tile.RegionId, out floor))
                    {
                        Assert.That(
                            floor,
                            Is.EqualTo(tile.Position.Floor),
                            "Seed " + layout.Seed + " region " + tile.RegionId + " spans two floors.");
                    }
                    else
                    {
                        floorOfRegion.Add(tile.RegionId, tile.Position.Floor);
                        membersOfRegion.Add(tile.RegionId, new List<TilePosition>());
                    }

                    membersOfRegion[tile.RegionId].Add(tile.Position);
                }

                Assert.That(
                    membersOfRegion.Count,
                    Is.EqualTo(MazePreset.Ship.Regions),
                    "Seed " + layout.Seed + " did not paint every region.");

                foreach (var region in membersOfRegion)
                {
                    Assert.That(
                        IsFourConnected(grid, region.Value),
                        Is.True,
                        "Seed " + layout.Seed + " region " + region.Key + " is not contiguous.");
                }
            }
        }

        [Test]
        public void AnEmptyCorridorJoinsTwoStairTilesAndNothingElse()
        {
            foreach (var layout in Sweep)
            {
                foreach (var corridor in layout.Graph.Decisions.Corridors)
                {
                    if (corridor.TilePath.Count != 0)
                    {
                        continue;
                    }

                    var low = layout.Graph.Decisions.Node(corridor.LowNodeId);
                    var high = layout.Graph.Decisions.Node(corridor.HighNodeId);
                    if (low.Type != NodeType.Empty || high.Type != NodeType.Empty)
                    {
                        continue;
                    }

                    Assert.That(
                        CarriesStair(layout.Graph.Tiles, low.Position) && CarriesStair(layout.Graph.Tiles, high.Position),
                        Is.True,
                        "Seed " + layout.Seed + " joined two Empty nodes with a zero-length corridor that is not a stair.");
                }
            }
        }

        [Test]
        public void TheMeasuredGateRatioAndPocketCountMatchBraidingAtAQuarter()
        {
            var gateRatio = 0.0;
            var pockets = 0.0;

            foreach (var layout in Sweep)
            {
                gateRatio += layout.Metrics.GateRatio;
                pockets += layout.Metrics.PocketCount;
            }

            gateRatio /= Sweep.Count;
            pockets /= Sweep.Count;

            Assert.That(gateRatio, Is.EqualTo(0.30).Within(0.06), "Mean gate ratio was " + gateRatio + ".");
            Assert.That(pockets, Is.EqualTo(2.3).Within(0.6), "Mean pocket count was " + pockets + ".");
        }

        [Test]
        public void EveryAcceptedSeedClearsTheGeometricProxyForInvariantC()
        {
            foreach (var layout in Sweep)
            {
                Assert.That(layout.Metrics.BossDepth, Is.GreaterThanOrEqualTo(MazePreset.Ship.MinimumBossDepth));
                Assert.That(layout.Metrics.OffPathSlotCount, Is.GreaterThanOrEqualTo(MazePreset.Ship.MinimumOffPathSlots));
            }
        }

        [Test]
        public void RejectionRatesStayInsideTheBandTheBraidWasTunedAgainst()
        {
            Assert.That(AcceptedOf(MazePreset.Ship), Is.GreaterThanOrEqualTo(850));
            Assert.That(AcceptedOf(MazePreset.Tiny), Is.GreaterThanOrEqualTo(600));
        }

        static int AcceptedOf(MazePreset preset)
        {
            var accepted = 0;
            for (var seed = 1; seed <= Seeds; seed++)
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

        static bool CarriesStair(TileGrid grid, TilePosition position)
        {
            foreach (var stair in grid.Stairs)
            {
                if (stair.Lower.Equals(position) || stair.Upper.Equals(position))
                {
                    return true;
                }
            }

            return false;
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
                    if (neighbour.Floor != queue[head].Floor || !inside.Contains(neighbour) || !seen.Add(neighbour))
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
