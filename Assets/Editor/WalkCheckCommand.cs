using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class WalkCheckCommand
    {
        const long Seed = 20250824L;

        const float Frame = 1f / 60f;

        const int Moves = 6;

        const int FrameCap = 4000;

        const int SettleCap = 400;

        const string TrailPath = "dev/scratch/t-14-walk-trail.png";

        const string BeatPath = "dev/scratch/t-14-walk-beat.png";

        const string CancelPath = "dev/scratch/t-14-walk-cancel.png";

        public static void Check()
        {
            Wipe(TrailPath);
            Wipe(BeatPath);
            Wipe(CancelPath);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            PreviewFilm.Sun();

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);

            var arrivals = new List<ActionResult>();
            walker.Arrived += arrivals.Add;

            var report = new StringBuilder("walk on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(Walk.StepsPerSecond.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" tiles a second, ")
                .Append(Trail.DotsPerStep.ToString(CultureInfo.InvariantCulture))
                .Append(" dots a step:");

            report.Append(Cancelled(rig, lens, builder, input, walker));

            var beats = 0;
            var hops = 0;
            var behindAnEnemy = 0;
            var shotTrail = false;

            for (var move = 0; move < Moves && !walker.Run.IsLevelComplete; move++)
            {
                var target = Furthest(walker.Run);
                if (target == TapAim.Nothing)
                {
                    break;
                }

                var before = walker.Run;
                var predicted = ActionResolver.Along(before, NavigationMap.Of(before).RouteTo(target));
                var halted = Halted(predicted);
                var expected = new List<int>();
                for (var step = 1; step < predicted.Route.Count; step++)
                {
                    expected.Add(predicted.Route[step]);

                    if (predicted.Route[step] == halted)
                    {
                        break;
                    }
                }

                if (!before.IsReachable(target))
                {
                    behindAnEnemy++;
                }

                arrivals.Clear();
                walker.WalkTo(target);

                if (!walker.IsWalking)
                {
                    Debug.LogError("A legal tap on node " + target + " started no walk.");
                    break;
                }

                hops += expected.Count;

                if (!shotTrail && expected.Count > 1)
                {
                    shotTrail = true;
                    Run(rig, builder, walker, 6);
                    report.Append(Photograph(lens, TrailPath, "trail", builder, walker));
                }

                var held = Ran(rig, builder, walker, lens);
                var eaten = MultipliersEaten(before, walker.Run);
                beats += held;

                if (held != eaten)
                {
                    Debug.LogError(
                        "The walk held " + held + " beats over a move that spent " + eaten
                        + " multipliers. A beat marks a multiplier being consumed, not one being walked over.");
                }

                report.Append(Row(move, before, walker.Run, expected.Count, held));
                report.Append(Landed(walker.Run, predicted, arrivals, expected));

                NoWallIsCrossed(graph, predicted.Route);
                TheFigureStandsOnItsNode(builder, walker.Run);
                TheTrailIsPutAway(builder);
                TheTapIsFreeAgain(input);

                if (walker.Run.ConsumedNodes.Count == before.ConsumedNodes.Count)
                {
                    report.Append(", and the greedy line has nothing left it can eat");
                    break;
                }
            }

            report.Append("\n  ")
                .Append(hops.ToString(CultureInfo.InvariantCulture))
                .Append(" nodes resolved over ")
                .Append(Moves.ToString(CultureInfo.InvariantCulture))
                .Append(" moves, ")
                .Append(beats.ToString(CultureInfo.InvariantCulture))
                .Append(" zoom beats held and released, ")
                .Append(behindAnEnemy.ToString(CultureInfo.InvariantCulture))
                .Append(" of the moves aimed past an unbeaten enemy");

            if (hops == 0)
            {
                Debug.LogError("The check needs at least one walk, and made none.");
            }

            if (behindAnEnemy == 0)
            {
                Debug.LogWarning(
                    "The greedy line never chose a target behind an unbeaten enemy on this seed.");
            }

            if (!shotTrail)
            {
                Debug.LogWarning("No multi-hop walk came up to photograph the trail on.");
            }

            if (beats == 0)
            {
                Debug.LogWarning("No multiplier was consumed, so no zoom beat was exercised.");
            }

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            report.Append(Disengaged(graph));

            Debug.Log(report.ToString());
        }

        static string Disengaged(LevelGraph graph)
        {
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);

            var told = BrokenOffMidFight(rig, lens, builder, input, walker)
                + BrokenOffMidWalk(rig, lens, builder, input, walker)
                + Hammered(rig, lens, builder, input, walker);

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return told;
        }

        static string Cancelled(
            CameraRig rig, Camera lens, WorldBuilder builder, TapInput input, Walker walker)
        {
            var target = Furthest(walker.Run);
            if (target == TapAim.Nothing)
            {
                return "\n  nothing was reachable to cancel a walk to";
            }

            var untouched = walker.Run;
            var arrivals = 0;
            Action<ActionResult> count = result => arrivals++;
            walker.Arrived += count;

            walker.WalkTo(target);
            Run(rig, builder, walker, 4);

            var abandoned = walker.Walk.Position;
            PreviewFilm.Shoot(lens, CancelPath);
            walker.Cancel();

            var held = Ran(rig, builder, walker, lens);
            walker.Arrived -= count;

            if (held != 0)
            {
                Debug.LogError("A cancelled walk held " + held + " beats on its way back.");
            }

            if (arrivals != 0)
            {
                Debug.LogError("A cancelled walk resolved " + arrivals + " nodes on its way back.");
            }

            if (!ReferenceEquals(walker.Run, untouched))
            {
                Debug.LogError("A cancelled walk replaced the run state rather than leaving it alone.");
            }

            if (!walker.Run.Equals(untouched))
            {
                Debug.LogError("A cancelled walk changed the run state.");
            }

            TheFigureStandsOnItsNode(builder, walker.Run);
            TheTrailIsPutAway(builder);
            TheTapIsFreeAgain(input);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  cancelled a walk to node {0} at {1}, fell back to node {2} at power {3} with nothing consumed",
                target,
                abandoned,
                walker.Run.PositionNodeId,
                walker.Run.Power);
        }

        static string BrokenOffMidWalk(
            CameraRig rig, Camera lens, WorldBuilder builder, TapInput input, Walker walker)
        {
            var first = Furthest(walker.Run, TapAim.Nothing);
            if (first == TapAim.Nothing)
            {
                return "\n  nothing was reachable to break a walk off";
            }

            walker.WalkTo(first);
            Run(rig, builder, walker, 5);

            if (!walker.IsWalking)
            {
                Debug.LogError("The walk to node " + first + " was over before a tap could break it off.");
                return "\n  the walk ended before a second tap could break it off";
            }

            var breakingOff = walker.Run;
            var second = Furthest(breakingOff, first);

            if (second == TapAim.Nothing)
            {
                Debug.LogError("The check needs a second node to break a walk off toward, and found none.");
                return "\n  nothing else was reachable to break the walk off toward";
            }

            var mid = walker.Walk.Position;
            RunState brokenOff = null;
            Action<RunState> once = settled => brokenOff = brokenOff ?? settled;
            walker.Finished += once;

            walker.WalkTo(second);

            if (!walker.IsWalking)
            {
                Debug.LogError("A tap on node " + second + " mid-walk stopped the walker dead.");
            }

            var predicted = ActionResolver.Along(
                breakingOff, NavigationMap.Of(breakingOff).RouteTo(second)).State;

            Ran(rig, builder, walker, lens);
            walker.Finished -= once;

            BrokeOffWhereItStood(brokenOff, breakingOff, "walk");
            LandedWhereBreakingOffPredicts(walker, breakingOff, second, predicted);

            TheFigureStandsOnItsNode(builder, walker.Run);
            TheTrailIsPutAway(builder);
            TheTapIsFreeAgain(input);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  a tap on node {0} at {1}, partway to node {2}, broke the walk off and carried the "
                + "walker on to node {3} at power {4}",
                second,
                mid,
                first,
                walker.Run.PositionNodeId,
                walker.Run.Power);
        }

        static string BrokenOffMidFight(
            CameraRig rig, Camera lens, WorldBuilder builder, TapInput input, Walker walker)
        {
            var target = TapAim.Nothing;
            var enemy = TapAim.Nothing;
            Guarded(walker.Run, ref target, ref enemy);

            if (target == TapAim.Nothing)
            {
                Debug.LogWarning("No target behind an unbeaten enemy came up to break a fight off on.");
                return "\n  no fight came up to break off";
            }

            var worth = walker.Run.Level.Decisions.Node(enemy).Value;
            walker.WalkTo(target);

            var joined = false;
            for (var frame = 0; frame < FrameCap && walker.IsWalking && !joined; frame++)
            {
                Step(rig, builder, walker);
                joined = walker.Walk.IsWaiting && walker.Walk.ArrivedNodeId == enemy;
            }

            if (!joined)
            {
                Debug.LogError("The walk to node " + target + " never joined the fight on node " + enemy + ".");
                return "\n  the walk to node " + target + " never met the enemy on node " + enemy;
            }

            var breakingOff = walker.Run;
            var beaten = breakingOff.IsConsumed(enemy);
            var away = Furthest(breakingOff, target);

            if (away == TapAim.Nothing)
            {
                Debug.LogError("The check needs somewhere to break a fight off toward, and found none.");
                return "\n  nothing was reachable to break the fight off toward";
            }

            RunState brokenOff = null;
            Action<RunState> once = settled => brokenOff = brokenOff ?? settled;
            walker.Finished += once;

            walker.WalkTo(away);

            var predicted = ActionResolver.Along(
                breakingOff, NavigationMap.Of(breakingOff).RouteTo(away)).State;

            Ran(rig, builder, walker, lens);
            walker.Finished -= once;

            BrokeOffWhereItStood(brokenOff, breakingOff, "fight on node " + enemy);
            LandedWhereBreakingOffPredicts(walker, breakingOff, away, predicted);

            if (brokenOff != null && !beaten)
            {
                if (brokenOff.IsConsumed(enemy))
                {
                    Debug.LogError(
                        "The fight on node " + enemy + " was broken off and the enemy fell anyway.");
                }

                if (brokenOff.Level.Decisions.Node(enemy).Value != worth)
                {
                    Debug.LogError(
                        "The enemy on node " + enemy + " was worth " + worth + " and is worth "
                        + brokenOff.Level.Decisions.Node(enemy).Value
                        + " after the fight on it was broken off.");
                }

                if (!new HashSet<int>(TapAim.Aimable(brokenOff)).Contains(enemy))
                {
                    Debug.LogError(
                        "The enemy on node " + enemy + " is no longer offered to the finger after "
                        + "the fight on it was broken off, so it cannot be fought again.");
                }
            }

            TheFigureStandsOnItsNode(builder, walker.Run);
            TheTrailIsPutAway(builder);
            TheTapIsFreeAgain(input);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  a tap on node {0} during the {1} fight on node {2} broke it off to node {3} at "
                + "power {4}, leaving the enemy {5}",
                away,
                beaten ? "won" : "lost",
                enemy,
                walker.Run.PositionNodeId,
                walker.Run.Power,
                walker.Run.IsConsumed(enemy) ? "fallen" : "standing at " + worth);
        }

        static string Hammered(
            CameraRig rig, Camera lens, WorldBuilder builder, TapInput input, Walker walker)
        {
            var taps = 0;
            var settlings = 0;
            var strandings = 0;

            Action<RunState> watch = settled =>
            {
                settlings++;
                strandings += StandsOffItsNode(builder, settled) ? 1 : 0;
            };

            walker.Finished += watch;

            for (var round = 0; round < 40; round++)
            {
                var target = Furthest(walker.Run, TapAim.Nothing);
                if (target == TapAim.Nothing)
                {
                    break;
                }

                walker.WalkTo(target);
                taps++;
                Run(rig, builder, walker, 3);
            }

            Ran(rig, builder, walker, lens);
            walker.Finished -= watch;

            if (taps < 20)
            {
                Debug.LogError("The hammering leg landed " + taps + " taps and needs at least twenty.");
            }

            if (settlings < 5)
            {
                Debug.LogError(
                    "The hammering leg watched " + settlings
                    + " walks settle and proves nothing about where they stopped.");
            }

            if (strandings != 0)
            {
                Debug.LogError(
                    "Tap after tap left the walker off the node the run stands on " + strandings + " times.");
            }

            TheFigureStandsOnItsNode(builder, walker.Run);
            TheTrailIsPutAway(builder);
            TheTapIsFreeAgain(input);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0} taps three frames apart never stranded the walker off its node, and left it on "
                + "node {1} at power {2}",
                taps,
                walker.Run.PositionNodeId,
                walker.Run.Power);
        }

        static void BrokeOffWhereItStood(RunState brokenOff, RunState breakingOff, string leg)
        {
            if (brokenOff == null)
            {
                Debug.LogError("Breaking off the " + leg + " never settled the walk it broke off.");
                return;
            }

            if (!brokenOff.Equals(breakingOff))
            {
                Debug.LogError(
                    "Breaking off the " + leg + " settled on node " + brokenOff.PositionNodeId
                    + " at power " + brokenOff.Power + " with " + brokenOff.ConsumedNodes.Count
                    + " nodes taken, where it broke off on node " + breakingOff.PositionNodeId
                    + " at power " + breakingOff.Power + " with " + breakingOff.ConsumedNodes.Count
                    + " taken. A tap keeps what the walker was already holding.");
            }
        }

        static void LandedWhereBreakingOffPredicts(
            Walker walker, RunState breakingOff, int target, RunState predicted)
        {
            if (walker.IsWalking)
            {
                Debug.LogError("A walk broken off toward node " + target + " never settled.");
                return;
            }

            if (!walker.Run.Equals(predicted))
            {
                Debug.LogError(
                    "Breaking off toward node " + target + " ended on node " + walker.Run.PositionNodeId
                    + " at power " + walker.Run.Power + " where setting out from node "
                    + breakingOff.PositionNodeId + " at power " + breakingOff.Power + " gives node "
                    + predicted.PositionNodeId + " at power " + predicted.Power + ".");
            }
        }

        static void Guarded(RunState state, ref int target, ref int enemy)
        {
            var navigation = NavigationMap.Of(state);

            foreach (var nodeId in TapAim.Aimable(navigation))
            {
                var route = navigation.RouteTo(nodeId);
                var resolved = ActionResolver.Along(state, route);

                if (resolved.Outcome == ActionOutcome.Rejected)
                {
                    continue;
                }

                var halted = Halted(resolved);
                if (halted != TapAim.Nothing)
                {
                    target = nodeId;
                    enemy = halted;
                    return;
                }

                if (target != TapAim.Nothing || navigation.FightsOnTheWayTo(nodeId) < 1)
                {
                    continue;
                }

                foreach (var step in route)
                {
                    if (step == state.PositionNodeId || !state.BlocksPassage(step))
                    {
                        continue;
                    }

                    target = nodeId;
                    enemy = step;
                    break;
                }
            }
        }

        static bool StandsOffItsNode(WorldBuilder builder, RunState state)
        {
            if (builder.Player == null)
            {
                return false;
            }

            var expected = IsoProjection.Of(state.Level.Decisions.Node(state.PositionNodeId).Position);
            var standing = builder.Player.Ground;

            return Mathf.Abs(standing.X - expected.X) > 0.001f
                || Mathf.Abs(standing.Y - expected.Y) > 0.001f
                || Mathf.Abs(standing.Z - expected.Z) > 0.001f;
        }

        static int Ran(CameraRig rig, WorldBuilder builder, Walker walker, Camera lens)
        {
            var beats = 0;
            var frames = 0;
            var cutAway = false;
            var holding = false;

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);

                if (walker.IsHolding)
                {
                    if (!holding)
                    {
                        holding = true;
                        beats++;
                        frames = 0;
                        cutAway = false;

                        if (!File.Exists(BeatPath))
                        {
                            PreviewFilm.Shoot(lens, BeatPath);
                        }
                    }

                    frames++;
                    cutAway |= !rig.Framing.Equals(rig.Following);
                    continue;
                }

                if (holding)
                {
                    holding = false;
                    CloseTheBeat(frames, cutAway);
                }
            }

            if (holding)
            {
                CloseTheBeat(frames, cutAway);
            }

            if (walker.IsWalking)
            {
                Debug.LogError("A walk was still going after " + FrameCap + " frames.");
            }

            var standing = LevelFraming.Play(
                IsoProjection.Of(walker.Run.Level.Decisions.Node(walker.Run.PositionNodeId).Position));

            for (var frame = 0; frame < SettleCap && !rig.Framing.Equals(standing); frame++)
            {
                Step(rig, builder, walker);
            }

            if (!rig.Framing.Equals(standing))
            {
                Debug.LogError(
                    "The camera came out of the walk framing " + rig.Framing
                    + " rather than following the player at " + standing + ".");
            }

            return beats;
        }

        static void CloseTheBeat(int frames, bool cutAway)
        {
            if (!cutAway)
            {
                Debug.LogError("The walk held for a beat the camera never cut away for.");
            }

            if (frames * Frame < ZoomBeat.FloorSeconds)
            {
                Debug.LogError(
                    "A beat held for " + (frames * Frame) + "s, under the " + ZoomBeat.FloorSeconds
                    + "s floor a cut away from the follow is worth.");
            }
        }

        static int MultipliersEaten(RunState before, RunState after)
        {
            var eaten = 0;

            foreach (var nodeId in after.ConsumedNodes)
            {
                if (!before.IsConsumed(nodeId)
                    && after.Level.Decisions.Node(nodeId).Type == NodeType.Multiplier)
                {
                    eaten++;
                }
            }

            return eaten;
        }

        static void Run(CameraRig rig, WorldBuilder builder, Walker walker, int frames)
        {
            for (var frame = 0; frame < frames && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);
            }
        }

        static void Step(CameraRig rig, WorldBuilder builder, Walker walker)
        {
            walker.Advance(Frame);
            rig.Advance(Frame);
            builder.Floor.Advance(Frame);
            builder.Pickups.Advance(Frame);

            if (builder.PlayerBadge != null)
            {
                builder.PlayerBadge.Advance(Frame);
            }
        }

        static int Halted(ActionResult predicted)
        {
            if (predicted.Outcome != ActionOutcome.Tie && predicted.Outcome != ActionOutcome.Loss)
            {
                return TapAim.Nothing;
            }

            for (var step = 0; step < predicted.Route.Count - 1; step++)
            {
                if (predicted.Route[step] == predicted.State.PositionNodeId)
                {
                    return predicted.Route[step + 1];
                }
            }

            return TapAim.Nothing;
        }

        static int Furthest(RunState state)
        {
            return Furthest(state, TapAim.Nothing);
        }

        static int Furthest(RunState state, int excluded)
        {
            var navigation = NavigationMap.Of(state);
            var furthest = TapAim.Nothing;
            var doomed = TapAim.Nothing;
            var steps = 0;
            var doomedSteps = 0;

            foreach (var nodeId in TapAim.Aimable(navigation))
            {
                if (nodeId == excluded)
                {
                    continue;
                }

                var resolved = ActionResolver.Along(state, navigation.RouteTo(nodeId));
                if (resolved.Outcome == ActionOutcome.Rejected)
                {
                    continue;
                }

                if (resolved.State.ConsumedNodes.Count == state.ConsumedNodes.Count)
                {
                    if (resolved.Route.Count > doomedSteps)
                    {
                        doomed = nodeId;
                        doomedSteps = resolved.Route.Count;
                    }

                    continue;
                }

                if (resolved.Route.Count > steps)
                {
                    furthest = nodeId;
                    steps = resolved.Route.Count;
                }
            }

            return furthest != TapAim.Nothing ? furthest : doomed;
        }

        static void NoWallIsCrossed(LevelGraph graph, IReadOnlyList<int> route)
        {
            var tiles = TileRoute.Of(graph, route).Tiles;

            for (var step = 1; step < tiles.Count; step++)
            {
                if (!graph.Tiles.AreAdjacent(tiles[step - 1], tiles[step]))
                {
                    Debug.LogError(
                        "The walk crossed a wall between " + tiles[step - 1] + " and " + tiles[step] + ".");
                }
            }
        }

        static void TheFigureStandsOnItsNode(WorldBuilder builder, RunState state)
        {
            if (builder.Player == null)
            {
                return;
            }

            var expected = IsoProjection.Of(state.Level.Decisions.Node(state.PositionNodeId).Position);
            var standing = builder.Player.Ground;

            if (Mathf.Abs(standing.X - expected.X) > 0.001f
                || Mathf.Abs(standing.Y - expected.Y) > 0.001f
                || Mathf.Abs(standing.Z - expected.Z) > 0.001f)
            {
                Debug.LogError(
                    "The run stands on node " + state.PositionNodeId + " at " + expected
                    + " while the figure stands at " + standing + ".");
            }
        }

        static void TheTrailIsPutAway(WorldBuilder builder)
        {
            if (builder.Trail.Showing != 0)
            {
                Debug.LogError(
                    "The walk ended with " + builder.Trail.Showing + " trail dots still lit.");
            }
        }

        static void TheTapIsFreeAgain(TapInput input)
        {
            if (input.IsLocked)
            {
                Debug.LogError("The walk ended without handing input back.");
            }
        }

        static string Landed(
            RunState state,
            ActionResult predicted,
            IReadOnlyList<ActionResult> arrivals,
            IReadOnlyList<int> expected)
        {
            if (arrivals.Count != expected.Count)
            {
                Debug.LogError(
                    "The walk resolved " + arrivals.Count + " nodes where its route passes " + expected.Count + ".");
            }

            for (var step = 0; step < arrivals.Count && step < expected.Count; step++)
            {
                if (arrivals[step].State.PositionNodeId != expected[step]
                    && arrivals[step].Outcome != ActionOutcome.Tie
                    && arrivals[step].Outcome != ActionOutcome.Loss)
                {
                    Debug.LogError(
                        "Step " + step + " of the walk resolved node "
                        + arrivals[step].State.PositionNodeId + " where the route passes " + expected[step] + ".");
                }
            }

            if (!state.Equals(predicted.State))
            {
                Debug.LogError(
                    "The walk arrived at power " + state.Power + " on node " + state.PositionNodeId
                    + " where the resolver predicted power " + predicted.State.Power
                    + " on node " + predicted.State.PositionNodeId + ".");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                " ({0} resolved, ending {1})",
                arrivals.Count,
                predicted.Outcome);
        }

        static string Photograph(Camera lens, string path, string leg, WorldBuilder builder, Walker walker)
        {
            PreviewFilm.Shoot(lens, path);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  photographed the {0} {1} tiles into a walk over {2} steps, {3} dots still ahead",
                leg,
                walker.Walk.Travelled,
                walker.Walk.Route.Steps,
                builder.Trail.Showing);
        }

        static string Row(int move, RunState before, RunState after, int nodes, int held)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  move {0}: node {1} to node {2}, power {3} to {4} over {5} nodes{6}",
                move,
                before.PositionNodeId,
                after.PositionNodeId,
                before.Power,
                after.Power,
                nodes,
                held == 0
                    ? string.Empty
                    : held == 1
                        ? ", held for a beat"
                        : ", held for " + held.ToString(CultureInfo.InvariantCulture) + " beats");
        }

        static void Wipe(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
