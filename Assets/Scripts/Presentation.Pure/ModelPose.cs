using System;

namespace Game.Presentation.Pure
{
    public static class ModelPose
    {
        public const float ChestFacing = 180f;

        public static WorldPoint PositionOf(WorldPart part)
        {
            switch (part.Model)
            {
                case PartModel.WallPanel:
                case PartModel.Chest:
                case PartModel.CoinStack:
                case PartModel.Foundation:
                case PartModel.Pillar:
                case PartModel.Candle:
                case PartModel.Column:
                case PartModel.TorchLit:
                case PartModel.BarrelLarge:
                case PartModel.CratesStacked:
                case PartModel.SwordShield:
                    return Footed(part);
                case PartModel.Staircase:
                    return Crested(part);
                case PartModel.Knight:
                case PartModel.Barbarian:
                case PartModel.Rogue:
                case PartModel.Mage:
                case PartModel.SkeletonMinion:
                case PartModel.SkeletonRogue:
                case PartModel.SkeletonWarrior:
                case PartModel.SkeletonMage:
                    return Standing(part);
                case PartModel.SwordA:
                case PartModel.AxeB:
                case PartModel.StaffB:
                case PartModel.BowA:
                case PartModel.None:
                case PartModel.FloorTile:
                    return part.Position;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Model, "No model position for that part model.");
            }
        }

        public static WorldPoint RotationOf(WorldPart part)
        {
            switch (part.Model)
            {
                case PartModel.FloorTile:
                    return new WorldPoint(0f, part.Rotation.Y, 0f);
                case PartModel.Chest:
                    return new WorldPoint(part.Rotation.X, part.Rotation.Y + ChestFacing, part.Rotation.Z);
                case PartModel.Knight:
                case PartModel.Barbarian:
                case PartModel.Rogue:
                case PartModel.Mage:
                case PartModel.SkeletonMinion:
                case PartModel.SkeletonRogue:
                case PartModel.SkeletonWarrior:
                case PartModel.SkeletonMage:
                    return new WorldPoint(
                        part.Rotation.X,
                        part.Rotation.Y + ArtPacks.FacingOf(part.Model),
                        part.Rotation.Z);
                case PartModel.SwordA:
                case PartModel.AxeB:
                case PartModel.StaffB:
                case PartModel.BowA:
                case PartModel.None:
                case PartModel.WallPanel:
                case PartModel.CoinStack:
                case PartModel.Staircase:
                case PartModel.Foundation:
                case PartModel.Pillar:
                case PartModel.Candle:
                case PartModel.Column:
                case PartModel.TorchLit:
                case PartModel.BarrelLarge:
                case PartModel.CratesStacked:
                case PartModel.SwordShield:
                    return part.Rotation;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Model, "No model pose for that part model.");
            }
        }

        public static WorldPoint ScaleOf(WorldPart part)
        {
            switch (part.Model)
            {
                case PartModel.FloorTile:
                    return new WorldPoint(part.Scale.X, part.Scale.Z, part.Scale.Y);
                case PartModel.WallPanel:
                    return Spanning(part);
                case PartModel.Chest:
                case PartModel.CoinStack:
                    return Fitted(part);
                case PartModel.Staircase:
                case PartModel.Foundation:
                case PartModel.Pillar:
                case PartModel.Candle:
                case PartModel.Column:
                case PartModel.TorchLit:
                case PartModel.BarrelLarge:
                case PartModel.CratesStacked:
                case PartModel.SwordShield:
                    return Boxed(part);
                case PartModel.Knight:
                case PartModel.Barbarian:
                case PartModel.Rogue:
                case PartModel.Mage:
                case PartModel.SkeletonMinion:
                case PartModel.SkeletonRogue:
                case PartModel.SkeletonWarrior:
                case PartModel.SkeletonMage:
                    return Sized(part);
                case PartModel.SwordA:
                case PartModel.AxeB:
                case PartModel.StaffB:
                case PartModel.BowA:
                case PartModel.None:
                    return part.Scale;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Model, "No model scale for that part model.");
            }
        }

        static WorldPoint Footed(WorldPart part)
        {
            return new WorldPoint(
                part.Position.X,
                part.Position.Y - part.Scale.Y * (0.5f + DungeonPack.BaseShareOf(part.Model)),
                part.Position.Z);
        }

        static WorldPoint Fitted(WorldPart part)
        {
            var square = part.Scale.Y / DungeonPack.HeightOf(part.Model);

            return new WorldPoint(square, square, square);
        }

        static WorldPoint Crested(WorldPart part)
        {
            var behind = TileSides.Toward(TileSides.Opposite(TileSides.OfInwardYaw(part.Rotation.Y)));

            return new WorldPoint(
                part.Position.X + behind.X * part.Scale.Z * 0.5f,
                part.Position.Y - part.Scale.Y * 0.5f,
                part.Position.Z + behind.Z * part.Scale.Z * 0.5f);
        }

        static WorldPoint Standing(WorldPart part)
        {
            var ground = part.Position.Y - part.Scale.Y * FigureFit.LiftOf(PartModel.None);

            return new WorldPoint(
                part.Position.X,
                ground + part.Scale.Y * FigureFit.LiftOf(part.Model),
                part.Position.Z);
        }

        static WorldPoint Sized(WorldPart part)
        {
            var fit = FigureFit.ScaleOf(part.Model);

            return new WorldPoint(part.Scale.X * fit, part.Scale.Y * fit, part.Scale.Z * fit);
        }

        static WorldPoint Boxed(WorldPart part)
        {
            return DungeonPack.FitOf(part.Model, part.Scale);
        }

        static WorldPoint Spanning(WorldPart part)
        {
            var fit = part.Scale.X / DungeonPack.WallPanelWidth;

            return new WorldPoint(fit, fit, fit);
        }
    }
}
