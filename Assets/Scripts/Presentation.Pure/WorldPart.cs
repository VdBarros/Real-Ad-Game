using System;

namespace Game.Presentation.Pure
{
    public readonly struct WorldPart : IEquatable<WorldPart>
    {
        public WorldPart(
            string name,
            PartShape shape,
            PartModel model,
            PartStyle style,
            WorldPoint position,
            WorldPoint rotation,
            WorldPoint scale)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Shape = shape;
            Model = model;
            Style = style;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public string Name { get; }

        public PartShape Shape { get; }

        public PartModel Model { get; }

        public PartStyle Style { get; }

        public WorldPoint Position { get; }

        public WorldPoint Rotation { get; }

        public WorldPoint Scale { get; }

        public bool Equals(WorldPart other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && Shape == other.Shape
                && Model == other.Model
                && Style == other.Style
                && Position.Equals(other.Position)
                && Rotation.Equals(other.Rotation)
                && Scale.Equals(other.Scale);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPart other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Name == null ? 0 : Name.GetHashCode();
                hash = (hash * 397) ^ (int)Shape;
                hash = (hash * 397) ^ (int)Model;
                hash = (hash * 397) ^ (int)Style;
                hash = (hash * 397) ^ Position.GetHashCode();
                hash = (hash * 397) ^ Rotation.GetHashCode();
                hash = (hash * 397) ^ Scale.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Name, " ", Model.ToString(), " ", Shape.ToString(), " ", Style.ToString(), " at ", Position.ToString());
        }
    }
}
