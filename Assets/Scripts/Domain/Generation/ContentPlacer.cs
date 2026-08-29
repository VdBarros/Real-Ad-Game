using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class ContentPlacer
    {
        public static bool TryPlace(
            MazeLayout layout,
            ContentRecipe recipe,
            PowerTuning tuning,
            out PlacedLevel placed,
            out ContentRejection rejection)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            placed = null;

            if (layout.SlotNodeIds.Count != recipe.Slots)
            {
                rejection = ContentRejection.RecipeSlotMismatch;
                return false;
            }

            var board = ContentBoard.Of(layout.Graph);
            var random = StageRandom.ForStage(layout.AttemptSeed, "content");

            var bossNodeId = Detours.DeepestSlotOf(layout);
            if (!TryAssignRoles(board, layout, recipe, tuning, random, bossNodeId))
            {
                rejection = ContentRejection.RolesUnfilled;
                return false;
            }

            rejection = Mint(board, tuning, random);
            if (rejection != ContentRejection.None)
            {
                return false;
            }

            var passes = RepairRegionFloor(board, tuning);

            board.SetValue(bossNodeId, BossNumber(board, tuning));

            var envelope = PowerEnvelope.Of(board, tuning);
            if (!envelope.FloorHolds)
            {
                rejection = ContentRejection.RegionFloorUnmet;
                return false;
            }

            if (!envelope.WallsAreOrdered)
            {
                rejection = ContentRejection.EnvelopeInverted;
                return false;
            }

            if (envelope.FirstRegionUnderTheFloor(tuning.SpreadFloor) != null)
            {
                rejection = ContentRejection.RegionSpreadTooThin;
                return false;
            }

            if (OpeningFrontier.Of(board, tuning).Count < tuning.OpeningChoices)
            {
                rejection = ContentRejection.OpeningWithoutAChoice;
                return false;
            }

            var graph = board.Rebuild();
            var verdict = SolvabilityValidator.Validate(graph, tuning);
            if (!verdict.IsSafe)
            {
                rejection = RejectionOf(verdict);
                return false;
            }

            placed = new PlacedLevel(layout, graph, recipe, tuning, passes, envelope, verdict);
            return true;
        }

        static ContentRejection RejectionOf(SolvabilityVerdict verdict)
        {
            switch (verdict.Reason)
            {
                case SolvabilityReason.GatedBehindBoss:
                    return ContentRejection.GatedBehindBoss;

                case SolvabilityReason.BossBeyondBound:
                    return ContentRejection.BossBeyondBound;

                case SolvabilityReason.BossWithinReach:
                    return ContentRejection.BossWithinReach;

                case SolvabilityReason.AdversaryStalled:
                    return ContentRejection.PanelStalled;

                case SolvabilityReason.MultiplierProductBeyondCap:
                    return ContentRejection.MultiplierProductBeyondCap;

                default:
                    throw new InvalidOperationException(
                        "Placement built a level no seed can excuse: " + verdict + ".");
            }
        }

        static bool TryAssignRoles(
            ContentBoard board,
            MazeLayout layout,
            ContentRecipe recipe,
            PowerTuning tuning,
            StageRandom random,
            int bossNodeId)
        {
            var isGate = new bool[board.Count];
            foreach (var nodeId in ArticulationPoints.Of(layout.Graph.Decisions))
            {
                isGate[nodeId] = true;
            }

            var detours = tuning.PickupsAskForADetour ? Detours.Of(layout, bossNodeId) : null;

            var rest = new List<int>();
            var pockets = new List<int>();
            var gates = new List<int>();

            foreach (var slotId in layout.SlotNodeIds)
            {
                if (slotId == bossNodeId)
                {
                    continue;
                }

                rest.Add(slotId);
                if (detours != null)
                {
                    if (detours.Holds(slotId))
                    {
                        pockets.Add(slotId);
                    }
                    else if (isGate[slotId])
                    {
                        gates.Add(slotId);
                    }

                    continue;
                }

                if (layout.Graph.Tiles.Neighbours(layout.Graph.Decisions.Node(slotId).Position).Count == 1)
                {
                    pockets.Add(slotId);
                }
                else if (isGate[slotId])
                {
                    gates.Add(slotId);
                }
            }

            var role = new NodeType[board.Count];
            var needEnemies = recipe.Enemies;
            var needMultipliers = recipe.Multipliers;
            var needAdditives = recipe.Additives;

            foreach (var nodeId in random.Shuffled(pockets))
            {
                var treasureWanted = needMultipliers > 0 || needAdditives > 0;
                var takesTreasure = detours != null
                    ? treasureWanted
                    : random.NextDouble() < tuning.PocketTreasure && treasureWanted;

                if (takesTreasure)
                {
                    if (needMultipliers > 0
                        && random.NextDouble() < (double)needMultipliers / (needMultipliers + needAdditives))
                    {
                        role[nodeId] = NodeType.Multiplier;
                        needMultipliers--;
                    }
                    else if (needAdditives > 0)
                    {
                        role[nodeId] = NodeType.Additive;
                        needAdditives--;
                    }
                    else
                    {
                        role[nodeId] = NodeType.Multiplier;
                        needMultipliers--;
                    }
                }
                else if (needEnemies > 0)
                {
                    role[nodeId] = NodeType.Enemy;
                    needEnemies--;
                }
            }

            if (detours != null && needMultipliers + needAdditives > 0)
            {
                return false;
            }

            foreach (var nodeId in random.Shuffled(gates))
            {
                if (role[nodeId] != NodeType.Unassigned)
                {
                    continue;
                }

                if (needEnemies > 0 && random.NextDouble() < tuning.GatePreference)
                {
                    role[nodeId] = NodeType.Enemy;
                    needEnemies--;
                }
            }

            var leftover = new List<int>();
            foreach (var nodeId in rest)
            {
                if (role[nodeId] == NodeType.Unassigned)
                {
                    leftover.Add(nodeId);
                }
            }

            var pool = new List<NodeType>();
            foreach (var nodeId in random.Shuffled(leftover))
            {
                pool.Clear();
                for (var index = 0; index < needEnemies; index++)
                {
                    pool.Add(NodeType.Enemy);
                }

                for (var index = 0; index < needMultipliers; index++)
                {
                    pool.Add(NodeType.Multiplier);
                }

                for (var index = 0; index < needAdditives; index++)
                {
                    pool.Add(NodeType.Additive);
                }

                if (pool.Count == 0)
                {
                    break;
                }

                var picked = random.Pick(pool);
                role[nodeId] = picked;
                if (picked == NodeType.Enemy)
                {
                    needEnemies--;
                }
                else if (picked == NodeType.Multiplier)
                {
                    needMultipliers--;
                }
                else
                {
                    needAdditives--;
                }
            }

            if (needEnemies + needMultipliers + needAdditives > 0)
            {
                return false;
            }

            SpreadEnemiesAcrossRegions(layout, rest, role, detours);

            board.SetType(bossNodeId, NodeType.Boss);
            foreach (var nodeId in rest)
            {
                board.SetType(nodeId, role[nodeId]);
            }

            return true;
        }

        static void SpreadEnemiesAcrossRegions(
            MazeLayout layout, List<int> rest, NodeType[] role, Detours detours)
        {
            var regionIds = new List<int>();
            var enemiesByRegion = new Dictionary<int, List<int>>();
            var treasureByRegion = new Dictionary<int, List<int>>();

            foreach (var nodeId in rest)
            {
                var regionId = layout.Graph.RegionOf(nodeId);
                if (!enemiesByRegion.ContainsKey(regionId))
                {
                    regionIds.Add(regionId);
                    enemiesByRegion.Add(regionId, new List<int>());
                    treasureByRegion.Add(regionId, new List<int>());
                }

                if (role[nodeId] == NodeType.Enemy)
                {
                    enemiesByRegion[regionId].Add(nodeId);
                }
                else
                {
                    treasureByRegion[regionId].Add(nodeId);
                }
            }

            regionIds.Sort();

            foreach (var regionId in regionIds)
            {
                if (enemiesByRegion[regionId].Count > 0 || treasureByRegion[regionId].Count == 0)
                {
                    continue;
                }

                var donor = -1;
                var spared = -1;
                foreach (var other in regionIds)
                {
                    if (enemiesByRegion[other].Count <= 1
                        || (donor >= 0 && enemiesByRegion[other].Count <= enemiesByRegion[donor].Count))
                    {
                        continue;
                    }

                    var candidate = SparedEnemyIn(enemiesByRegion[other], detours);
                    if (candidate < 0)
                    {
                        continue;
                    }

                    donor = other;
                    spared = candidate;
                }

                if (donor < 0)
                {
                    continue;
                }

                var starved = treasureByRegion[regionId][0];

                role[spared] = role[starved];
                role[starved] = NodeType.Enemy;

                enemiesByRegion[donor].Remove(spared);
                treasureByRegion[donor].Add(spared);
                enemiesByRegion[regionId].Add(starved);
                treasureByRegion[regionId].RemoveAt(0);
            }
        }

        static int SparedEnemyIn(List<int> enemies, Detours detours)
        {
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                if (detours == null || detours.Holds(enemies[index]))
                {
                    return enemies[index];
                }
            }

            return -1;
        }

        static ContentRejection Mint(ContentBoard board, PowerTuning tuning, StageRandom random)
        {
            var content = new List<int>();
            var multipliers = new List<int>();
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (!board.IsContent(nodeId))
                {
                    continue;
                }

                content.Add(nodeId);
                if (board.TypeOf(nodeId) == NodeType.Multiplier)
                {
                    multipliers.Add(nodeId);
                }
            }

            var ladder = random.Shuffled(PowerTuning.MultiplierLadder);
            var product = 1.0;
            for (var index = 0; index < multipliers.Count; index++)
            {
                var value = ladder[index % ladder.Count];
                board.SetValue(multipliers[index], value);
                product *= value;
            }

            if (MultiplierProduct.Of(board) > tuning.MultiplierProductCap)
            {
                return ContentRejection.MultiplierProductBeyondCap;
            }

            var steps = Math.Max(1, content.Count - multipliers.Count);
            var ratio = Math.Pow(
                Math.Max(1.01, tuning.StripTarget / (double)tuning.StartingPower / product), 1.0 / steps);

            var consumed = new bool[board.Count];
            var curve = (double)tuning.StartingPower;
            var power = tuning.StartingPower;
            var arrivalByRegion = new Dictionary<int, int>();
            var regionsWithAMintedEnemy = new HashSet<int>();
            var walked = new List<int>(content.Count);
            var powerAfter = new List<int>(content.Count);
            var uncapped = new int[board.Count];

            for (var step = 0; step < content.Count; step++)
            {
                var reachable = board.ReachableFrom(consumed);
                foreach (var nodeId in reachable)
                {
                    var region = board.RegionOf(nodeId);
                    if (!arrivalByRegion.ContainsKey(region))
                    {
                        arrivalByRegion.Add(region, power);
                    }
                }

                var take = PoorWalk.Next(board, reachable, consumed, power);
                if (take < 0)
                {
                    return ContentRejection.AdversaryStalled;
                }

                var type = board.TypeOf(take);
                curve *= type == NodeType.Multiplier ? board.ValueOf(take) : ratio;
                var goal = Math.Max(power + 1, curve);

                if (!board.IsMinted(take))
                {
                    var jitter = 1.0 + (random.NextDouble() * 2.0 - 1.0) * tuning.Jitter;
                    var minted = Math.Max(1, (int)Math.Floor((goal - power) * jitter + 0.5));
                    if (type == NodeType.Enemy)
                    {
                        uncapped[take] = minted;
                        minted = Math.Min(minted, Math.Max(1, (int)(power * tuning.EnemyCap)));
                        var region = board.RegionOf(take);
                        if (regionsWithAMintedEnemy.Add(region))
                        {
                            int arrival;
                            if (!arrivalByRegion.TryGetValue(region, out arrival))
                            {
                                arrival = power;
                            }

                            minted = Math.Min(minted, Math.Max(1, arrival - 1));
                        }
                    }

                    board.SetValue(take, minted);
                }

                var value = board.ValueOf(take);
                if (type == NodeType.Enemy && power <= value)
                {
                    return ContentRejection.UnaffordableEnemy;
                }

                power = board.PowerAfter(power, take);
                consumed[take] = true;
                walked.Add(take);
                powerAfter.Add(power);
            }

            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.IsContent(nodeId) && !board.IsMinted(nodeId))
                {
                    return ContentRejection.ValueNeverMinted;
                }
            }

            MintOffTheSpine(board, tuning, random, walked, powerAfter, uncapped);
            return ContentRejection.None;
        }

        static void MintOffTheSpine(
            ContentBoard board,
            PowerTuning tuning,
            StageRandom random,
            List<int> walked,
            List<int> powerAfter,
            int[] uncapped)
        {
            var offSpine = new List<int>();
            for (var index = SpineLength(powerAfter, BossNumber(board, tuning)); index < walked.Count; index++)
            {
                if (board.TypeOf(walked[index]) == NodeType.Enemy)
                {
                    offSpine.Add(walked[index]);
                }
            }

            var rich = (int)Math.Floor(tuning.EliteFraction * offSpine.Count + 0.5);
            if (rich <= 0)
            {
                return;
            }

            var wallet = new Dictionary<int, int>();
            foreach (var regionId in board.RegionIds)
            {
                wallet.Add(regionId, EnvelopeWalks.RichestEntry(board, tuning, regionId));
            }

            var order = random.Shuffled(offSpine);
            for (var index = 0; index < rich; index++)
            {
                var nodeId = order[index];
                var minted = Math.Max(
                    uncapped[nodeId],
                    Math.Max(1, (int)(wallet[board.RegionOf(nodeId)] * PowerTuning.EliteShare)));

                if (minted > board.ValueOf(nodeId))
                {
                    board.SetValue(nodeId, minted);
                }
            }
        }

        static int SpineLength(List<int> powerAfter, int bossPower)
        {
            for (var index = 0; index < powerAfter.Count; index++)
            {
                if (powerAfter[index] > bossPower)
                {
                    return index + 1;
                }
            }

            return powerAfter.Count;
        }

        static int BossNumber(ContentBoard board, PowerTuning tuning)
        {
            return Math.Max(2, (int)Math.Floor(PowerBound.Of(board, tuning) * tuning.BossFactor + 0.5));
        }

        static int RepairRegionFloor(ContentBoard board, PowerTuning tuning)
        {
            for (var pass = 0; pass < PowerTuning.FloorRepairPasses; pass++)
            {
                var touched = 0;
                foreach (var regionId in board.RegionIds)
                {
                    var cheapest = -1;
                    for (var nodeId = 0; nodeId < board.Count; nodeId++)
                    {
                        if (board.TypeOf(nodeId) != NodeType.Enemy || board.RegionOf(nodeId) != regionId)
                        {
                            continue;
                        }

                        if (cheapest < 0 || board.ValueOf(nodeId) < board.ValueOf(cheapest))
                        {
                            cheapest = nodeId;
                        }
                    }

                    if (cheapest < 0)
                    {
                        continue;
                    }

                    var floor = EnvelopeWalks.CheapestUnlock(board, tuning, regionId);
                    if (board.ValueOf(cheapest) <= floor)
                    {
                        continue;
                    }

                    board.SetValue(cheapest, Math.Max(1, floor - 1));
                    touched++;
                }

                if (touched == 0)
                {
                    return pass;
                }
            }

            return PowerTuning.FloorRepairPasses;
        }
    }
}
