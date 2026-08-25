using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class Journey : IEquatable<Journey>
    {
        public static readonly Journey Nowhere = new Journey(null, Walk.Nowhere, null, false);

        readonly bool owesABeat;

        Journey(RunState state, Walk walk, ActionResult arrival, bool owesABeat)
        {
            State = state;
            Walk = walk;
            Arrival = arrival;
            this.owesABeat = owesABeat;
        }

        public static Journey Toward(RunState state, int nodeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var resolved = ActionResolver.Resolve(state, nodeId);
            if (resolved.Outcome == ActionOutcome.Rejected)
            {
                return Nowhere;
            }

            return new Journey(state, Walk.Along(TileRoute.Of(state.Level, resolved.Route)), null, false);
        }

        public RunState State { get; }

        public Walk Walk { get; }

        public ActionResult Arrival { get; }

        public bool IsOver
        {
            get { return Walk.IsSettled; }
        }

        public bool IsWaiting
        {
            get { return Walk.IsWaiting; }
        }

        public bool HoldsForABeat
        {
            get { return IsWaiting && owesABeat; }
        }

        public Journey Advanced(float deltaSeconds)
        {
            if (IsOver || IsWaiting)
            {
                return this;
            }

            var walked = Walk.Advanced(deltaSeconds);
            if (!walked.IsWaiting)
            {
                return new Journey(State, walked, null, false);
            }

            var reached = walked.ArrivedNodeId;
            var spendsAMultiplier = !State.IsConsumed(reached)
                && State.Level.Decisions.Node(reached).Type == NodeType.Multiplier;
            var resolved = ActionResolver.Resolve(State, reached);

            return new Journey(resolved.State, walked, resolved, spendsAMultiplier);
        }

        public Journey Resumed()
        {
            if (!IsWaiting)
            {
                return this;
            }

            return new Journey(
                State, StandsWhereItArrived ? Walk.Resumed() : Walk.Backtracked(), null, false);
        }

        public Journey Cancelled()
        {
            if (IsOver)
            {
                return this;
            }

            if (IsWaiting && StandsWhereItArrived)
            {
                return new Journey(State, Walk.Stopped(), Arrival, owesABeat);
            }

            return new Journey(State, Walk.Backtracked(), Arrival, owesABeat);
        }

        bool StandsWhereItArrived
        {
            get { return State.PositionNodeId == Walk.ArrivedNodeId; }
        }

        public bool Equals(Journey other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Walk.Equals(other.Walk)
                && owesABeat == other.owesABeat
                && ReferenceEquals(Arrival, other.Arrival)
                && (State == null ? other.State == null : State.Equals(other.State));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Journey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Walk.GetHashCode();
                hash = (hash * 397) ^ (owesABeat ? 1 : 0);
                hash = (hash * 397) ^ (State == null ? 0 : State.GetHashCode());
                return hash;
            }
        }

        public override string ToString()
        {
            return Walk.ToString();
        }
    }
}
