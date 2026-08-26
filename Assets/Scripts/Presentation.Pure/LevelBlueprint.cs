using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class LevelBlueprint
    {
        readonly List<WorldPart> allParts;

        public LevelBlueprint(IReadOnlyList<TerraceBlueprint> terraces)
        {
            if (terraces == null)
            {
                throw new ArgumentNullException(nameof(terraces));
            }

            Terraces = terraces;
            RootName = PartNames.Root;

            allParts = new List<WorldPart>();
            foreach (var terrace in terraces)
            {
                allParts.AddRange(terrace.Tiles);
                allParts.AddRange(terrace.Nodes);
            }
        }

        public string RootName { get; }

        public IReadOnlyList<TerraceBlueprint> Terraces { get; }

        public IReadOnlyList<WorldPart> AllParts
        {
            get { return allParts; }
        }
    }
}
