using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class LandmarkForm
    {
        public const float Span = 0.36f;

        public const float Height = 1.6f;

        public const float Yaw = 45f;

        public static float Reach
        {
            get { return Span * 0.5f; }
        }

        public static IReadOnlyList<LandmarkKind> Kinds
        {
            get { return kinds; }
        }

        static readonly LandmarkKind[] kinds =
        {
            LandmarkKind.Pillar,
            LandmarkKind.Brazier,
            LandmarkKind.Hoard,
            LandmarkKind.Trophy,
            LandmarkKind.Shrine
        };

        public static IReadOnlyList<WorldPart> Pieces(LandmarkKind kind)
        {
            switch (kind)
            {
                case LandmarkKind.Pillar:
                    return Stack(Course(PartModel.Pillar, 0.92f), Course(PartModel.Pillar, 0.46f));
                case LandmarkKind.Brazier:
                    return Stack(
                        Course(PartModel.Foundation, 0.32f),
                        Course(PartModel.Column, 0.30f),
                        Course(PartModel.TorchLit, 0.70f));
                case LandmarkKind.Hoard:
                    return Stack(
                        Course(PartModel.BarrelLarge, 0.38f),
                        Course(PartModel.CratesStacked, 0.30f),
                        Course(PartModel.BarrelLarge, 0.30f),
                        Course(PartModel.CoinStack, 0.24f));
                case LandmarkKind.Trophy:
                    return Stack(
                        Course(PartModel.CratesStacked, 0.32f),
                        Course(PartModel.Pillar, 0.64f),
                        Turned(PartModel.SwordShield, 0.268f));
                case LandmarkKind.Shrine:
                    return Stack(
                        Course(PartModel.Foundation, 0.32f),
                        Course(PartModel.Foundation, 0.28f),
                        Course(PartModel.Foundation, 0.25f),
                        Course(PartModel.Foundation, 0.22f),
                        Course(PartModel.Foundation, 0.19f));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind), kind, "No landmark is drawn for that kind.");
            }
        }

        public static float StandingHeightOf(LandmarkKind kind)
        {
            var floor = -Height * 0.5f;
            var top = floor;

            foreach (var part in Pieces(kind))
            {
                var reach = part.Position.Y + part.Scale.Y * 0.5f;
                if (reach > top)
                {
                    top = reach;
                }
            }

            return top - floor;
        }

        public static float ReachOf(LandmarkKind kind)
        {
            var widest = 0f;

            foreach (var part in Pieces(kind))
            {
                var plan = PlanReachOf(part);
                if (plan > widest)
                {
                    widest = plan;
                }
            }

            return widest;
        }

        public static float PlanReachOf(WorldPart part)
        {
            var halfWide = part.Scale.X * 0.5f;
            var halfDeep = part.Scale.Z * 0.5f;
            var turn = part.Rotation.Y * Math.PI / 180.0;
            var across = Math.Abs(Math.Cos(turn));
            var along = Math.Abs(Math.Sin(turn));

            var sideways = halfWide * across + halfDeep * along;
            var forwards = halfWide * along + halfDeep * across;
            var off = OffCentreOf(part);

            return (float)Math.Max(
                Math.Abs(part.Position.X + off.X) + sideways,
                Math.Abs(part.Position.Z + off.Z) + forwards);
        }

        public static WorldPoint OffCentreOf(WorldPart part)
        {
            if (part.Model == PartModel.None)
            {
                return new WorldPoint(0f, 0f, 0f);
            }

            var fit = part.Scale.Y / DungeonPack.HeightOf(part.Model);
            var wide = DungeonPack.ShiftAcrossOf(part.Model) * fit;
            var deep = DungeonPack.ShiftAlongOf(part.Model) * fit;
            var turn = part.Rotation.Y * Math.PI / 180.0;
            var across = Math.Cos(turn);
            var along = Math.Sin(turn);

            return new WorldPoint(
                (float)(wide * across + deep * along), 0f, (float)(deep * across - wide * along));
        }

        static IReadOnlyList<WorldPart> Stack(params Layer[] courses)
        {
            var floor = -Height * 0.5f;
            var pieces = new List<WorldPart>(courses.Length);
            var laid = 0f;

            for (var index = 0; index < courses.Length; index++)
            {
                var size = DungeonPack.SizeOf(courses[index].Model, courses[index].Tall);

                pieces.Add(new WorldPart(
                    PartNames.LandmarkPiece(index),
                    PartShape.Landmark,
                    courses[index].Model,
                    PartStyle.Landmark,
                    new WorldPoint(0f, floor + laid + size.Y * 0.5f, 0f),
                    new WorldPoint(0f, courses[index].Turn, 0f),
                    size));

                laid += size.Y;
            }

            return pieces;
        }

        static Layer Course(PartModel model, float tall)
        {
            return new Layer(model, tall, 0f);
        }

        static Layer Turned(PartModel model, float tall)
        {
            return new Layer(model, tall, Yaw);
        }

        readonly struct Layer
        {
            public Layer(PartModel model, float tall, float turn)
            {
                Model = model;
                Tall = tall;
                Turn = turn;
            }

            public PartModel Model { get; }

            public float Tall { get; }

            public float Turn { get; }
        }
    }
}
