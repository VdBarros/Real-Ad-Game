using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class FigureCues
    {
        public static FigureCue Of(Journey journey, PlayerWeapon gripped)
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
                return Striking(fight, gripped);
            }

            if (journey.HoldsForABeat)
            {
                return FigureCue.Within(FigureAct.Take, Take.Seconds);
            }

            return FigureCue.Still;
        }

        public static FigureAct FinisherOf(PlayerWeapon gripped)
        {
            switch (gripped)
            {
                case PlayerWeapon.None:
                    return FigureAct.Kick;
                case PlayerWeapon.Shortsword:
                    return FigureAct.Slice;
                case PlayerWeapon.Axe:
                    return FigureAct.Cleave;
                case PlayerWeapon.Spear:
                    return FigureAct.Thrust;
                case PlayerWeapon.Greatsword:
                    return FigureAct.Sweep;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(gripped), gripped, "No finisher is swung with that weapon.");
            }
        }

        public static FigureCue Striking(Fight fight, PlayerWeapon gripped)
        {
            if (!fight.IsJoined)
            {
                return FigureCue.Still;
            }

            switch (fight.Outcome)
            {
                case ActionOutcome.Win:
                    return fight.IsExecuting
                        ? FigureCue.Within(FinisherOf(gripped), fight.BlowBeat)
                        : FigureCue.Still;
                case ActionOutcome.Tie:
                    return FigureCue.Within(FigureAct.Clash, fight.BlowBeat);
                case ActionOutcome.Loss:
                    return fight.HasStruck
                        ? FigureCue.Within(FigureAct.Fall, fight.FallBeat)
                        : FigureCue.Still;
                default:
                    return FigureCue.Still;
            }
        }

        public static FigureCue Answering(Fight fight)
        {
            if (!fight.IsJoined)
            {
                return FigureCue.Still;
            }

            switch (fight.Outcome)
            {
                case ActionOutcome.Win:
                    return fight.HasStruck
                        ? FigureCue.Within(FigureAct.Fall, fight.FallBeat)
                        : FigureCue.Still;
                case ActionOutcome.Tie:
                    return FigureCue.Within(FigureAct.Clash, fight.BlowBeat);
                case ActionOutcome.Loss:
                    return FigureCue.Within(FigureAct.Strike, fight.BlowBeat);
                default:
                    return FigureCue.Still;
            }
        }
    }
}
