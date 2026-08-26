using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class TerraceBlueprint
    {
        public TerraceBlueprint(int elevation, IReadOnlyList<WorldPart> tiles, IReadOnlyList<WorldPart> nodes)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            Elevation = elevation;
            Name = PartNames.Terrace(elevation);
            Tiles = tiles;
            Nodes = nodes;
        }

        public int Elevation { get; }

        public string Name { get; }

        public IReadOnlyList<WorldPart> Tiles { get; }

        public IReadOnlyList<WorldPart> Nodes { get; }
    }
}
