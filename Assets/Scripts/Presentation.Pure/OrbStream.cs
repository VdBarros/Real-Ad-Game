using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct OrbStream : IEquatable<OrbStream>
    {
        public const int Fewest = 3;

        public const int Most = 7;

        public const float Size = 0.17f;

        public const float Spread = 0.55f;

        public const float Arc = 0.70f;

        public const int TrailDots = 3;

        public const float TrailGap = 0.06f;

        public const float TrailTaper = 0.62f;

        public static readonly Tint Glow = new Tint(1f, 0.86f, 0.42f);

        readonly WorldPoint site;
        readonly int gain;
        readonly VictoryTimeline timeline;

        OrbStream(WorldPoint site, int gain, VictoryTimeline timeline)
        {
            this.site = site;
            this.gain = gain;
            this.timeline = timeline;
        }

        public static OrbStream None
        {
            get { return default(OrbStream); }
        }

        public static OrbStream From(WorldPoint deathSite, int gain)
        {
            return gain <= 0 ? None : new OrbStream(deathSite, gain, VictoryTimeline.Begun);
        }

        public static float Stagger
        {
            get { return VictoryStages.BurstSeconds / Most; }
        }

        public bool IsCarried
        {
            get { return gain > 0 && timeline.HasBegun; }
        }

        public int Gain
        {
            get { return IsCarried ? gain : 0; }
        }

        public WorldPoint Site
        {
            get { return site; }
        }

        public float Elapsed
        {
            get { return timeline.Elapsed; }
        }

        public VictoryStage Stage
        {
            get { return timeline.Stage; }
        }

        public int Orbs
        {
            get
            {
                if (!IsCarried)
                {
                    return 0;
                }

                return gain < Fewest ? Fewest : gain > Most ? Most : gain;
            }
        }

        public bool IsFlying
        {
            get
            {
                return IsCarried
                    && Elapsed >= VictoryStages.OpensAt(VictoryStage.OrbFlight)
                    && Elapsed < VictoryStages.ClosesAt(VictoryStage.Burst);
            }
        }

        public bool HasLanded
        {
            get { return IsCarried && Elapsed >= VictoryStages.ClosesAt(VictoryStage.Burst); }
        }

        public bool IsSpent
        {
            get { return !IsCarried || timeline.IsOver; }
        }

        public float Flare
        {
            get
            {
                var into = Elapsed - VictoryStages.ClosesAt(VictoryStage.OrbFlight);
                if (!IsCarried || into <= 0f || into >= VictoryStages.BurstSeconds)
                {
                    return 0f;
                }

                var through = into / VictoryStages.BurstSeconds;
                return 4f * through * (1f - through);
            }
        }

        public float OpensAt(int orb)
        {
            return VictoryStages.OpensAt(VictoryStage.OrbFlight) + Flown(orb) * Stagger;
        }

        public float LandsAt(int orb)
        {
            return OpensAt(orb) + VictoryStages.OrbFlightSeconds;
        }

        public float ThroughOf(int orb)
        {
            var into = Elapsed - OpensAt(orb);
            if (into <= 0f)
            {
                return 0f;
            }

            var through = into / VictoryStages.OrbFlightSeconds;
            return through > 1f ? 1f : through;
        }

        public WorldPoint PositionOf(int orb, WorldPoint player)
        {
            return At(orb, ThroughOf(orb), player);
        }

        public WorldPoint At(int orb, float through, WorldPoint player)
        {
            Flown(orb);

            var t = through < 0f ? 0f : through > 1f ? 1f : through;
            var flown = WorldPoint.Between(site, player, t * t);
            var bulge = Spread * (float)Math.Sin(Math.PI * t);
            var turn = 2d * Math.PI * orb / Orbs;

            return new WorldPoint(
                flown.X + (float)Math.Cos(turn) * bulge,
                flown.Y + Arc * 4f * t * (1f - t),
                flown.Z + (float)Math.Sin(turn) * bulge);
        }

        public float SizeOf(int orb, int dot)
        {
            if (dot < 0 || dot > TrailDots)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dot), dot, "An orb trails no such dot behind it.");
            }

            var through = ThroughOf(orb) - dot * TrailGap;
            if (through <= 0f || ThroughOf(orb) >= 1f)
            {
                return 0f;
            }

            var size = Size;
            for (var behind = 0; behind < dot; behind++)
            {
                size *= TrailTaper;
            }

            return size;
        }

        public WorldPoint TrailOf(int orb, int dot, WorldPoint player)
        {
            if (dot < 0 || dot > TrailDots)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dot), dot, "An orb trails no such dot behind it.");
            }

            return At(orb, ThroughOf(orb) - dot * TrailGap, player);
        }

        public OrbStream Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "An orb only ever flies forwards.");
            }

            if (!IsCarried || deltaSeconds == 0f)
            {
                return this;
            }

            return new OrbStream(site, gain, timeline.Advanced(deltaSeconds));
        }

        int Flown(int orb)
        {
            if (orb < 0 || orb >= Orbs)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orb), orb, "The stream carries no such orb.");
            }

            return orb;
        }

        public bool Equals(OrbStream other)
        {
            return site.Equals(other.site) && gain == other.gain && timeline.Equals(other.timeline);
        }

        public override bool Equals(object obj)
        {
            return obj is OrbStream other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = site.GetHashCode();
                hash = (hash * 397) ^ gain;
                hash = (hash * 397) ^ timeline.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (!IsCarried)
            {
                return "no orbs";
            }

            return string.Concat(
                Orbs.ToString(CultureInfo.InvariantCulture),
                " orbs worth ",
                gain.ToString(CultureInfo.InvariantCulture),
                " from ",
                site.ToString(),
                ", ",
                HasLanded ? "landed" : IsFlying ? "in flight" : "waiting on the dissolve");
        }
    }
}
