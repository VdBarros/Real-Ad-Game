using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraStaging : IEquatable<CameraStaging>
    {
        readonly CameraFlight flight;
        readonly ZoomBeat beat;

        CameraStaging(CameraFlight flight, ZoomBeat beat)
        {
            this.flight = flight;
            this.beat = beat;
        }

        public static CameraStaging Over(LevelGraph graph)
        {
            return new CameraStaging(CameraFlight.Over(graph), ZoomBeat.None);
        }

        public CameraFraming Constant
        {
            get { return flight.Destination; }
        }

        public CameraFraming Framing
        {
            get { return beat.IsSettled ? flight.Framing : beat.Framing; }
        }

        public bool IsBusy
        {
            get { return !flight.IsSettled || !beat.IsSettled; }
        }

        public CameraStaging Advanced(float deltaSeconds)
        {
            return new CameraStaging(flight.Advanced(deltaSeconds), beat.Advanced(deltaSeconds));
        }

        public CameraStaging CutTo(TilePosition position)
        {
            if (!flight.IsSettled)
            {
                throw new InvalidOperationException(
                    "A beat fires on a pickup or on arrival at the boss, and the flight owns input until it lands. "
                    + "Skip the flight before cutting away from the constant.");
            }

            return new CameraStaging(flight, ZoomBeat.On(LevelFraming.CloseUp(position)));
        }

        public CameraStaging Released()
        {
            return new CameraStaging(flight, beat.Released());
        }

        public CameraStaging Skipped()
        {
            return new CameraStaging(flight.Skipped(), ZoomBeat.None);
        }

        public bool Equals(CameraStaging other)
        {
            return flight.Equals(other.flight) && beat.Equals(other.beat);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraStaging other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (flight.GetHashCode() * 397) ^ beat.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(flight.ToString(), ", ", beat.ToString());
        }
    }
}
