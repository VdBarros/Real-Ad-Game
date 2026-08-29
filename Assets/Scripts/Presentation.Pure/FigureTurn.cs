using System;
using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct FigureTurn : IEquatable<FigureTurn>
    {
        public const float HalfTurnsPerTile = 1.5f;

        const float Aimed = 1e-3f;

        readonly float from;

        readonly float swing;

        readonly float elapsed;

        FigureTurn(float from, float swing, float elapsed)
        {
            this.from = from;
            this.swing = swing;
            this.elapsed = elapsed;
        }

        public static float DegreesPerSecond
        {
            get { return FigureFacing.HalfTurn * HalfTurnsPerTile * Pace.StepsPerSecond; }
        }

        public static float TileSeconds
        {
            get { return 1f / Pace.StepsPerSecond; }
        }

        public static float SecondsToTurn(float degrees)
        {
            return Math.Abs(degrees) / DegreesPerSecond;
        }

        public static FigureTurn Facing(float yaw)
        {
            return new FigureTurn(FigureFacing.Normalised(yaw), 0f, 0f);
        }

        public float Wanted
        {
            get { return FigureFacing.Normalised(from + swing); }
        }

        public float Swing
        {
            get { return swing; }
        }

        public float Seconds
        {
            get { return SecondsToTurn(swing); }
        }

        public float Elapsed
        {
            get { return elapsed; }
        }

        public bool IsSettled
        {
            get { return elapsed >= Seconds; }
        }

        public float Yaw
        {
            get
            {
                var span = Seconds;

                if (span <= 0f || elapsed >= span)
                {
                    return Wanted;
                }

                return FigureFacing.Normalised(from + swing * Eased(elapsed / span));
            }
        }

        public FigureTurn Toward(float yaw)
        {
            var wanted = FigureFacing.Normalised(yaw);

            if (Math.Abs(FigureFacing.Shortest(Wanted, wanted)) <= Aimed)
            {
                return this;
            }

            var here = Yaw;

            return new FigureTurn(here, FigureFacing.Shortest(here, wanted), 0f);
        }

        public FigureTurn Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A turn only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            var span = Seconds;
            var run = elapsed + deltaSeconds;

            return new FigureTurn(from, swing, run > span ? span : run);
        }

        static float Eased(float part)
        {
            return part * part * (3f - 2f * part);
        }

        public bool Equals(FigureTurn other)
        {
            return from.Equals(other.from)
                && swing.Equals(other.swing)
                && elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is FigureTurn other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = from.GetHashCode();
                hash = (hash * 397) ^ swing.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsSettled)
            {
                return string.Concat("facing ", Yaw.ToString("0.#", CultureInfo.InvariantCulture));
            }

            return string.Concat(
                "turning ",
                Yaw.ToString("0.#", CultureInfo.InvariantCulture),
                " toward ",
                Wanted.ToString("0.#", CultureInfo.InvariantCulture));
        }
    }
}
