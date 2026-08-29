using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class Journey : IEquatable<Journey>
    {
        public static readonly Journey Nowhere =
            new Journey(null, Walk.Nowhere, null, false, Fight.None);

        readonly bool owesABeat;
        readonly Fight fight;

        Journey(RunState state, Walk walk, ActionResult arrival, bool owesABeat, Fight fight)
        {
            State = state;
            Walk = walk;
            Arrival = arrival;
            this.owesABeat = owesABeat;
            this.fight = fight;
        }

        public static Journey Toward(RunState state, int nodeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var resolved = ActionResolver.Along(state, NavigationMap.Of(state).RouteTo(nodeId));
            if (resolved.Outcome == ActionOutcome.Rejected)
            {
                return Nowhere;
            }

            return new Journey(
                state, Walk.Along(TileRoute.Of(state.Level, resolved.Route)), null, false, Fight.None);
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

        public Fight Fight
        {
            get { return fight; }
        }

        public bool HoldsForAFight
        {
            get { return IsWaiting && !fight.IsSettled; }
        }

        public Journey Advanced(float deltaSeconds)
        {
            if (IsOver)
            {
                return this;
            }

            if (IsWaiting)
            {
                var fought = fight.Advanced(deltaSeconds);
                return fought.Equals(fight)
                    ? this
                    : new Journey(State, Walk, Arrival, owesABeat, fought);
            }

            var walked = Walk.Advanced(deltaSeconds);
            if (!walked.IsWaiting)
            {
                return new Journey(State, walked, null, false, Fight.None);
            }

            var reached = walked.ArrivedNodeId;
            var spendsAMultiplier = !State.IsConsumed(reached)
                && State.Level.Decisions.Node(reached).Type == NodeType.Multiplier;
            var resolved = ActionResolver.Along(State, new[] { State.PositionNodeId, reached });

            return new Journey(
                resolved.State, walked, resolved, spendsAMultiplier, Fight.Of(resolved.Outcome));
        }

        public Journey Resumed()
        {
            if (!IsWaiting || !fight.IsSettled)
            {
                return this;
            }

            return new Journey(
                State,
                StandsWhereItArrived ? Walk.Resumed() : Walk.Backtracked(),
                null,
                false,
                Fight.None);
        }

        public Journey Cancelled()
        {
            if (IsOver)
            {
                return this;
            }

            if (IsWaiting)
            {
                return new Journey(State, Walk.Stopped(), Arrival, owesABeat, fight);
            }

            return new Journey(State, Walk.Backtracked(), Arrival, owesABeat, fight);
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
                && fight.Equals(other.fight)
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
                hash = (hash * 397) ^ fight.GetHashCode();
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
