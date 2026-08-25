using System;

namespace Game.Presentation.Pure
{
    public static class TouchTargets
    {
        public const float MinimumMillimetres = 9f;

        public const float MillimetresPerInch = 25.4f;

        public const float ReferenceDiagonalInches = 5.5f;

        static readonly float referenceDotsPerInch = (float)(Math.Sqrt(
            (double)ScreenFrame.Width * ScreenFrame.Width
            + (double)ScreenFrame.Height * ScreenFrame.Height) / ReferenceDiagonalInches);

        public static float ReferenceDotsPerInch
        {
            get { return referenceDotsPerInch; }
        }

        public static float Reach
        {
            get { return ReachOn(referenceDotsPerInch); }
        }

        public static float DotsPerInchOr(float reported)
        {
            return reported > 0f && !float.IsNaN(reported) && !float.IsInfinity(reported)
                ? reported
                : referenceDotsPerInch;
        }

        public static float Pixels(float millimetres, float dotsPerInch)
        {
            RequireDensity(dotsPerInch);
            return millimetres * dotsPerInch / MillimetresPerInch;
        }

        public static float Millimetres(float pixels, float dotsPerInch)
        {
            RequireDensity(dotsPerInch);
            return pixels * MillimetresPerInch / dotsPerInch;
        }

        public static float ReachOn(float dotsPerInch)
        {
            return Pixels(MinimumMillimetres * 0.5f, dotsPerInch);
        }

        static void RequireDensity(float dotsPerInch)
        {
            if (dotsPerInch <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dotsPerInch), dotsPerInch, "A screen always packs a positive number of dots to the inch.");
            }
        }
    }
}
