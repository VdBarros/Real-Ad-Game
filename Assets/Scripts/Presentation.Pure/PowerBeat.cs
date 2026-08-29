using System;

namespace Game.Presentation.Pure
{
    public readonly struct PowerBeat : IEquatable<PowerBeat>
    {
        readonly CountUp countUp;
        readonly Promotion promotion;

        PowerBeat(CountUp countUp, Promotion promotion)
        {
            this.countUp = countUp;
            this.promotion = promotion;
        }

        public static PowerBeat Begin(int power)
        {
            return new PowerBeat(CountUp.Settled(power), Promotion.Settled(PlayerLook.Of(power)));
        }

        public int Power
        {
            get { return countUp.Target; }
        }

        public int Shown
        {
            get { return countUp.Display; }
        }

        public bool HasLanded
        {
            get { return countUp.IsSettled; }
        }

        public bool IsSettled
        {
            get { return countUp.IsSettled && promotion.IsSettled; }
        }

        public PlayerLook Look
        {
            get { return promotion.Target; }
        }

        public float Scale
        {
            get { return promotion.Scale; }
        }

        public PowerBeat Toward(int power)
        {
            var retargeted = countUp.Toward(power);
            if (retargeted.Equals(countUp))
            {
                return this;
            }

            return new PowerBeat(retargeted, promotion);
        }

        public PowerBeat Advanced(float deltaSeconds)
        {
            if (IsSettled)
            {
                return this;
            }

            var spill = deltaSeconds - countUp.Remaining;
            if (spill < 0f)
            {
                spill = 0f;
            }

            var counted = countUp.Advanced(deltaSeconds);
            var grown = promotion.Advanced(deltaSeconds - spill);

            if (counted.IsSettled)
            {
                grown = grown.Toward(PlayerLook.Of(counted.Target)).Advanced(spill);
            }

            return new PowerBeat(counted, grown);
        }

        public bool Equals(PowerBeat other)
        {
            return countUp.Equals(other.countUp) && promotion.Equals(other.promotion);
        }

        public override bool Equals(object obj)
        {
            return obj is PowerBeat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (countUp.GetHashCode() * 397) ^ promotion.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(countUp.ToString(), ", ", promotion.ToString());
        }
    }
}
