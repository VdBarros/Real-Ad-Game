using System;

namespace Game.Presentation.Pure
{
    public static class PartModels
    {
        public static PartModel Of(PartStyle style)
        {
            switch (style)
            {
                case PartStyle.Floor:
                case PartStyle.Cleared:
                    return PartModel.FloorTile;
                case PartStyle.Wall:
                    return PartModel.WallPanel;
                case PartStyle.Additive:
                    return PartModel.Chest;
                case PartStyle.Staircase:
                    return PartModel.Staircase;
                case PartStyle.Foundation:
                    return PartModel.Foundation;
                case PartStyle.Start:
                case PartStyle.Enemy:
                case PartStyle.Boss:
                    return CharacterCast.MeshOf(style);
                case PartStyle.Multiplier:
                case PartStyle.Pillar:
                case PartStyle.Trail:
                case PartStyle.Spark:
                    return PartModel.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No model for that part style.");
            }
        }

        public static PartModel Of(PartStyle style, int power)
        {
            return CharacterCast.IsRole(style) ? CharacterCast.MeshOf(style, power) : Of(style);
        }
    }
}
