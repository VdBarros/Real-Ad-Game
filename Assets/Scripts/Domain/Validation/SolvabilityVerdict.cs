using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public sealed class SolvabilityVerdict
    {
        SolvabilityVerdict(
            SolvabilityReason reason,
            int offendingNodeId,
            int bossNodeId,
            int bossPower,
            long bound,
            int beelinePower,
            bool beelineBlocked,
            StallReport stall)
        {
            Reason = reason;
            OffendingNodeId = offendingNodeId;
            BossNodeId = bossNodeId;
            BossPower = bossPower;
            Bound = bound;
            BeelinePower = beelinePower;
            BeelineBlocked = beelineBlocked;
            Stall = stall;
        }

        internal static SolvabilityVerdict Malformed(SolvabilityReason reason, int offendingNodeId)
        {
            return new SolvabilityVerdict(reason, offendingNodeId, -1, 0, 0, 0, false, null);
        }

        internal static SolvabilityVerdict Judged(
            SolvabilityReason reason,
            int offendingNodeId,
            int bossNodeId,
            int bossPower,
            long bound,
            int beelinePower,
            bool beelineBlocked,
            StallReport stall)
        {
            return new SolvabilityVerdict(
                reason, offendingNodeId, bossNodeId, bossPower, bound, beelinePower, beelineBlocked, stall);
        }

        public bool IsSafe
        {
            get { return Reason == SolvabilityReason.None; }
        }

        public SolvabilityReason Reason { get; }

        public int OffendingNodeId { get; }

        public int BossNodeId { get; }

        public int BossPower { get; }

        public long Bound { get; }

        public int BeelinePower { get; }

        public bool BeelineBlocked { get; }

        public StallReport Stall { get; }

        public override string ToString()
        {
            var description = new StringBuilder();

            if (IsSafe)
            {
                description.Append("safe");
            }
            else
            {
                description.Append(Reason);
                if (OffendingNodeId >= 0)
                {
                    description.Append(" at #");
                    description.Append(OffendingNodeId.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (BossNodeId >= 0)
            {
                description.Append(" — boss ");
                description.Append(BossPower.ToString(CultureInfo.InvariantCulture));
                description.Append(" under a bound of ");
                description.Append(Bound.ToString(CultureInfo.InvariantCulture));
                description.Append(", beeline ");
                description.Append(BeelinePower.ToString(CultureInfo.InvariantCulture));
                description.Append(BeelineBlocked ? " (blocked)" : string.Empty);
            }

            if (Stall != null)
            {
                description.Append(" — ");
                description.Append(Stall);
            }

            return description.ToString();
        }
    }
}
