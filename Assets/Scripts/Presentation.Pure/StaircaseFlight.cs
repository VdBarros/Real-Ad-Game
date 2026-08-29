using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class StaircaseFlight
    {
        static readonly float[] PackCrestSlices =
        {
            5.1f, 4f, 3.5f, 3f, 2.5f, 2f, 1.9f, 1.5f, 1f, 1.1f
        };

        public static IReadOnlyList<float> PackCrestFromItsOriginOnward
        {
            get { return PackCrestSlices; }
        }

        public static float PackCrestAtItsOrigin
        {
            get { return PackCrestSlices[0]; }
        }

        public static float PackCrestAtItsFarEnd
        {
            get { return PackCrestSlices[PackCrestSlices.Length - 1]; }
        }

        public static TileSide LaidAgainst(TileSide ascent)
        {
            return TileSides.Opposite(ascent);
        }

        public static bool RailsItsOwn(TileSide side, TileSide ascent)
        {
            return side != ascent && side != TileSides.Opposite(ascent);
        }

        public static float HandsOverAt(TileGrid tiles, TilePosition position, TileSide side)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            var ground = IsoProjection.Of(position).Y;

            if (TileFootings.Under(tiles, position) != TileFooting.Flight)
            {
                return ground;
            }

            var ascent = TileFootings.AscentOf(tiles, position);

            if (side == ascent)
            {
                return ground;
            }

            return RailsItsOwn(side, ascent)
                ? ground - IsoProjection.StepHeight * 0.5f
                : ground - IsoProjection.StepHeight;
        }

        public static WorldPoint CrestOf(WorldPart part)
        {
            return ModelPose.PositionOf(part);
        }

        public static WorldPoint SinkOf(WorldPart part)
        {
            var forward = TileSides.Toward(TileSides.OfInwardYaw(part.Rotation.Y));
            var crest = CrestOf(part);

            return new WorldPoint(
                crest.X + forward.X * part.Scale.Z,
                crest.Y,
                crest.Z + forward.Z * part.Scale.Z);
        }

        public static float ReachAlong(WorldPoint point, WorldPoint ground, TileSide ascent)
        {
            var along = TileSides.Toward(ascent);

            return (point.X - ground.X) * along.X + (point.Z - ground.Z) * along.Z;
        }
    }
}
