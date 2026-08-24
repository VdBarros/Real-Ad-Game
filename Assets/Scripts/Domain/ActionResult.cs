using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class ActionResult
    {
        static readonly int[] NoRoute = new int[0];

        ActionResult(ActionOutcome outcome, RunState state, IReadOnlyList<int> route)
        {
            Outcome = outcome;
            State = state;
            Route = route;
        }

        public ActionOutcome Outcome { get; }

        public RunState State { get; }

        public IReadOnlyList<int> Route { get; }

        internal static ActionResult Rejected(RunState state)
        {
            return new ActionResult(ActionOutcome.Rejected, state, NoRoute);
        }

        internal static ActionResult Of(ActionOutcome outcome, RunState state, IReadOnlyList<int> route)
        {
            if (outcome == ActionOutcome.Rejected)
            {
                throw new ArgumentException("A rejected tap carries no route.", nameof(outcome));
            }

            return new ActionResult(outcome, state, route);
        }
    }
}
