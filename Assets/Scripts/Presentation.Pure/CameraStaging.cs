using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraStaging : IEquatable<CameraStaging>
    {
        readonly CameraFlight flight;
        readonly CameraFollow follow;
        readonly ZoomBeat beat;
        readonly CameraPan pan;

        CameraStaging(CameraFlight flight, CameraFollow follow, ZoomBeat beat, CameraPan pan)
        {
            this.flight = flight;
            this.follow = follow;
            this.beat = beat;
            this.pan = pan;
        }

        public static CameraStaging Over(LevelGraph graph)
        {
            var flight = CameraFlight.Over(graph);

            return new CameraStaging(
                flight,
                CameraFollow.From(flight.Destination, LevelFraming.StartPoint(graph)),
                ZoomBeat.None,
                CameraPan.Around(graph));
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

        public bool IsAway
        {
            get { return pan.IsAway && !Framing.Equals(LevelFraming.Play(Subject)); }
        }

        public CameraStaging Advanced(float deltaSeconds)
        {
            var flown = flight.Advanced(deltaSeconds);
            var followed = flight.IsSettled ? follow.Advanced(deltaSeconds) : follow;

            return new CameraStaging(
                flown,
                followed,
                beat.Advanced(deltaSeconds),
                followed.IsSettled ? pan.Arrived() : pan);
        }

        public CameraStaging Follows(WorldPoint subject)
        {
            return new CameraStaging(flight, follow.Toward(subject), beat, pan);
        }

        public CameraStaging Looks(WorldPoint offset)
        {
            if (IsBusy)
            {
                return this;
            }

            var dragged = pan.Dragged(follow.Framing.Target, offset);

            return new CameraStaging(flight, follow.LookingAt(dragged.Look), beat, dragged);
        }

        public CameraStaging LookHeld()
        {
            var held = pan.LetGo();

            return held.Equals(pan) ? this : new CameraStaging(flight, follow, beat, held);
        }

        public CameraStaging LooksBack()
        {
            return new CameraStaging(flight, follow.LookingBack(), beat, pan.Recalled());
        }

        public CameraStaging CutTo(TilePosition position)
        {
            if (!flight.IsSettled)
            {
                throw new InvalidOperationException(
                    "A beat fires on a pickup or on arrival at the boss, and the flight owns input until it lands. "
                    + "Skip the flight before cutting away from the follow.");
            }

            return new CameraStaging(
                flight, follow.LookingBack(), ZoomBeat.On(LevelFraming.CloseUp(position)), pan.Dropped());
        }

        public CameraStaging Released()
        {
            return new CameraStaging(flight, follow, beat.Released(), pan);
        }

        public CameraStaging Skipped()
        {
            return new CameraStaging(flight.Skipped(), follow, ZoomBeat.None, pan);
        }

        public bool Equals(CameraStaging other)
        {
            return flight.Equals(other.flight)
                && follow.Equals(other.follow)
                && beat.Equals(other.beat)
                && pan.Equals(other.pan);
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
                hash = (hash * 397) ^ pan.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                flight.ToString(), ", ", follow.ToString(), ", ", beat.ToString(), ", ", pan.ToString());
        }
    }
}
