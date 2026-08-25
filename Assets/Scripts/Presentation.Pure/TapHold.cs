using System;

namespace Game.Presentation.Pure
{
    public readonly struct TapHold : IEquatable<TapHold>
    {
        readonly bool owns;
        readonly TapGesture gesture;

        TapHold(bool owns, TapGesture gesture)
        {
            this.owns = owns;
            this.gesture = gesture;
        }

        public static TapHold Idle
        {
            get { return default(TapHold); }
        }

        public bool OwnsThePress
        {
            get { return owns; }
        }

        public TapGesture Gesture
        {
            get { return gesture; }
        }

        public TapHold Reading(bool pressedNow, bool releasedNow, bool isPressed, bool hovers)
        {
            var mine = owns || pressedNow;

            if (releasedNow)
            {
                return new TapHold(false, mine ? TapGesture.Release : TapGesture.Ignore);
            }

            if (isPressed)
            {
                return new TapHold(mine, mine ? TapGesture.Aim : TapGesture.Ignore);
            }

            return new TapHold(false, hovers ? TapGesture.Aim : TapGesture.Ignore);
        }

        public bool Equals(TapHold other)
        {
            return owns == other.owns && gesture == other.gesture;
        }

        public override bool Equals(object obj)
        {
            return obj is TapHold other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (owns.GetHashCode() * 397) ^ (int)gesture;
            }
        }

        public override string ToString()
        {
            return owns ? gesture + " on a press of its own" : gesture.ToString();
        }
    }
}
