using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class TerraceBlueprint
    {
        public TerraceBlueprint(
            int elevation,
            IReadOnlyList<WorldPart> tiles,
            IReadOnlyList<WorldPart> nodes,
            IReadOnlyList<WorldPart> landmarks)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (landmarks == null)
            {
                throw new ArgumentNullException(nameof(landmarks));
            }

            Elevation = elevation;
            Name = PartNames.Terrace(elevation);
            Tiles = tiles;
            Nodes = nodes;
            Landmarks = landmarks;
        }

        public int Elevation { get; }

        public string Name { get; }

        public IReadOnlyList<WorldPart> Tiles { get; }

        public IReadOnlyList<WorldPart> Nodes { get; }

        public IReadOnlyList<WorldPart> Landmarks { get; }
    }
}
