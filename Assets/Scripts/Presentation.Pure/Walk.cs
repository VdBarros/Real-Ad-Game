using System;
using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct Walk : IEquatable<Walk>
    {
        public const float StepsPerSecond = Pace.StepsPerSecond;

        readonly TileRoute route;
        readonly int stage;
        readonly float travelled;
        readonly bool retreating;

        Walk(TileRoute route, int stage, float travelled, bool retreating)
        {
            this.route = route;
            this.stage = stage;
            this.travelled = travelled;
            this.retreating = retreating;
        }

        public static Walk Nowhere
        {
            get { return default(Walk); }
        }

        public static Walk Along(TileRoute followed)
        {
            if (followed == null)
            {
                throw new ArgumentNullException(nameof(followed));
            }

            return new Walk(followed, 1, 0f, false);
        }

        public TileRoute Route
        {
            get { return route; }
        }

        public float Travelled
        {
            get { return travelled; }
        }

        public bool IsRetreating
        {
            get { return retreating; }
        }

        public bool IsSettled
        {
            get
            {
                if (route == null || stage >= route.Nodes.Count)
                {
                    return true;
                }

                return retreating && travelled <= LastStood;
            }
        }

        public bool IsWaiting
        {
            get { return !IsSettled && !retreating && travelled >= route.TileOf(stage); }
        }

        public int ArrivedNodeId
        {
            get { return IsWaiting ? route.Nodes[stage] : TapAim.Nothing; }
        }

        public WorldPoint Position
        {
            get
            {
                if (route == null)
                {
                    return default(WorldPoint);
                }

                var behind = (int)travelled;
                if (behind >= route.Steps)
                {
                    return IsoProjection.Of(route.Tiles[route.Steps]);
                }

                return WorldPoint.Between(
                    IsoProjection.Of(route.Tiles[behind]),
                    IsoProjection.Of(route.Tiles[behind + 1]),
                    travelled - behind);
            }
        }

        public WorldPoint Facing
        {
            get
            {
                if (route == null || route.Steps < 1)
                {
                    return default(WorldPoint);
                }

                var ahead = IsWaiting ? (int)travelled : (int)travelled + 1;
                if (ahead > route.Steps)
                {
                    ahead = route.Steps;
                }

                return Heading(route.Tiles[ahead - 1], route.Tiles[ahead]);
            }
        }

        static WorldPoint Heading(TilePosition from, TilePosition to)
        {
            var start = IsoProjection.Of(from);
            var end = IsoProjection.Of(to);
            var x = end.X - start.X;
            var z = end.Z - start.Z;
            var length = (float)Math.Sqrt(x * x + z * z);

            return length <= 0f ? default(WorldPoint) : new WorldPoint(x / length, 0f, z / length);
        }

        public Walk Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A walk only ever runs forwards.");
            }

            if (IsSettled || IsWaiting)
            {
                return this;
            }

            var covered = deltaSeconds * StepsPerSecond;

            if (retreating)
            {
                var back = travelled - covered;
                return new Walk(route, stage, back < LastStood ? LastStood : back, true);
            }

            var reached = route.TileOf(stage);
            var walked = travelled + covered;
            return new Walk(route, stage, walked > reached ? reached : walked, false);
        }

        public Walk Resumed()
        {
            return IsWaiting ? new Walk(route, stage + 1, travelled, false) : this;
        }

        public Walk Stopped()
        {
            return IsSettled ? this : new Walk(route.Upto(stage), stage, travelled, retreating);
        }

        public Walk Backtracked()
        {
            return IsSettled || retreating ? this : new Walk(route, stage, travelled, true);
        }

        int LastStood
        {
            get { return route.TileOf(stage - 1); }
        }

        public bool Equals(Walk other)
        {
            return ReferenceEquals(route, other.route)
                && stage == other.stage
                && travelled.Equals(other.travelled)
                && retreating == other.retreating;
        }

        public override bool Equals(object obj)
        {
            return obj is Walk other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = route == null ? 0 : route.GetHashCode();
                hash = (hash * 397) ^ stage;
                hash = (hash * 397) ^ travelled.GetHashCode();
                hash = (hash * 397) ^ (retreating ? 1 : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsSettled)
            {
                return "standing still";
            }

            return string.Concat(
                retreating ? "falling back to node " : IsWaiting ? "waiting on node " : "walking to node ",
                route.Nodes[retreating ? stage - 1 : stage].ToString(CultureInfo.InvariantCulture));
        }
    }
}
