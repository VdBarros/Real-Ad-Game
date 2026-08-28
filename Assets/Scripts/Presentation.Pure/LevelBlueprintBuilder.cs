using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class LevelBlueprintBuilder
    {
        public const float FigureScale = 0.4f;

        public const float BossScale = 0.64f;

        public const float PickupScale = 0.5f;

        public static bool IsWalkingSurface(PartStyle style)
        {
            return style == PartStyle.Floor || style == PartStyle.Staircase;
        }

        public static string WalkingSurfaceOf(TileGrid tiles, TilePosition position)
        {
            return TileFootings.Under(tiles, position) == TileFooting.Flight
                ? PartNames.Stair(position)
                : PartNames.Tile(position);
        }

        public static LevelBlueprint Build(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var elevations = new List<int>();
            var tilesByTerrace = new List<List<WorldPart>>();
            var nodesByTerrace = new List<List<WorldPart>>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var slot = TerraceSlot(elevations, tilesByTerrace, nodesByTerrace, TerraceUnder(tile.Position));
                var target = tilesByTerrace[slot];
                var footing = TileFootings.Under(graph.Tiles, tile.Position);

                if (footing != TileFooting.Flight)
                {
                    target.Add(FloorQuad(tile.Position));
                }

                switch (footing)
                {
                    case TileFooting.Flight:
                        target.Add(Staircase(
                            tile.Position, TileFootings.AscentOf(graph.Tiles, tile.Position)));
                        break;
                    case TileFooting.Plinth:
                        target.Add(Plinth(tile.Position));
                        break;
                }

                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (!graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        target.Add(Wall(tile.Position, side));
                    }
                }
            }

            foreach (var node in graph.Decisions.Nodes)
            {
                WorldPart prop;
                if (!TryProp(node, out prop))
                {
                    continue;
                }

                var slot = TerraceSlot(elevations, tilesByTerrace, nodesByTerrace, TerraceUnder(node.Position));
                nodesByTerrace[slot].Add(prop);
            }

            var terraces = new List<TerraceBlueprint>(elevations.Count);
            for (var slot = 0; slot < elevations.Count; slot++)
            {
                terraces.Add(new TerraceBlueprint(elevations[slot], tilesByTerrace[slot], nodesByTerrace[slot]));
            }

            return new LevelBlueprint(terraces);
        }

        static int TerraceSlot(
            List<int> elevations,
            List<List<WorldPart>> tilesByTerrace,
            List<List<WorldPart>> nodesByTerrace,
            int elevation)
        {
            for (var slot = 0; slot < elevations.Count; slot++)
            {
                if (elevations[slot] == elevation)
                {
                    return slot;
                }
            }

            elevations.Add(elevation);
            tilesByTerrace.Add(new List<WorldPart>());
            nodesByTerrace.Add(new List<WorldPart>());
            return elevations.Count - 1;
        }

        static int TerraceUnder(TilePosition position)
        {
            return Terraces.ElevationOf(Terraces.TerraceUnder(position.Elevation));
        }

        static WorldPart FloorQuad(TilePosition position)
        {
            return new WorldPart(
                PartNames.Tile(position),
                PartShape.Quad,
                PartModels.Of(PartStyle.Floor),
                PartStyle.Floor,
                IsoProjection.Of(position),
                new WorldPoint(90f, 0f, 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.TileEdge, 1f));
        }

        static WorldPart Staircase(TilePosition position, TileSide ascent)
        {
            var tile = IsoProjection.Of(position);

            return new WorldPart(
                PartNames.Stair(position),
                PartShape.Cube,
                PartModels.Of(PartStyle.Staircase),
                PartStyle.Staircase,
                new WorldPoint(tile.X, tile.Y - IsoProjection.StepHeight * 0.5f, tile.Z),
                new WorldPoint(0f, TileSides.InwardYaw(StaircaseFlight.LaidAgainst(ascent)), 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.StepHeight, IsoProjection.TileEdge));
        }

        static WorldPart Plinth(TilePosition position)
        {
            var tile = IsoProjection.Of(position);

            return new WorldPart(
                PartNames.Footing(position),
                PartShape.Cube,
                PartModels.Of(PartStyle.Foundation),
                PartStyle.Foundation,
                new WorldPoint(tile.X, tile.Y - IsoProjection.StepHeight * 0.5f, tile.Z),
                new WorldPoint(0f, 0f, 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.StepHeight, IsoProjection.TileEdge));
        }

        static WorldPart Wall(TilePosition position, TileSide side)
        {
            var tile = IsoProjection.Of(position);
            var neighbour = IsoProjection.Of(TileSides.Step(position, side));

            return new WorldPart(
                PartNames.Wall(position, side),
                PartShape.Quad,
                PartModels.Of(PartStyle.Wall),
                PartStyle.Wall,
                new WorldPoint(
                    (tile.X + neighbour.X) * 0.5f,
                    tile.Y + IsoProjection.WallHeight * 0.5f,
                    (tile.Z + neighbour.Z) * 0.5f),
                new WorldPoint(0f, TileSides.InwardYaw(side), 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.WallHeight, 1f));
        }

        public static bool TryProp(DecisionNode node, out WorldPart prop)
        {
            switch (node.Type)
            {
                case NodeType.Start:
                    prop = Figure(node, PartStyle.Start, FigureScale);
                    return true;
                case NodeType.Enemy:
                    prop = Figure(node, PartStyle.Enemy, FigureScale);
                    return true;
                case NodeType.Boss:
                    prop = Figure(node, PartStyle.Boss, BossScale);
                    return true;
                case NodeType.Additive:
                    prop = Pickup(node, PartStyle.Additive, 0f);
                    return true;
                case NodeType.Multiplier:
                    prop = Pickup(node, PartStyle.Multiplier, 45f);
                    return true;
                default:
                    prop = default(WorldPart);
                    return false;
            }
        }

        static WorldPart Figure(DecisionNode node, PartStyle style, float scale)
        {
            var tile = IsoProjection.Of(node.Position);

            return new WorldPart(
                PartNames.Node(node.Id),
                PartShape.Capsule,
                PartModels.Of(style, node.Value),
                style,
                new WorldPoint(tile.X, tile.Y + scale, tile.Z),
                new WorldPoint(0f, 0f, 0f),
                new WorldPoint(scale, scale, scale));
        }

        static WorldPart Pickup(DecisionNode node, PartStyle style, float yaw)
        {
            var tile = IsoProjection.Of(node.Position);

            return new WorldPart(
                PartNames.Node(node.Id),
                PartShape.Cube,
                PartModels.Of(style),
                style,
                new WorldPoint(tile.X, tile.Y + PickupScale * 0.5f, tile.Z),
                new WorldPoint(0f, yaw, 0f),
                new WorldPoint(PickupScale, PickupScale, PickupScale));
        }
    }
}
