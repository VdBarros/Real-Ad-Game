using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class DeadWalk
    {
        static readonly int[] StepsX = { -1, 1, 0, 0 };

        static readonly int[] StepsY = { 0, 0, -1, 1 };

        public static int LongestOf(LevelGraph level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var beats = new List<TilePosition>();
            foreach (var node in level.Decisions.Nodes)
            {
                if (MarksAnEvent(node.Type))
                {
                    beats.Add(node.Position);
                }
            }

            return Longest(level.Tiles, beats);
        }

        public static double SecondsOf(LevelGraph level)
        {
            return Pace.SecondsOf(LongestOf(level));
        }

        internal static int LongestOf(ContentBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var beats = new List<TilePosition>();
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (MarksAnEvent(board.TypeOf(nodeId)))
                {
                    beats.Add(board.PositionOf(nodeId));
                }
            }

            return Longest(board.Tiles, beats);
        }

        public static bool MarksAnEvent(NodeType type)
        {
            return type == NodeType.Start
                || type == NodeType.Boss
                || type == NodeType.Enemy
                || type == NodeType.Additive
                || type == NodeType.Multiplier;
        }

        public static bool ClimbsATerrace(int elevation)
        {
            return !Terraces.IsTerrace(elevation);
        }

        static int Longest(TileGrid tiles, List<TilePosition> beats)
        {
            var ordered = tiles.Tiles;
            var indexOfPlace = new Dictionary<long, int>(ordered.Count);
            for (var tile = 0; tile < ordered.Count; tile++)
            {
                indexOfPlace.Add(PlaceOf(ordered[tile].Position), tile);
            }

            var silence = new int[ordered.Count];
            var queue = new List<int>(ordered.Count);

            for (var tile = 0; tile < ordered.Count; tile++)
            {
                var sounds = ClimbsATerrace(ordered[tile].Position.Elevation);
                silence[tile] = sounds ? 0 : -1;
                if (sounds)
                {
                    queue.Add(tile);
                }
            }

            foreach (var beat in beats)
            {
                int tile;
                if (!indexOfPlace.TryGetValue(PlaceOf(beat), out tile) || silence[tile] == 0)
                {
                    continue;
                }

                silence[tile] = 0;
                queue.Add(tile);
            }

            for (var head = 0; head < queue.Count; head++)
            {
                var here = ordered[queue[head]].Position;
                var reached = silence[queue[head]] + 1;

                for (var step = 0; step < StepsX.Length; step++)
                {
                    int neighbour;
                    if (!indexOfPlace.TryGetValue(
                            PlaceOf(here.X + StepsX[step], here.Y + StepsY[step]), out neighbour)
                        || silence[neighbour] >= 0)
                    {
                        continue;
                    }

                    silence[neighbour] = reached;
                    queue.Add(neighbour);
                }
            }

            var longest = 0;
            for (var tile = 0; tile < ordered.Count; tile++)
            {
                if (silence[tile] < 0)
                {
                    continue;
                }

                var here = ordered[tile].Position;
                for (var step = 0; step < StepsX.Length; step++)
                {
                    int neighbour;
                    if (!indexOfPlace.TryGetValue(
                            PlaceOf(here.X + StepsX[step], here.Y + StepsY[step]), out neighbour)
                        || silence[neighbour] < 0)
                    {
                        continue;
                    }

                    var crossing = silence[tile] + silence[neighbour] + 1;
                    if (crossing > longest)
                    {
                        longest = crossing;
                    }
                }
            }

            return longest;
        }

        static long PlaceOf(TilePosition position)
        {
            return PlaceOf(position.X, position.Y);
        }

        static long PlaceOf(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}
