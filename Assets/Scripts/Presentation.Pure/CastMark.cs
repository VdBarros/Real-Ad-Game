using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct CastMark : IEquatable<CastMark>
    {
        public CastMark(
            int number,
            CastLook look,
            BadgeStyle badge,
            float pillarHeight,
            WorldPoint pillarBase,
            WorldPoint position)
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number), number, "Nobody on the pillars carries a negative number.");
            }

            if (pillarHeight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pillarHeight), pillarHeight, "A pillar rises out of the ground, never into it.");
            }

            Number = number;
            Look = look;
            Badge = badge;
            PillarHeight = pillarHeight;
            PillarBase = pillarBase;
            Position = position;
        }

        public int Number { get; }

        public CastLook Look { get; }

        public BadgeStyle Badge { get; }

        public float PillarHeight { get; }

        public WorldPoint PillarBase { get; }

        public WorldPoint Position { get; }

        public Tint Tint
        {
            get { return CastLooks.TintOf(Look); }
        }

        public float Scale
        {
            get { return CastLooks.ScaleOf(Look); }
        }

        public int Cells
        {
            get { return BadgeText.Cells(Badge, Number); }
        }

        public bool Equals(CastMark other)
        {
            return Number == other.Number
                && Look == other.Look
                && Badge == other.Badge
                && PillarHeight.Equals(other.PillarHeight)
                && PillarBase.Equals(other.PillarBase)
                && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            return obj is CastMark other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Number;
                hash = (hash * 397) ^ (int)Look;
                hash = (hash * 397) ^ (int)Badge;
                hash = (hash * 397) ^ PillarHeight.GetHashCode();
                hash = (hash * 397) ^ PillarBase.GetHashCode();
                hash = (hash * 397) ^ Position.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Look.ToString(),
                " at ",
                Number.ToString(CultureInfo.InvariantCulture),
                " on ",
                PillarHeight.ToString("0.###", CultureInfo.InvariantCulture),
                " standing ",
                Position.ToString());
        }
    }
}
