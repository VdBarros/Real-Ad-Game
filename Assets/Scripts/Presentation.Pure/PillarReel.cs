using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct PillarReel : IEquatable<PillarReel>
    {
        readonly float elapsed;

        PillarReel(float elapsed)
        {
            this.elapsed = elapsed;
        }

        public static PillarReel Opening
        {
            get { return default(PillarReel); }
        }

        public float Elapsed
        {
            get { return elapsed; }
        }

        public bool IsOver
        {
            get { return elapsed >= PillarStage.Total; }
        }

        public PillarReel Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A reel only ever runs forwards.");
            }

            var moved = elapsed + deltaSeconds;
            return new PillarReel(moved >= PillarStage.Total ? PillarStage.Total : moved);
        }

        public PillarReel Skipped()
        {
            return new PillarReel(PillarStage.Total);
        }

        public PillarBeat Beat
        {
            get
            {
                if (elapsed >= PillarStage.Total)
                {
                    return PillarBeat.Over;
                }

                if (elapsed >= PillarStage.Fall)
                {
                    return PillarBeat.Fall;
                }

                if (elapsed >= PillarStage.Cross)
                {
                    return PillarBeat.Cross;
                }

                if (elapsed >= PillarStage.Crown)
                {
                    return PillarBeat.Crown;
                }

                if (elapsed >= PillarStage.Count)
                {
                    return PillarBeat.Count;
                }

                if (elapsed >= PillarStage.Drain)
                {
                    return PillarBeat.Drain;
                }

                return elapsed >= PillarStage.Throw ? PillarBeat.Throw : PillarBeat.Establish;
            }
        }

        public CastMark Player
        {
            get
            {
                var number = PillarStage.PlayerNumberAt(elapsed);
                var height = PillarStage.HeightOf(number);
                var stand = PillarStage.Stand(PillarStage.PlayerOffset, height);
                var dropped = PillarStage.FallDepth * PillarStage.Ease(PillarStage.PlayerFallAt(elapsed));

                return new CastMark(
                    number,
                    elapsed >= PillarStage.Drain ? CastLook.Skeleton : CastLook.Peasant,
                    BadgeStyle.Player,
                    height,
                    PillarStage.Stand(PillarStage.PlayerOffset, 0f),
                    new WorldPoint(stand.X, stand.Y - dropped, stand.Z));
            }
        }

        public CastMark Girl
        {
            get
            {
                var height = PillarStage.GirlHeightAt(elapsed);

                return new CastMark(
                    PillarStage.GirlNumberAt(elapsed),
                    elapsed >= PillarStage.Crown ? CastLook.Queen : CastLook.Peasant,
                    BadgeStyle.Enemy,
                    height,
                    PillarStage.Stand(PillarStage.GirlOffset, 0f),
                    PillarStage.Stand(PillarStage.GirlOffsetAt(elapsed), height));
            }
        }

        public CastMark Rival
        {
            get
            {
                var height = PillarStage.HeightOf(PillarStage.RivalNumber);

                return new CastMark(
                    PillarStage.RivalNumber,
                    CastLook.Champion,
                    BadgeStyle.Enemy,
                    height,
                    PillarStage.Stand(PillarStage.RivalOffset, 0f),
                    PillarStage.Stand(PillarStage.RivalOffset, height));
            }
        }

        public bool HeartIsFlying
        {
            get { return elapsed >= PillarStage.Throw && elapsed < PillarStage.Drain; }
        }

        public WorldPoint HeartPosition
        {
            get
            {
                var thrown = PillarStage.Ease(
                    (elapsed - PillarStage.Throw) / (PillarStage.Drain - PillarStage.Throw));

                return WorldPoint.Between(Player.Position, Girl.Position, thrown);
            }
        }

        public float PortalOpen
        {
            get { return PillarStage.PortalOpenAt(elapsed); }
        }

        public float PlayerFall
        {
            get { return PillarStage.PlayerFallAt(elapsed); }
        }

        public CameraFraming Framing
        {
            get { return PillarStage.FramingAt(elapsed); }
        }

        public bool Equals(PillarReel other)
        {
            return elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is PillarReel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return elapsed.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(
                Beat.ToString(),
                " at ",
                elapsed.ToString("0.###", CultureInfo.InvariantCulture),
                "s of ",
                PillarStage.Total.ToString("0.###", CultureInfo.InvariantCulture),
                "s");
        }
    }
}
