using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class ContentPlacer
    {
        public static PlacedLevel Place(MazeLayout layout, ContentRecipe recipe, PowerTuning tuning)
        {
            PlacedLevel placed;
            ContentRejection rejection;
            if (TryPlace(layout, recipe, tuning, out placed, out rejection))
            {
                return placed;
            }

            throw new InvalidOperationException(
                "Seed " + layout.AttemptSeed + " could not be filled: " + rejection + ".");
        }

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

            var bossNodeId = DeepestSlot(layout);
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

            long additiveTotal = 0;
            long multiplierProduct = 1;
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (nodeId == bossNodeId)
                {
                    continue;
                }

                var type = board.TypeOf(nodeId);
                if (type == NodeType.Multiplier)
                {
                    multiplierProduct *= board.ValueOf(nodeId);
                }
                else if (type == NodeType.Enemy || type == NodeType.Additive)
                {
                    additiveTotal += board.ValueOf(nodeId);
                }
            }

            var bound = tuning.StartingPower * multiplierProduct + additiveTotal;
            var bossPower = Math.Max(2, (int)Math.Floor(bound * tuning.BossFactor + 0.5));
            board.SetValue(bossNodeId, bossPower);

            if (bossPower >= bound)
            {
                rejection = ContentRejection.BossBeyondBound;
                return false;
            }

            if (!NothingIsGatedBehindTheBoss(board, bossNodeId))
            {
                rejection = ContentRejection.GatedBehindBoss;
                return false;
            }

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

            bool blocked;
            var shortestPathPower = EnvelopeWalks.ShortestPathPower(board, tuning, bossNodeId, out blocked);
            if (bossPower <= shortestPathPower)
            {
                rejection = ContentRejection.BossWithinReach;
                return false;
            }

            placed = new PlacedLevel(
                layout,
                board.Rebuild(),
                recipe,
                tuning,
                bossNodeId,
                bound,
                shortestPathPower,
                blocked,
                passes,
                envelope);
            return true;
        }

        static int DeepestSlot(MazeLayout layout)
        {
            var deepest = -1;
            var deepestDistance = -1;
            foreach (var slotId in layout.SlotNodeIds)
            {
                var distance = layout.DistanceFromStart.DistanceTo(
                    layout.Graph.Decisions.Node(slotId).Position);
                if (distance <= deepestDistance)
                {
                    continue;
                }

                deepest = slotId;
                deepestDistance = distance;
            }

            return deepest;
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
                if (random.NextDouble() < tuning.PocketTreasure && (needMultipliers > 0 || needAdditives > 0))
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

            foreach (var nodeId in random.Shuffled(gates))
            {
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

            SpreadEnemiesAcrossRegions(layout, rest, role);

            board.SetType(bossNodeId, NodeType.Boss);
            foreach (var nodeId in rest)
            {
                board.SetType(nodeId, role[nodeId]);
            }

            return true;
        }

        static void SpreadEnemiesAcrossRegions(MazeLayout layout, List<int> rest, NodeType[] role)
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
                foreach (var other in regionIds)
                {
                    if (enemiesByRegion[other].Count > 1
                        && (donor < 0 || enemiesByRegion[other].Count > enemiesByRegion[donor].Count))
                    {
                        donor = other;
                    }
                }

                if (donor < 0)
                {
                    continue;
                }

                var spared = enemiesByRegion[donor][enemiesByRegion[donor].Count - 1];
                var starved = treasureByRegion[regionId][0];

                role[spared] = role[starved];
                role[starved] = NodeType.Enemy;

                enemiesByRegion[donor].RemoveAt(enemiesByRegion[donor].Count - 1);
                treasureByRegion[donor].Add(spared);
                enemiesByRegion[regionId].Add(starved);
                treasureByRegion[regionId].RemoveAt(0);
            }
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

            var steps = Math.Max(1, content.Count - multipliers.Count);
            var ratio = Math.Pow(
                Math.Max(1.01, tuning.StripTarget / (double)tuning.StartingPower / product), 1.0 / steps);

            var consumed = new bool[board.Count];
            var curve = (double)tuning.StartingPower;
            var power = tuning.StartingPower;
            var arrivalByRegion = new Dictionary<int, int>();
            var regionsWithAMintedEnemy = new HashSet<int>();

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

                var take = ChooseWorst(board, reachable, consumed, power);
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

                power = type == NodeType.Multiplier ? power * value : power + value;
                consumed[take] = true;
            }

            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.IsContent(nodeId) && !board.IsMinted(nodeId))
                {
                    return ContentRejection.ValueNeverMinted;
                }
            }

            return ContentRejection.None;
        }

        static int ChooseWorst(ContentBoard board, List<int> reachable, bool[] consumed, int power)
        {
            var multiplier = -1;
            var additive = -1;
            var affordable = -1;
            var any = -1;

            foreach (var nodeId in reachable)
            {
                if (consumed[nodeId] || !board.IsContent(nodeId))
                {
                    continue;
                }

                if (any < 0)
                {
                    any = nodeId;
                }

                var type = board.TypeOf(nodeId);
                if (type == NodeType.Multiplier && multiplier < 0)
                {
                    multiplier = nodeId;
                }

                if (type == NodeType.Additive && additive < 0)
                {
                    additive = nodeId;
                }

                if (type == NodeType.Enemy
                    && affordable < 0
                    && (!board.IsMinted(nodeId) || power > board.ValueOf(nodeId)))
                {
                    affordable = nodeId;
                }
            }

            if (multiplier >= 0)
            {
                return multiplier;
            }

            if (additive >= 0)
            {
                return additive;
            }

            return affordable >= 0 ? affordable : any;
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

        static bool NothingIsGatedBehindTheBoss(ContentBoard board, int bossNodeId)
        {
            var reachable = board.ReachableAround(bossNodeId);
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.IsContent(nodeId) && !reachable[nodeId])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
