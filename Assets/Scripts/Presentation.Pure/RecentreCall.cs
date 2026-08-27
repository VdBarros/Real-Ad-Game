using System;

namespace Game.Presentation.Pure
{
    public static class RecentreCall
    {
        public const float Width = 420f;

        public const float Height = 132f;

        public const float Lift = 120f;

        public static bool Showing(GamePhase phase, bool cameraIsAway, bool cameraIsBusy)
        {
            return phase == GamePhase.Play && cameraIsAway && !cameraIsBusy;
        }

        public static bool Holds(int frameWidth, int frameHeight, ScreenPoint finger)
        {
            if (frameWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameWidth), frameWidth, "A frame with no pixels across has nowhere to put a button.");
            }

            if (frameHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameHeight), frameHeight, "A frame with no pixels up has nowhere to put a button.");
            }

            var scale = frameHeight / (float)ScreenFrame.Height;
            var bottom = Lift * scale;

            return Math.Abs(finger.X - frameWidth * 0.5f) <= Width * 0.5f * scale
                && finger.Y >= bottom
                && finger.Y <= bottom + Height * scale;
        }
    }
}
