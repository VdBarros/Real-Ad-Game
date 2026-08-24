using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class LevelBlueprint
    {
        readonly List<WorldPart> allParts;

        public LevelBlueprint(IReadOnlyList<FloorBlueprint> floors)
        {
            if (floors == null)
            {
                throw new ArgumentNullException(nameof(floors));
            }

            Floors = floors;
            RootName = PartNames.Root;

            allParts = new List<WorldPart>();
            foreach (var floor in floors)
            {
                allParts.AddRange(floor.Tiles);
                allParts.AddRange(floor.Nodes);
            }
        }

        public string RootName { get; }

        public IReadOnlyList<FloorBlueprint> Floors { get; }

        public IReadOnlyList<WorldPart> AllParts
        {
            get { return allParts; }
        }
    }
}
