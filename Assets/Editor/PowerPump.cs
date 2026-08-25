using Game.Presentation;

namespace Game.EditorTooling
{
    public static class PowerPump
    {
        public const float Frame = 1f / 60f;

        public const int Ceiling = 200;

        public static void Settle(PowerBadge power, int target)
        {
            if (power == null || power.Power == target)
            {
                return;
            }

            power.Show(target);
            for (var frame = 0; frame < Ceiling && !power.IsSettled; frame++)
            {
                power.Advance(Frame);
            }
        }
    }
}
