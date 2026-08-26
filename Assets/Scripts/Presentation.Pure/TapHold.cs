using System;

namespace Game.Presentation.Pure
{
    public readonly struct TapHold : IEquatable<TapHold>
    {
        readonly bool held;
        readonly bool owns;
        readonly bool strayed;
        readonly TapGesture gesture;
        readonly ScreenPoint origin;

        TapHold(bool held, bool owns, bool strayed, TapGesture gesture, ScreenPoint origin)
        {
            this.held = held;
            this.owns = owns;
            this.strayed = strayed;
            this.gesture = gesture;
            this.origin = origin;
        }

        public static TapHold Idle
        {
            get { return default(TapHold); }
        }

        public bool HoldsAPress
        {
            get { return held; }
        }

        public bool OwnsThePress
        {
            get { return owns; }
        }

        public TapGesture Gesture
        {
            get { return gesture; }
        }

        public ScreenPoint Origin
        {
            get { return origin; }
        }

        public TapHold Reading(
            bool pressedNow,
            bool releasedNow,
            bool isPressed,
            bool hovers,
            bool locked,
            ScreenPoint finger,
            float reach)
        {
            var holding = held || pressedNow;
            var mine = owns || (pressedNow && !locked);
            var from = pressedNow ? finger : origin;
            var wandered = strayed || (holding && ScreenPoint.Distance(from, finger) > reach);

            if (releasedNow)
            {
                if (mine && wandered)
                {
                    return new TapHold(false, false, false, TapGesture.Pan, finger);
                }

                return new TapHold(
                    false,
                    false,
                    false,
                    mine || (holding && locked) ? TapGesture.Release : TapGesture.Ignore,
                    finger);
            }

            if (isPressed)
            {
                if (!mine)
                {
                    return new TapHold(holding, false, wandered, TapGesture.Ignore, from);
                }

                return new TapHold(holding, true, wandered, wandered ? TapGesture.Pan : TapGesture.Aim, from);
            }

            return new TapHold(false, false, false, hovers ? TapGesture.Aim : TapGesture.Ignore, finger);
        }

        public bool Equals(TapHold other)
        {
            return held == other.held
                && owns == other.owns
                && strayed == other.strayed
                && gesture == other.gesture
                && origin.Equals(other.origin);
        }

        public override bool Equals(object obj)
        {
            return obj is TapHold other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = held.GetHashCode();
                hash = (hash * 397) ^ owns.GetHashCode();
                hash = (hash * 397) ^ strayed.GetHashCode();
                hash = (hash * 397) ^ (int)gesture;
                hash = (hash * 397) ^ origin.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (owns)
            {
                return gesture + " on a press of its own from " + origin;
            }

            return held ? gesture + " on a press it may not aim" : gesture.ToString();
        }
    }
}
