using System;

namespace Game.Presentation.Pure
{
    public readonly struct BadgePart : IEquatable<BadgePart>
    {
        public BadgePart(
            string name,
            int nodeId,
            int elevation,
            BadgeStyle style,
            int value,
            int cells,
            float subjectWidth,
            WorldPoint position,
            WorldPoint rotation)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            NodeId = nodeId;
            Elevation = elevation;
            Style = style;
            Value = value;
            Cells = cells;
            SubjectWidth = subjectWidth;
            Position = position;
            Rotation = rotation;
        }

        public string Name { get; }

        public int NodeId { get; }

        public int Elevation { get; }

        public BadgeStyle Style { get; }

        public int Value { get; }

        public int Cells { get; }

        public float SubjectWidth { get; }

        public BadgeSize Size
        {
            get { return BadgeFit.Of(Cells, SubjectWidth); }
        }

        public float Width
        {
            get { return Size.Width; }
        }

        public string Text
        {
            get { return BadgeText.Of(Style, Value); }
        }

        public WorldPoint Position { get; }

        public WorldPoint Rotation { get; }

        public BadgeShape Shape
        {
            get { return BadgeStyles.ShapeOf(Style); }
        }

        public bool Equals(BadgePart other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && NodeId == other.NodeId
                && Elevation == other.Elevation
                && Style == other.Style
                && Value == other.Value
                && Cells == other.Cells
                && SubjectWidth.Equals(other.SubjectWidth)
                && Position.Equals(other.Position)
                && Rotation.Equals(other.Rotation);
        }

        public override bool Equals(object obj)
        {
            return obj is BadgePart other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Name == null ? 0 : Name.GetHashCode();
                hash = (hash * 397) ^ NodeId;
                hash = (hash * 397) ^ Elevation;
                hash = (hash * 397) ^ (int)Style;
                hash = (hash * 397) ^ Value.GetHashCode();
                hash = (hash * 397) ^ Cells;
                hash = (hash * 397) ^ SubjectWidth.GetHashCode();
                hash = (hash * 397) ^ Position.GetHashCode();
                hash = (hash * 397) ^ Rotation.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(Name, " ", Style.ToString(), " \"", Text, "\" at ", Position.ToString());
        }
    }
}
