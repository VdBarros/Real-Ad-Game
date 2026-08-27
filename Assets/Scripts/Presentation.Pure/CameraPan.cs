using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraPan : IEquatable<CameraPan>
    {
        public enum Stance
        {
            OnTheSubject,
            UnderTheFinger,
            Held,
            OnItsWayBack
        }

        readonly CameraBounds bounds;
        readonly WorldPoint anchor;
        readonly WorldPoint look;
        readonly Stance stance;

        CameraPan(CameraBounds bounds, WorldPoint anchor, WorldPoint look, Stance stance)
        {
            this.bounds = bounds;
            this.anchor = anchor;
            this.look = look;
            this.stance = stance;
        }

        public static CameraPan Around(LevelGraph graph)
        {
            return new CameraPan(
                CameraBounds.Around(graph), default(WorldPoint), default(WorldPoint), Stance.OnTheSubject);
        }

        public Stance Standing
        {
            get { return stance; }
        }

        public bool IsResting
        {
            get { return stance == Stance.OnTheSubject; }
        }

        public bool IsAway
        {
            get { return stance != Stance.OnTheSubject; }
        }

        public bool HoldsTheCamera
        {
            get { return stance == Stance.UnderTheFinger || stance == Stance.Held; }
        }

        public WorldPoint Look
        {
            get
            {
                if (!HoldsTheCamera)
                {
                    throw new InvalidOperationException(
                        "Only a pan the finger put somewhere says where to look. This one is " + this + ".");
                }

                return look;
            }
        }

        public CameraPan Dragged(WorldPoint from, WorldPoint offset)
        {
            var origin = stance == Stance.UnderTheFinger ? anchor : from;
            var wanted = bounds.Clamp(
                new WorldPoint(origin.X + offset.X, origin.Y + offset.Y, origin.Z + offset.Z));

            if (stance == Stance.UnderTheFinger && wanted.Equals(look) && origin.Equals(anchor))
            {
                return this;
            }

            return new CameraPan(bounds, origin, wanted, Stance.UnderTheFinger);
        }

        public CameraPan LetGo()
        {
            return stance == Stance.UnderTheFinger
                ? new CameraPan(bounds, anchor, look, Stance.Held)
                : this;
        }

        public CameraPan Recalled()
        {
            return HoldsTheCamera ? new CameraPan(bounds, anchor, look, Stance.OnItsWayBack) : this;
        }

        public CameraPan Arrived()
        {
            return stance == Stance.OnItsWayBack ? Resting() : this;
        }

        public CameraPan Dropped()
        {
            return stance == Stance.OnTheSubject ? this : Resting();
        }

        public bool Equals(CameraPan other)
        {
            return bounds.Equals(other.bounds)
                && anchor.Equals(other.anchor)
                && look.Equals(other.look)
                && stance == other.stance;
        }

        public override bool Equals(object obj)
        {
            return obj is CameraPan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = bounds.GetHashCode();
                hash = (hash * 397) ^ anchor.GetHashCode();
                hash = (hash * 397) ^ look.GetHashCode();
                hash = (hash * 397) ^ (int)stance;
                return hash;
            }
        }

        public override string ToString()
        {
            switch (stance)
            {
                case Stance.UnderTheFinger:
                    return "dragged from " + anchor + " to " + look;
                case Stance.Held:
                    return "held at " + look;
                case Stance.OnItsWayBack:
                    return "on its way back from " + look;
                default:
                    return "on the player";
            }
        }

        CameraPan Resting()
        {
            return new CameraPan(bounds, default(WorldPoint), default(WorldPoint), Stance.OnTheSubject);
        }
    }
}
