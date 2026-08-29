using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class Journey : IEquatable<Journey>
    {
        public static readonly Journey Nowhere =
            new Journey(null, Walk.Nowhere, null, false, Fight.None, TapAim.Nothing);

        readonly bool owesABeat;
        readonly Fight fight;
        readonly int diversion;

        Journey(
            RunState state,
            Walk walk,
            ActionResult arrival,
            bool owesABeat,
            Fight fight,
            int diversion)
        {
            State = state;
            Walk = walk;
            Arrival = arrival;
            this.owesABeat = owesABeat;
            this.fight = fight;
            this.diversion = diversion;
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
                state,
                Walk.Along(TileRoute.Of(state.Level, resolved.Route)),
                null,
                false,
                Fight.None,
                TapAim.Nothing);
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

        public int Diversion
        {
            get { return diversion; }
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
                    : new Journey(State, Walk, Arrival, owesABeat, fought, diversion);
            }

            var walked = Walk.Advanced(deltaSeconds);
            if (!walked.IsWaiting)
            {
                return new Journey(State, walked, null, false, Fight.None, diversion);
            }

            var reached = walked.ArrivedNodeId;
            var spendsAMultiplier = !State.IsConsumed(reached)
                && State.Level.Decisions.Node(reached).Type == NodeType.Multiplier;
            var resolved = ActionResolver.Along(State, new[] { State.PositionNodeId, reached });

            return new Journey(
                resolved.State,
                walked,
                resolved,
                spendsAMultiplier,
                Fight.Of(resolved.Outcome),
                diversion);
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
                Fight.None,
                diversion);
        }

        public Journey Cancelled()
        {
            return BrokenOff(TapAim.Nothing);
        }

        public Journey DivertedTo(int nodeId)
        {
            return BrokenOff(nodeId);
        }

        public Journey Onward()
        {
            if (!IsOver || State == null || diversion == TapAim.Nothing)
            {
                return Nowhere;
            }

            return Toward(State, diversion);
        }

        Journey BrokenOff(int nextNodeId)
        {
            if (IsOver)
            {
                return this;
            }

            return new Journey(
                State,
                IsWaiting ? Walk.Stopped() : Walk.Backtracked(),
                Arrival,
                owesABeat,
                fight,
                nextNodeId);
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
                && diversion == other.diversion
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
                hash = (hash * 397) ^ diversion;
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
