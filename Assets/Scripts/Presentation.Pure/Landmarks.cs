using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class Landmarks
    {
        public const int Fewest = 3;

        public const int Most = 5;

        public const int JunctionDegree = 3;

        public const int TerraceWeight = 8;

        public static IReadOnlyList<LandmarkSpot> Of(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var chosen = SpreadOut(new List<LandmarkSpot>(), Candidates(graph, standingOnIt: true), Most);

            if (chosen.Count < Fewest)
            {
                chosen = SpreadOut(chosen, Candidates(graph, standingOnIt: false), Fewest);
            }

            chosen.Sort(BySweep);

            var turn = Rotation(graph.Seed);
            var spots = new List<LandmarkSpot>(chosen.Count);

            for (var slot = 0; slot < chosen.Count; slot++)
            {
                var kind = LandmarkForm.Kinds[(turn + slot) % LandmarkForm.Kinds.Count];
                spots.Add(new LandmarkSpot(chosen[slot].Tile, chosen[slot].Against, kind));
            }

            return spots;
        }

        public static bool IsJunction(TileGrid tiles, TilePosition position)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            return tiles.Neighbours(position).Count >= JunctionDegree;
        }

        public static bool IsAreaEntrance(TileGrid tiles, TilePosition position)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            var region = tiles.RegionOf(position);

            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (tiles.RegionOf(neighbour) != region)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MarksADecision(TileGrid tiles, TilePosition position)
        {
            return IsJunction(tiles, position) || IsAreaEntrance(tiles, position);
        }

        public static bool Flanks(TileGrid tiles, TilePosition position)
        {
            if (MarksADecision(tiles, position))
            {
                return false;
            }

            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (MarksADecision(tiles, neighbour))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool FlanksAJunction(TileGrid tiles, TilePosition position)
        {
            if (MarksADecision(tiles, position))
            {
                return false;
            }

            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (IsJunction(tiles, neighbour))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryOpenSide(TileGrid tiles, TilePosition position, out TileSide open)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            foreach (var side in TileSides.All)
            {
                var beyond = TileSides.Step(position, side);
                if (!tiles.ContainsPlace(beyond.X, beyond.Y))
                {
                    open = side;
                    return true;
                }
            }

            open = TileSide.North;
            return false;
        }

        public static WorldPoint StandingOf(LandmarkSpot spot)
        {
            var tile = IsoProjection.Of(spot.Tile);
            var toward = TileSides.Toward(spot.Against);
            var standoff = IsoProjection.TileEdge * 0.5f;

            return new WorldPoint(
                tile.X + toward.X * standoff,
                tile.Y + LandmarkForm.Height * 0.5f,
                tile.Z + toward.Z * standoff);
        }

        public static float Clearance
        {
            get { return IsoProjection.TileEdge * 0.5f - LandmarkForm.Reach; }
        }

        static List<LandmarkSpot> Candidates(LevelGraph graph, bool standingOnIt)
        {
            var candidates = new List<LandmarkSpot>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (graph.Decisions.NodeAt(tile.Position) != null)
                {
                    continue;
                }

                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                {
                    continue;
                }

                var marks = standingOnIt
                    ? MarksADecision(graph.Tiles, tile.Position) || FlanksAJunction(graph.Tiles, tile.Position)
                    : Flanks(graph.Tiles, tile.Position);

                if (!marks)
                {
                    continue;
                }

                TileSide open;
                if (!TryOpenSide(graph.Tiles, tile.Position, out open))
                {
                    continue;
                }

                candidates.Add(new LandmarkSpot(tile.Position, open, LandmarkForm.Kinds[0]));
            }

            return candidates;
        }

        static List<LandmarkSpot> SpreadOut(
            List<LandmarkSpot> standing, IReadOnlyList<LandmarkSpot> candidates, int wanted)
        {
            var chosen = new List<LandmarkSpot>(standing);

            if (candidates.Count == 0)
            {
                return chosen;
            }

            var taken = new bool[candidates.Count];

            if (chosen.Count == 0)
            {
                chosen.Add(candidates[0]);
                taken[0] = true;
            }

            if (chosen.Count >= wanted)
            {
                return chosen;
            }

            while (chosen.Count < wanted)
            {
                var best = -1;
                var bestApart = -1;

                for (var slot = 0; slot < candidates.Count; slot++)
                {
                    if (taken[slot] || Holds(chosen, candidates[slot].Tile))
                    {
                        continue;
                    }

                    var nearest = int.MaxValue;
                    foreach (var already in chosen)
                    {
                        var apart = Apart(already.Tile, candidates[slot].Tile);
                        if (apart < nearest)
                        {
                            nearest = apart;
                        }
                    }

                    if (nearest > bestApart)
                    {
                        bestApart = nearest;
                        best = slot;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                taken[best] = true;
                chosen.Add(candidates[best]);
            }

            return chosen;
        }

        static bool Holds(IReadOnlyList<LandmarkSpot> chosen, TilePosition tile)
        {
            foreach (var spot in chosen)
            {
                if (spot.Tile.Equals(tile))
                {
                    return true;
                }
            }

            return false;
        }

        static int Apart(TilePosition first, TilePosition second)
        {
            return Math.Abs(first.X - second.X)
                + Math.Abs(first.Y - second.Y)
                + Math.Abs(first.Elevation - second.Elevation) * TerraceWeight;
        }

        static int Rotation(long seed)
        {
            var kinds = LandmarkForm.Kinds.Count;

            return (int)(((seed % kinds) + kinds) % kinds);
        }

        static int BySweep(LandmarkSpot left, LandmarkSpot right)
        {
            return left.Tile.CompareTo(right.Tile);
        }
    }
}
