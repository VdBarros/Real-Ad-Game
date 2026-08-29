using System;

namespace Game.Presentation.Pure
{
    public static class Backdrop
    {
        public const float LeastSurfaceSeparation = 2f;

        public const float LeastFigureSeparation = 1.7f;

        public const float LeastAmbientTilt = 1.25f;

        public const float SharedAmbientHue = 12f;

        public const int RampBands = 64;

        public const float Reach = IsoProjection.FarPlane - 1f;

        public const float WidthOverHeight = 3f;

        public const float Overscan = 1.02f;

        public const float ReflectionStrength = 0f;

        public static Tint Above
        {
            get { return new Tint(0.35f, 0.41f, 0.51f); }
        }

        public static Tint Below
        {
            get { return new Tint(0.26f, 0.32f, 0.42f); }
        }

        public static Tint Clear
        {
            get { return At(0.5f); }
        }

        public static Tint AmbientSky
        {
            get { return new Tint(0.38f, 0.41f, 0.47f); }
        }

        public static Tint AmbientEquator
        {
            get { return new Tint(0.33f, 0.33f, 0.34f); }
        }

        public static Tint AmbientGround
        {
            get { return new Tint(0.22f, 0.2f, 0.18f); }
        }

        public static Tint AmbientBudget
        {
            get { return new Tint(0.34f, 0.34f, 0.34f); }
        }

        public static float AmbientLoad
        {
            get { return (AmbientSky.Luminance + AmbientEquator.Luminance + AmbientGround.Luminance) / 3f; }
        }

        public static Tint At(float height)
        {
            return Tint.Lerp(Below, Above, Held(height));
        }

        public static float BandHeight(int band)
        {
            return RampBands < 2 ? 0.5f : Held(band / (float)(RampBands - 1));
        }

        public static float SeparationFrom(Tint tint)
        {
            var high = Tint.Contrast(tint, Above);
            var low = Tint.Contrast(tint, Below);

            return high < low ? high : low;
        }

        public static float LeastSeparationFor(PartLayer layer)
        {
            switch (layer)
            {
                case PartLayer.Surface:
                    return LeastSurfaceSeparation;
                case PartLayer.Figure:
                    return LeastFigureSeparation;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layer),
                        layer,
                        "Marks are drawn on the floor they mark, never against the backdrop.");
            }
        }

        static float Held(float height)
        {
            if (height < 0f)
            {
                return 0f;
            }

            return height > 1f ? 1f : height;
        }
    }
}
