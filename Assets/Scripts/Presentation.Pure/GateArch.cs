using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class GateArch
    {
        public const float Span = 0.98f;

        public const float Depth = 0.14f;

        public const float PostThickness = 0.14f;

        public const float PostHeight = 1f;

        public const float LintelThickness = 0.14f;

        public const float PipSize = 0.15f;

        public const float PipHeight = 0.26f;

        public const float PipGap = 0.05f;

        public const float Height = PostHeight + LintelThickness + PipHeight;

        public const float Yaw = 45f;

        public const int SmallestFactor = 2;

        public const int MostPips = 5;

        public static float Walkway
        {
            get { return Span - 2f * PostThickness; }
        }

        public static float PipRowFor(int factor)
        {
            RequireAFactor(factor);
            return factor * PipSize + (factor - 1) * PipGap;
        }

        public static IReadOnlyList<WorldPart> Pieces(int factor)
        {
            RequireAFactor(factor);

            var floor = -Height * 0.5f;
            var post = (Span - PostThickness) * 0.5f;
            var pieces = new List<WorldPart>(factor + 3)
            {
                Block(
                    PartNames.GateLeftPost,
                    new WorldPoint(-post, floor + PostHeight * 0.5f, 0f),
                    new WorldPoint(PostThickness, PostHeight, Depth)),
                Block(
                    PartNames.GateRightPost,
                    new WorldPoint(post, floor + PostHeight * 0.5f, 0f),
                    new WorldPoint(PostThickness, PostHeight, Depth)),
                Block(
                    PartNames.GateLintel,
                    new WorldPoint(0f, floor + PostHeight + LintelThickness * 0.5f, 0f),
                    new WorldPoint(Span, LintelThickness, Depth))
            };

            var row = PipRowFor(factor);
            var lift = floor + PostHeight + LintelThickness + PipHeight * 0.5f;

            for (var pip = 0; pip < factor; pip++)
            {
                pieces.Add(Block(
                    PartNames.GatePip(pip),
                    new WorldPoint((PipSize - row) * 0.5f + pip * (PipSize + PipGap), lift, 0f),
                    new WorldPoint(PipSize, PipHeight, PipSize)));
            }

            return pieces;
        }

        public static int PipsOn(IReadOnlyList<WorldPart> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            var pips = 0;
            for (var slot = 0; slot < pieces.Count; slot++)
            {
                if (PartNames.IsGatePip(pieces[slot].Name))
                {
                    pips++;
                }
            }

            return pips;
        }

        static WorldPart Block(string name, WorldPoint position, WorldPoint scale)
        {
            return new WorldPart(
                name,
                PartShape.Cube,
                PartModel.None,
                PartStyle.Multiplier,
                position,
                new WorldPoint(0f, 0f, 0f),
                scale);
        }

        static void RequireAFactor(int factor)
        {
            if (factor < SmallestFactor || factor > MostPips)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(factor),
                    factor,
                    "A gate counts its factor in pips along its lintel, from "
                    + SmallestFactor + " to " + MostPips + ".");
            }
        }
    }
}
