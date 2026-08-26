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

        public const float RampThickness = 0.6f;

        public const float RampClearance = 0.02f;

        public const float RampHeight = IsoProjection.StepHeight * Terraces.Rise - RampClearance;

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
                target.Add(FloorQuad(tile.Position));

                foreach (var side in TileSides.All)
                {
                    var beyond = TileSides.Step(tile.Position, side);
                    if (!graph.Tiles.ContainsPlace(beyond.X, beyond.Y))
                    {
                        target.Add(Wall(tile.Position, side));
                    }
                }

                if (CarriesRamp(graph.Tiles, tile.Position))
                {
                    target.Add(Ramp(tile.Position));
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

        static bool CarriesRamp(TileGrid tiles, TilePosition position)
        {
            var above = new TilePosition(position.Elevation + Terraces.Rise, position.X, position.Y);
            return tiles.Contains(above) && tiles.AreAdjacent(position, above);
        }

        static WorldPart FloorQuad(TilePosition position)
        {
            return new WorldPart(
                PartNames.Tile(position),
                PartShape.Quad,
                PartStyle.Floor,
                IsoProjection.Of(position),
                new WorldPoint(90f, 0f, 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.TileEdge, 1f));
        }

        static WorldPart Wall(TilePosition position, TileSide side)
        {
            var tile = IsoProjection.Of(position);
            var neighbour = IsoProjection.Of(TileSides.Step(position, side));

            return new WorldPart(
                PartNames.Wall(position, side),
                PartShape.Quad,
                PartStyle.Wall,
                new WorldPoint(
                    (tile.X + neighbour.X) * 0.5f,
                    tile.Y + IsoProjection.WallHeight * 0.5f,
                    (tile.Z + neighbour.Z) * 0.5f),
                new WorldPoint(0f, TileSides.InwardYaw(side), 0f),
                new WorldPoint(IsoProjection.TileEdge, IsoProjection.WallHeight, 1f));
        }

        static WorldPart Ramp(TilePosition position)
        {
            var tile = IsoProjection.Of(position);

            return new WorldPart(
                PartNames.Ramp(position),
                PartShape.Cube,
                PartStyle.Ramp,
                new WorldPoint(tile.X, tile.Y + RampHeight * 0.5f, tile.Z),
                new WorldPoint(0f, 0f, 0f),
                new WorldPoint(RampThickness, RampHeight, RampThickness));
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
                style,
                new WorldPoint(tile.X, tile.Y + PickupScale * 0.5f, tile.Z),
                new WorldPoint(0f, yaw, 0f),
                new WorldPoint(PickupScale, PickupScale, PickupScale));
        }
    }
}
