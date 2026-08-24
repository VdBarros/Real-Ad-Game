using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public sealed class OracleVerdict
    {
        internal OracleVerdict(int stalls, OracleStall firstStall, int peakStates, int exploredStates, bool aborted)
        {
            Stalls = stalls;
            FirstStall = firstStall;
            PeakStates = peakStates;
            ExploredStates = exploredStates;
            Aborted = aborted;
        }

        public int Stalls { get; }

        public OracleStall FirstStall { get; }

        public int PeakStates { get; }

        public int ExploredStates { get; }

        public bool Aborted { get; }

        public bool Stalled
        {
            get { return Stalls > 0; }
        }

        public bool IsSafe
        {
            get { return !Aborted && Stalls == 0; }
        }

        public override string ToString()
        {
            var description = new StringBuilder();

            if (Aborted)
            {
                description.Append("budget blown after ");
            }
            else if (Stalls == 0)
            {
                description.Append("no stall in ");
            }
            else
            {
                description.Append(Stalls.ToString(CultureInfo.InvariantCulture));
                description.Append(Stalls == 1 ? " stall in " : " stalls in ");
            }

            description.Append(ExploredStates.ToString(CultureInfo.InvariantCulture));
            description.Append(" states explored, peak ");
            description.Append(PeakStates.ToString(CultureInfo.InvariantCulture));

            if (FirstStall != null)
            {
                description.Append(" — ");
                description.Append(FirstStall);
            }

            return description.ToString();
        }
    }
}
