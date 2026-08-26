using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraStaging : IEquatable<CameraStaging>
    {
        readonly CameraFlight flight;
        readonly CameraFollow follow;
        readonly ZoomBeat beat;

        CameraStaging(CameraFlight flight, CameraFollow follow, ZoomBeat beat)
        {
            this.flight = flight;
            this.follow = follow;
            this.beat = beat;
        }

        public static CameraStaging Over(LevelGraph graph)
        {
            var flight = CameraFlight.Over(graph);

            return new CameraStaging(
                flight,
                CameraFollow.From(flight.Destination, LevelFraming.StartPoint(graph)),
                ZoomBeat.None);
        }

        public CameraFraming Reveal
        {
            get { return flight.Destination; }
        }

        public CameraFraming Following
        {
            get { return follow.Framing; }
        }

        public WorldPoint Subject
        {
            get { return follow.Subject; }
        }

        public CameraFraming Framing
        {
            get
            {
                if (!beat.IsSettled)
                {
                    return beat.Framing;
                }

                return flight.IsSettled ? follow.Framing : flight.Framing;
            }
        }

        public bool IsBusy
        {
            get { return !flight.IsSettled || !beat.IsSettled; }
        }

        public bool IsSettled
        {
            get { return flight.IsSettled && beat.IsSettled && follow.IsSettled; }
        }

        public CameraStaging Advanced(float deltaSeconds)
        {
            return new CameraStaging(
                flight.Advanced(deltaSeconds),
                flight.IsSettled ? follow.Advanced(deltaSeconds) : follow,
                beat.Advanced(deltaSeconds));
        }

        public CameraStaging Follows(WorldPoint subject)
        {
            return new CameraStaging(flight, follow.Toward(subject), beat);
        }

        public CameraStaging CutTo(TilePosition position)
        {
            if (!flight.IsSettled)
            {
                throw new InvalidOperationException(
                    "A beat fires on a pickup or on arrival at the boss, and the flight owns input until it lands. "
                    + "Skip the flight before cutting away from the follow.");
            }

            return new CameraStaging(flight, follow, ZoomBeat.On(LevelFraming.CloseUp(position)));
        }

        public CameraStaging Released()
        {
            return new CameraStaging(flight, follow, beat.Released());
        }

        public CameraStaging Skipped()
        {
            return new CameraStaging(flight.Skipped(), follow, ZoomBeat.None);
        }

        public bool Equals(CameraStaging other)
        {
            return flight.Equals(other.flight) && follow.Equals(other.follow) && beat.Equals(other.beat);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraStaging other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = flight.GetHashCode();
                hash = (hash * 397) ^ follow.GetHashCode();
                hash = (hash * 397) ^ beat.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(flight.ToString(), ", ", follow.ToString(), ", ", beat.ToString());
        }
    }
}
