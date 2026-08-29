using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PillarStage
    {
        public const float Throw = 8f;

        public const float Drain = 9f;

        public const float Count = 10f;

        public const float Crown = 10.5f;

        public const float Cross = 11f;

        public const float Fall = 17f;

        public const float Total = 18f;

        public const float LadderStep = 0.25f;

        public const float RiseSeconds = Cross - Crown;

        public const float WalkSeconds = 4f;

        public const float PortalSeconds = 0.4f;

        public const float MetresPerPoint = 0.1f;

        public const float Spacing = 4f;

        public const float PlayerOffset = -Spacing;

        public const float GirlOffset = 0f;

        public const float RivalOffset = Spacing;

        public const float Elbow = 1.2f;

        public const float MeetOffset = RivalOffset - Elbow;

        public const float FallDepth = 4f;

        public const float EyeLift = 1f;

        public const float NearSize = 3f;

        public const float WideSize = 10f;

        public const float StageMiddle = 5f;

        public const int RivalNumber = 99;

        static readonly int[] playerLadder = { 5, 4, 2 };

        static readonly int[] girlLadder = { 25, 34, 46, 50 };

        public static IReadOnlyList<int> PlayerLadder
        {
            get { return playerLadder; }
        }

        public static IReadOnlyList<int> GirlLadder
        {
            get { return girlLadder; }
        }

        public static BadgePlan Plan
        {
            get { return new BadgePlan(BadgeText.Digits(RivalNumber)); }
        }

        public static float HeightOf(int number)
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number), number, "A pillar stands as tall as a number, and numbers here are never negative.");
            }

            return number * MetresPerPoint;
        }

        public static int PlayerNumberAt(float seconds)
        {
            return Rung(playerLadder, Drain, seconds);
        }

        public static int GirlNumberAt(float seconds)
        {
            return Rung(girlLadder, Count, seconds);
        }

        public static float GirlHeightAt(float seconds)
        {
            var seated = HeightOf(GirlNumberAt(seconds));
            if (seconds < Crown)
            {
                return seated;
            }

            var risen = HeightOf(RivalNumber);
            return seated + (risen - seated) * Ease(Amount(seconds, Crown, RiseSeconds));
        }

        public static float GirlOffsetAt(float seconds)
        {
            if (seconds < Cross)
            {
                return GirlOffset;
            }

            return GirlOffset + (MeetOffset - GirlOffset) * Ease(Amount(seconds, Cross, WalkSeconds));
        }

        public static float PortalOpenAt(float seconds)
        {
            if (seconds < Fall)
            {
                return 0f;
            }

            return Amount(seconds, Fall, PortalSeconds);
        }

        public static float PlayerFallAt(float seconds)
        {
            var opened = Fall + PortalSeconds;
            if (seconds < opened)
            {
                return 0f;
            }

            return Amount(seconds, opened, Total - opened);
        }

        public static WorldPoint Stand(float offset, float height)
        {
            var right = IsoProjection.CameraRight;
            return new WorldPoint(right.X * offset, height, right.Z * offset);
        }

        public static CameraFraming Near
        {
            get { return new CameraFraming(Stand(PlayerOffset, HeightOf(playerLadder[0]) + EyeLift), NearSize); }
        }

        public static CameraFraming Wide
        {
            get { return new CameraFraming(new WorldPoint(0f, StageMiddle, 0f), WideSize); }
        }

        public static CameraFraming FramingAt(float seconds)
        {
            return CameraFraming.Between(Near, Wide, Ease(Amount(seconds, 0f, Throw)));
        }

        public static float Ease(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            if (amount >= 1f)
            {
                return 1f;
            }

            return amount * amount * (3f - 2f * amount);
        }

        static float Amount(float seconds, float start, float span)
        {
            var amount = (seconds - start) / span;
            if (amount <= 0f)
            {
                return 0f;
            }

            return amount >= 1f ? 1f : amount;
        }

        static int Rung(int[] ladder, float start, float seconds)
        {
            if (seconds < start)
            {
                return ladder[0];
            }

            var rung = (int)((seconds - start) / LadderStep) + 1;
            return rung >= ladder.Length ? ladder[ladder.Length - 1] : ladder[rung];
        }
    }
}
