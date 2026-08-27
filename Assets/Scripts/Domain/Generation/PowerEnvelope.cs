using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class PowerEnvelope
    {
        readonly List<RegionEnvelope> regions;

        PowerEnvelope(List<RegionEnvelope> regions)
        {
            this.regions = regions;
        }

        public IReadOnlyList<RegionEnvelope> Regions
        {
            get { return regions; }
        }

        public static PowerEnvelope Of(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return Of(ContentBoard.Of(level), tuning);
        }

        internal static PowerEnvelope Of(ContentBoard board, PowerTuning tuning)
        {
            var startRegionId = board.RegionOf(board.StartNodeId);
            var rows = new List<RegionEnvelope>(board.RegionIds.Count);
            foreach (var regionId in board.RegionIds)
            {
                var cheapestEnemy = 0;
                var dearestEnemy = 0;
                for (var nodeId = 0; nodeId < board.Count; nodeId++)
                {
                    if (board.TypeOf(nodeId) != NodeType.Enemy || board.RegionOf(nodeId) != regionId)
                    {
                        continue;
                    }

                    var value = board.ValueOf(nodeId);
                    if (cheapestEnemy == 0 || value < cheapestEnemy)
                    {
                        cheapestEnemy = value;
                    }

                    if (value > dearestEnemy)
                    {
                        dearestEnemy = value;
                    }
                }

                rows.Add(new RegionEnvelope(
                    regionId,
                    EnvelopeWalks.CheapestUnlock(board, tuning, regionId),
                    EnvelopeWalks.RichestEntry(board, tuning, regionId),
                    cheapestEnemy,
                    dearestEnemy,
                    regionId == startRegionId));
            }

            return new PowerEnvelope(rows);
        }

        public RegionEnvelope FirstRegionUnderTheFloor(double floor)
        {
            foreach (var region in regions)
            {
                if (!region.SpreadClears(floor))
                {
                    return region;
                }
            }

            return null;
        }

        public bool FloorHolds
        {
            get
            {
                foreach (var region in regions)
                {
                    if (!region.FloorHolds)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool WallsAreOrdered
        {
            get
            {
                foreach (var region in regions)
                {
                    if (!region.WallsAreOrdered)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
