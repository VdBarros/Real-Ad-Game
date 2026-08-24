using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class FloorBlueprint
    {
        public FloorBlueprint(int floor, IReadOnlyList<WorldPart> tiles, IReadOnlyList<WorldPart> nodes)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            Floor = floor;
            Name = PartNames.Floor(floor);
            Tiles = tiles;
            Nodes = nodes;
        }

        public int Floor { get; }

        public string Name { get; }

        public IReadOnlyList<WorldPart> Tiles { get; }

        public IReadOnlyList<WorldPart> Nodes { get; }
    }
}
