using System;

namespace Game.Presentation.Pure
{
    public readonly struct TapHold : IEquatable<TapHold>
    {
        readonly bool held;
        readonly bool owns;
        readonly TapGesture gesture;

        TapHold(bool held, bool owns, TapGesture gesture)
        {
            this.held = held;
            this.owns = owns;
            this.gesture = gesture;
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

        public TapHold Reading(bool pressedNow, bool releasedNow, bool isPressed, bool hovers, bool locked)
        {
            var holding = held || pressedNow;
            var mine = owns || (pressedNow && !locked);

            if (releasedNow)
            {
                return new TapHold(
                    false, false, mine || (holding && locked) ? TapGesture.Release : TapGesture.Ignore);
            }

            if (isPressed)
            {
                return new TapHold(holding, mine, mine ? TapGesture.Aim : TapGesture.Ignore);
            }

            return new TapHold(false, false, hovers ? TapGesture.Aim : TapGesture.Ignore);
        }

        public bool Equals(TapHold other)
        {
            return held == other.held && owns == other.owns && gesture == other.gesture;
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
                hash = (hash * 397) ^ (int)gesture;
                return hash;
            }
        }

        public override string ToString()
        {
            if (owns)
            {
                return gesture + " on a press of its own";
            }

            return held ? gesture + " on a press it may not aim" : gesture.ToString();
        }
    }
}
