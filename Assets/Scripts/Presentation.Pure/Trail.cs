using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class Trail
    {
        public const int DotsPerStep = 2;

        public const float Size = 0.12f;

        public const float Lift = 0.04f;

        public const float DangerSize = 0.19f;

        static readonly TrailLook[] looks =
        {
            new TrailLook(new Tint(0.97f, 0.93f, 0.55f), Size),
            new TrailLook(new Tint(1f, 0.27f, 0.14f), DangerSize)
        };

        public static TrailLook Look(TrailMood mood)
        {
            var slot = (int)mood;
            if (slot < 0 || slot >= looks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(mood), mood, "No look for that mood.");
            }

            return looks[slot];
        }

        public static TrailMood MoodOf(TargetPreview preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            return preview.IsDangerous ? TrailMood.Dangerous : TrailMood.Safe;
        }

        public static IReadOnlyList<TrailDot> Along(TileRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            var dots = new List<TrailDot>(route.Steps * DotsPerStep);

            for (var step = 0; step < route.Steps; step++)
            {
                var from = IsoProjection.Of(route.Tiles[step]);
                var to = IsoProjection.Of(route.Tiles[step + 1]);

                for (var dot = 1; dot <= DotsPerStep; dot++)
                {
                    var along = (float)dot / (DotsPerStep + 1);
                    var laid = WorldPoint.Between(from, to, along);

                    dots.Add(new TrailDot(
                        new WorldPoint(laid.X, laid.Y + Lift, laid.Z),
                        step + along));
                }
            }

            return dots;
        }

        public static bool IsSpent(TrailDot dot, float travelled)
        {
            return travelled >= dot.Step;
        }
    }
}
