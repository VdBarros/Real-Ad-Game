using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class FigureCues
    {
        public static FigureCue Of(Journey journey)
        {
            if (journey == null || journey.IsOver)
            {
                return FigureCue.Still;
            }

            if (!journey.Walk.IsWaiting)
            {
                return journey.Walk.IsRetreating ? FigureCue.Looping(FigureAct.Retreat) : FigureCue.Walking;
            }

            var fight = journey.Fight;
            if (fight.IsJoined && !fight.IsSettled)
            {
                return Striking(fight);
            }

            if (journey.HoldsForABeat)
            {
                return FigureCue.Within(FigureAct.Take, Take.Seconds);
            }

            return FigureCue.Still;
        }

        public static FigureCue Striking(Fight fight)
        {
            if (!fight.IsJoined)
            {
                return FigureCue.Still;
            }

            switch (fight.Outcome)
            {
                case ActionOutcome.Win:
                    return FigureCue.Within(FigureAct.Strike, fight.Seconds);
                case ActionOutcome.Tie:
                    return FigureCue.Within(FigureAct.Clash, fight.Seconds);
                case ActionOutcome.Loss:
                    return FigureCue.Within(FigureAct.Recoil, fight.Seconds);
                default:
                    return FigureCue.Still;
            }
        }
    }
}
