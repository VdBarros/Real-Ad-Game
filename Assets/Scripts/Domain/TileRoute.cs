using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Domain
{
    public sealed class TileRoute
    {
        readonly List<int> nodes;
        readonly List<TilePosition> tiles;
        readonly List<int> tileOfNode;

        TileRoute(List<int> nodes, List<TilePosition> tiles, List<int> tileOfNode)
        {
            this.nodes = nodes;
            this.tiles = tiles;
            this.tileOfNode = tileOfNode;
        }

        public static TileRoute Of(LevelGraph level, IReadOnlyList<int> route)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (route.Count == 0)
            {
                throw new ArgumentException("A route starts at the node the walker stands on.", nameof(route));
            }

            var decisions = level.Decisions;
            var nodes = new List<int>(route);
            var tiles = new List<TilePosition> { decisions.Node(nodes[0]).Position };
            var tileOfNode = new List<int>(nodes.Count) { 0 };

            for (var step = 1; step < nodes.Count; step++)
            {
                var corridor = decisions.CorridorBetween(nodes[step - 1], nodes[step]);
                if (corridor == null)
                {
                    throw new ArgumentException(
                        "No corridor joins node " + nodes[step - 1] + " to node " + nodes[step]
                        + ", so a walk between them would cross a wall.",
                        nameof(route));
                }

                tiles.AddRange(corridor.TilesLeaving(nodes[step - 1]));
                tiles.Add(decisions.Node(nodes[step]).Position);
                tileOfNode.Add(tiles.Count - 1);
            }

            return new TileRoute(nodes, tiles, tileOfNode);
        }

        public IReadOnlyList<int> Nodes
        {
            get { return nodes; }
        }

        public IReadOnlyList<TilePosition> Tiles
        {
            get { return tiles; }
        }

        public int Steps
        {
            get { return tiles.Count - 1; }
        }

        public int TileOf(int step)
        {
            if (step < 0 || step >= tileOfNode.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(step), step, "The route passes no node at that step.");
            }

            return tileOfNode[step];
        }

        public TileRoute Upto(int step)
        {
            if (step < 0 || step >= nodes.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(step), step, "The route passes no node at that step.");
            }

            if (step == nodes.Count - 1)
            {
                return this;
            }

            return new TileRoute(
                nodes.GetRange(0, step + 1),
                tiles.GetRange(0, tileOfNode[step] + 1),
                tileOfNode.GetRange(0, step + 1));
        }

        public override string ToString()
        {
            return string.Concat(
                nodes.Count.ToString(CultureInfo.InvariantCulture),
                " nodes over ",
                Steps.ToString(CultureInfo.InvariantCulture),
                " steps");
        }
    }
}
