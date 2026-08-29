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
            LandmarkKind.Tree,
            LandmarkKind.Statue,
            LandmarkKind.Fountain,
            LandmarkKind.Crystal,
            LandmarkKind.Obelisk
        };

        public static IReadOnlyList<LandmarkPiece> Pieces(LandmarkKind kind)
        {
            switch (kind)
            {
                case LandmarkKind.Tree:
                    return Tree();
                case LandmarkKind.Statue:
                    return Statue();
                case LandmarkKind.Fountain:
                    return Fountain();
                case LandmarkKind.Crystal:
                    return Crystal();
                case LandmarkKind.Obelisk:
                    return Obelisk();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind), kind, "No landmark is drawn for that kind.");
            }
        }

        public static float StandingHeightOf(LandmarkKind kind)
        {
            var floor = -Height * 0.5f;
            var top = floor;

            foreach (var piece in Pieces(kind))
            {
                var reach = piece.Part.Position.Y + HalfHeightOf(piece.Part);
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

            foreach (var piece in Pieces(kind))
            {
                var plan = PlanReachOf(piece.Part);
                if (plan > widest)
                {
                    widest = plan;
                }
            }

            return widest;
        }

        public static float HalfHeightOf(WorldPart part)
        {
            switch (part.Shape)
            {
                case PartShape.Cube:
                case PartShape.Sphere:
                    return part.Scale.Y * 0.5f;
                case PartShape.Capsule:
                case PartShape.Cylinder:
                    return part.Scale.Y;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Shape, "No landmark piece is cut in that shape.");
            }
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

            return (float)Math.Max(
                Math.Abs(part.Position.X) + sideways, Math.Abs(part.Position.Z) + forwards);
        }

        static IReadOnlyList<LandmarkPiece> Tree()
        {
            var floor = -Height * 0.5f;
            var bark = LandmarkLook.FootingOf(LandmarkKind.Tree);
            var leaf = LandmarkLook.Of(LandmarkKind.Tree);

            return new[]
            {
                Piece(0, PartShape.Cylinder, new WorldPoint(0f, floor + 0.36f, 0f), new WorldPoint(0.12f, 0.36f, 0.12f), bark),
                Piece(1, PartShape.Sphere, new WorldPoint(0f, floor + 0.88f, 0f), new WorldPoint(0.36f, 0.36f, 0.36f), leaf),
                Piece(2, PartShape.Sphere, new WorldPoint(0f, floor + 1.14f, 0f), new WorldPoint(0.24f, 0.24f, 0.24f), leaf)
            };
        }

        static IReadOnlyList<LandmarkPiece> Statue()
        {
            var floor = -Height * 0.5f;
            var stone = LandmarkLook.FootingOf(LandmarkKind.Statue);
            var marble = LandmarkLook.Of(LandmarkKind.Statue);

            return new[]
            {
                Piece(0, PartShape.Cube, new WorldPoint(0f, floor + 0.22f, 0f), new WorldPoint(0.36f, 0.44f, 0.36f), stone),
                Piece(1, PartShape.Capsule, new WorldPoint(0f, floor + 0.70f, 0f), new WorldPoint(0.22f, 0.26f, 0.22f), marble),
                Piece(2, PartShape.Sphere, new WorldPoint(0f, floor + 1.07f, 0f), new WorldPoint(0.22f, 0.22f, 0.22f), marble)
            };
        }

        static IReadOnlyList<LandmarkPiece> Fountain()
        {
            var floor = -Height * 0.5f;
            var stone = LandmarkLook.FootingOf(LandmarkKind.Fountain);
            var water = LandmarkLook.Of(LandmarkKind.Fountain);

            return new[]
            {
                Piece(0, PartShape.Cylinder, new WorldPoint(0f, floor + 0.11f, 0f), new WorldPoint(0.36f, 0.11f, 0.36f), stone),
                Piece(1, PartShape.Cylinder, new WorldPoint(0f, floor + 0.42f, 0f), new WorldPoint(0.14f, 0.20f, 0.14f), stone),
                Piece(2, PartShape.Cylinder, new WorldPoint(0f, floor + 0.67f, 0f), new WorldPoint(0.28f, 0.05f, 0.28f), stone),
                Piece(3, PartShape.Cylinder, new WorldPoint(0f, floor + 0.97f, 0f), new WorldPoint(0.09f, 0.25f, 0.09f), water),
                Piece(4, PartShape.Sphere, new WorldPoint(0f, floor + 1.32f, 0f), new WorldPoint(0.22f, 0.22f, 0.22f), water)
            };
        }

        static IReadOnlyList<LandmarkPiece> Crystal()
        {
            var floor = -Height * 0.5f;
            var rock = LandmarkLook.FootingOf(LandmarkKind.Crystal);
            var glass = LandmarkLook.Of(LandmarkKind.Crystal);

            return new[]
            {
                Turned(0, new WorldPoint(0f, floor + 0.07f, 0f), new WorldPoint(0.24f, 0.14f, 0.24f), rock),
                Turned(1, new WorldPoint(0f, floor + 0.66f, 0f), new WorldPoint(0.14f, 1.10f, 0.14f), glass),
                Turned(2, new WorldPoint(0f, floor + 1.32f, 0f), new WorldPoint(0.09f, 0.24f, 0.09f), glass),
                Turned(3, new WorldPoint(-0.115f, floor + 0.33f, -0.02f), new WorldPoint(0.09f, 0.66f, 0.09f), glass),
                Turned(4, new WorldPoint(0.115f, floor + 0.23f, 0.03f), new WorldPoint(0.08f, 0.46f, 0.08f), glass)
            };
        }

        static IReadOnlyList<LandmarkPiece> Obelisk()
        {
            var floor = -Height * 0.5f;
            var slate = LandmarkLook.FootingOf(LandmarkKind.Obelisk);
            var gold = LandmarkLook.Of(LandmarkKind.Obelisk);

            return new[]
            {
                Piece(0, PartShape.Cube, new WorldPoint(0f, floor + 0.07f, 0f), new WorldPoint(0.36f, 0.14f, 0.36f), slate),
                Piece(1, PartShape.Cube, new WorldPoint(0f, floor + 0.72f, 0f), new WorldPoint(0.22f, 1.16f, 0.22f), slate),
                Piece(2, PartShape.Cube, new WorldPoint(0f, floor + 1.37f, 0f), new WorldPoint(0.14f, 0.14f, 0.14f), gold),
                Turned(3, new WorldPoint(0f, floor + 1.50f, 0f), new WorldPoint(0.10f, 0.12f, 0.10f), gold)
            };
        }

        static LandmarkPiece Piece(int index, PartShape shape, WorldPoint position, WorldPoint scale, Tint tint)
        {
            return Cut(index, shape, position, new WorldPoint(0f, 0f, 0f), scale, tint);
        }

        static LandmarkPiece Turned(int index, WorldPoint position, WorldPoint scale, Tint tint)
        {
            return Cut(index, PartShape.Cube, position, new WorldPoint(0f, Yaw, 0f), scale, tint);
        }

        static LandmarkPiece Cut(
            int index, PartShape shape, WorldPoint position, WorldPoint rotation, WorldPoint scale, Tint tint)
        {
            return new LandmarkPiece(
                new WorldPart(
                    PartNames.LandmarkPiece(index),
                    shape,
                    PartModel.None,
                    PartStyle.Landmark,
                    position,
                    rotation,
                    scale),
                tint);
        }
    }
}
