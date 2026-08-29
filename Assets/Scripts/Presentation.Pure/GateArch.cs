using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class GateArch
    {
        public const PartModel Masonry = PartModel.Pillar;

        public const PartModel Pipwork = PartModel.Candle;

        public const float Headroom = 1.05f;

        public const float Depth = 0.14f;

        public const float PostThickness = 0.1f;

        public const float LintelThickness = 0.14f;

        public const float PipSize = 0.15f;

        public const float PipHeight = 0.26f;

        public const float PipGap = 0.05f;

        public const float Yaw = 45f;

        public const int SmallestFactor = 2;

        public const int MostPips = 5;

        public static PartModel Passer
        {
            get { return CharacterCast.MeshOf(PartStyle.Start); }
        }

        public static float PasserScale
        {
            get
            {
                var thresholds = PlayerTier.Thresholds;

                return PlayerLook.Of(thresholds[thresholds.Count - 1]).Scale;
            }
        }

        public static float TallestPasser
        {
            get { return FigureFit.StandingHeight(Passer, PasserScale); }
        }

        public static float WidestPasser
        {
            get { return FigureFit.SpreadOf(Passer, PasserScale); }
        }

        public static float PostHeight
        {
            get { return TallestPasser * Headroom; }
        }

        public static float Walkway
        {
            get { return WidestPasser * Headroom; }
        }

        public static float Span
        {
            get { return Walkway + 2f * PostThickness; }
        }

        public static float Height
        {
            get { return PostHeight + LintelThickness + PipHeight; }
        }

        public static float TileFootprint
        {
            get
            {
                var turn = Yaw * Math.PI / 180.0;
                var across = Math.Abs(Math.Cos(turn));
                var along = Math.Abs(Math.Sin(turn));

                return (float)Math.Max(Span * across + Depth * along, Span * along + Depth * across);
            }
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
                Cut(
                    PartNames.GateLeftPost,
                    Masonry,
                    new WorldPoint(-post, floor + PostHeight * 0.5f, 0f),
                    new WorldPoint(PostThickness, PostHeight, Depth)),
                Cut(
                    PartNames.GateRightPost,
                    Masonry,
                    new WorldPoint(post, floor + PostHeight * 0.5f, 0f),
                    new WorldPoint(PostThickness, PostHeight, Depth)),
                Cut(
                    PartNames.GateLintel,
                    Masonry,
                    new WorldPoint(0f, floor + PostHeight + LintelThickness * 0.5f, 0f),
                    new WorldPoint(Span, LintelThickness, Depth))
            };

            var row = PipRowFor(factor);
            var lift = floor + PostHeight + LintelThickness + PipHeight * 0.5f;

            for (var pip = 0; pip < factor; pip++)
            {
                pieces.Add(Cut(
                    PartNames.GatePip(pip),
                    Pipwork,
                    new WorldPoint((PipSize - row) * 0.5f + pip * (PipSize + PipGap), lift, 0f),
                    new WorldPoint(PipSize, PipHeight, Depth)));
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

        static WorldPart Cut(string name, PartModel model, WorldPoint position, WorldPoint scale)
        {
            return new WorldPart(
                name,
                PartShape.Gate,
                model,
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
