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
    public static class TapInputCheckCommand
    {
        const long Seed = 20250824L;

        const int Moves = 4;

        const string IdlePath = "dev/scratch/t-13-tap-idle.png";

        const string WinPath = "dev/scratch/t-13-tap-win.png";

        const string LossPath = "dev/scratch/t-13-tap-loss.png";

        const string SafeTrailPath = "dev/scratch/t-134-trail-safe.png";

        const string DangerTrailPath = "dev/scratch/t-134-trail-danger.png";

        public static void Check()
        {
            Wipe(IdlePath);
            Wipe(WinPath);
            Wipe(LossPath);
            Wipe(SafeTrailPath);
            Wipe(DangerTrailPath);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            PreviewFilm.Sun();

            rig.Begin(graph);
            rig.Skip();

            var state = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var opening = state;
            var input = TapInput.Raise(rig, builder.Targets, state);
            var tapped = new List<TargetPreview>();
            input.Tapped += tapped.Add;

            var report = new StringBuilder("tap input on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(input.FrameWidth.ToString(CultureInfo.InvariantCulture))
                .Append('x')
                .Append(input.FrameHeight.ToString(CultureInfo.InvariantCulture))
                .Append(" at ")
                .Append(TouchTargets.DotsPerInchOr(Screen.dpi).ToString("0", CultureInfo.InvariantCulture))
                .Append(" dpi, reach ")
                .Append(input.Reach.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" px; targets measured on the reference device, ")
                .Append(ScreenFrame.Width.ToString(CultureInfo.InvariantCulture))
                .Append('x')
                .Append(ScreenFrame.Height.ToString(CultureInfo.InvariantCulture))
                .Append(" at ")
                .Append(TouchTargets.ReferenceDotsPerInch.ToString("0", CultureInfo.InvariantCulture))
                .Append(" dpi:");

            var previewed = 0;
            var multiHop = 0;
            var behindAnEnemy = 0;
            var shotWin = false;
            var shotLoss = false;

            for (var move = 0; move <= Moves && !state.IsLevelComplete; move++)
            {
                input.Show(state);
                Settle(builder.Floor);
                PreviewFilm.Shoot(lens, IdlePath);
                report.Append(Row(input, state, move));

                var stepped = TapAim.Nothing;
                var clearestWin = default(TapCandidate);
                var clearestLoss = default(TapCandidate);
                var winRoom = 0f;
                var lossRoom = 0f;
                var navigation = NavigationMap.Of(state);

                foreach (var candidate in input.Candidates())
                {
                    input.AimAt(candidate.Point);

                    var preview = input.Preview;
                    var resolved = ActionResolver.Along(state, navigation.RouteTo(candidate.NodeId));

                    if (preview.NodeId != candidate.NodeId)
                    {
                        Debug.LogError(
                            "A finger on node " + candidate.NodeId + " aimed at " + preview.NodeId + " instead.");
                        continue;
                    }

                    if (preview.Outcome != resolved.Outcome || preview.Power != resolved.State.Power)
                    {
                        Debug.LogError(
                            "Node " + candidate.NodeId + " previews " + preview.Outcome + " at power "
                            + preview.Power + " where the resolver gives " + resolved.Outcome + " at power "
                            + resolved.State.Power + ".");
                    }

                    WearsThePreview(builder.Targets, state, preview, candidate.NodeId);

                    previewed++;
                    if (preview.Route.Count > 2)
                    {
                        multiHop++;
                    }

                    var room = Room(input, state, candidate);
                    Clearest(ref clearestWin, ref winRoom, candidate, room, preview.Outcome == ActionOutcome.Win);
                    Clearest(ref clearestLoss, ref lossRoom, candidate, room, preview.Outcome == ActionOutcome.Loss);

                    if (stepped == TapAim.Nothing && preview.Outcome == ActionOutcome.Win)
                    {
                        stepped = candidate.NodeId;
                    }
                }

                if (!shotWin && winRoom >= 2f * input.Reach)
                {
                    shotWin = true;
                    report.Append(Photograph(input, rig, lens, WinPath, "win", builder.Targets, state, clearestWin));
                }

                if (!shotLoss && lossRoom >= 2f * input.Reach)
                {
                    shotLoss = true;
                    report.Append(Photograph(input, rig, lens, LossPath, "loss", builder.Targets, state, clearestLoss));
                }

                behindAnEnemy += TakenBehindAnEnemy(input, builder.Targets, state, navigation);

                if (stepped == TapAim.Nothing)
                {
                    break;
                }

                var before = tapped.Count;
                report.Append(SlidOnto(input, rig, state, stepped));

                if (tapped.Count != before + 1 || tapped[tapped.Count - 1].NodeId != stepped)
                {
                    Debug.LogError("Sliding onto node " + stepped + " and letting go did not commit that node.");
                }

                state = ActionResolver.Along(state, navigation.RouteTo(stepped)).State;
                builder.Floor.Show(state);
                builder.PlayerBadge.Show(state.Power);
            }

            report.Append(PanHeldAndGivenBack(input, rig, state));

            input.Show(opening);
            var walker = Walker.Raise(rig, builder, input, opening);
            report.Append(ReachesTheWalkerMidJourney(walker, rig, builder, input, opening));
            report.Append(TrailPreviewedOnAim(walker, rig, builder, input, lens, opening));

            report.Append("\n  ")
                .Append(previewed.ToString(CultureInfo.InvariantCulture))
                .Append(" previews agreed with the resolver, ")
                .Append(multiHop.ToString(CultureInfo.InvariantCulture))
                .Append(" of them multi-hop, ")
                .Append(tapped.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" taps committed, ")
                .Append(behindAnEnemy.ToString(CultureInfo.InvariantCulture))
                .Append(" of the offered targets standing behind an unbeaten enemy");

            if (previewed == 0 || multiHop == 0 || tapped.Count == 0)
            {
                Debug.LogError(
                    "The check needs previews, a multi-hop one and a committed tap, and got "
                    + previewed + ", " + multiHop + " and " + tapped.Count + ".");
            }

            if (behindAnEnemy == 0)
            {
                Debug.LogError(
                    "The check never met a target behind an unbeaten enemy, so it proved nothing "
                    + "about the navigation predicate.");
            }

            if (!shotWin || !shotLoss)
            {
                Debug.LogWarning("No " + (shotWin ? "loss" : "win") + " was ever previewed to photograph.");
            }

            Debug.Log(report.ToString());

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();
        }

        static string SlidOnto(TapInput input, CameraRig rig, RunState state, int nodeId)
        {
            var anchor = FingerOn(input, state, nodeId);
            var from = Nudged(anchor, -input.Reach * 0.9f);
            var framing = rig.Framing;

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: from);
            input.Reading(pressedNow: false, releasedNow: false, isPressed: true, hovers: false, finger: anchor);

            var aimed = input.Preview.NodeId;
            if (aimed != nodeId)
            {
                Debug.LogError(
                    "A press that began " + ScreenPoint.Distance(from, anchor)
                    + " px off node " + nodeId + " slid onto it and aimed at " + aimed + " instead.");
            }

            if (!rig.Framing.Equals(framing))
            {
                Debug.LogError(
                    "A slide shorter than the " + input.Reach + " px reach panned the camera from "
                    + framing + " to " + rig.Framing + ".");
            }

            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: anchor);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  a press {0:0.#} px off node {1} slid onto it, re-aimed and committed without panning",
                ScreenPoint.Distance(from, anchor),
                nodeId);
        }

        static string ReachesTheWalkerMidJourney(
            Walker walker, CameraRig rig, WorldBuilder builder, TapInput input, RunState opening)
        {
            walker.Begin(rig, builder, input, opening);
            input.Show(opening);

            var committed = new List<int>();
            Action<TargetPreview> note = preview => committed.Add(preview.NodeId);
            input.Tapped += note;

            var first = Longest(opening);
            if (first == TapAim.Nothing)
            {
                input.Tapped -= note;
                Debug.LogError("The check needs a journey to break off, and nothing was walkable to.");
                return "\n  nothing was walkable to, so no journey was there to break off";
            }

            walker.WalkTo(first);

            if (!walker.IsWalking)
            {
                input.Tapped -= note;
                Debug.LogError("A walk to node " + first + " never started, so no tap could break it off.");
                return "\n  no journey started to break off";
            }

            for (var frame = 0; frame < 12 && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);
            }

            if (!walker.IsWalking)
            {
                input.Tapped -= note;
                Debug.LogError("The walk to node " + first + " was over before a second tap could land.");
                return "\n  the journey ended before a second tap could land on it";
            }

            var breakingOff = walker.Run;
            var second = Roomiest(input, breakingOff, first);

            if (second == TapAim.Nothing)
            {
                input.Tapped -= note;
                Debug.LogError("The check needs a second node to break off toward, and found none.");
                return "\n  nothing else was reachable to break the journey off toward";
            }

            var finger = FingerOn(input, breakingOff, second);

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: finger);

            if (input.Preview.NodeId != second)
            {
                Debug.LogError(
                    "A finger on node " + second + " with a journey running aimed at " + input.Preview.NodeId
                    + " instead. Input does not reach the walker while it is walking.");
            }

            if (!input.Preview.IsLegal)
            {
                Debug.LogError(
                    "Node " + second + " previewed as illegal while a journey was running.");
            }

            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: finger);
            input.Tapped -= note;

            if (committed.Count != 1 || committed[0] != second)
            {
                Debug.LogError(
                    "A tap on node " + second + " mid-journey committed " + committed.Count
                    + " taps rather than that one node.");
            }

            var predicted = ActionResolver.Along(
                breakingOff, NavigationMap.Of(breakingOff).RouteTo(second)).State;

            for (var frame = 0; frame < 4000 && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);
            }

            if (walker.IsWalking)
            {
                Debug.LogError("A journey broken off by a tap was still running 4000 frames later.");
            }

            if (!walker.Run.Equals(predicted))
            {
                Debug.LogError(
                    "A tap on node " + second + " mid-journey ended on node " + walker.Run.PositionNodeId
                    + " at power " + walker.Run.Power + " where breaking off on node "
                    + breakingOff.PositionNodeId + " and walking there gives node "
                    + predicted.PositionNodeId + " at power " + predicted.Power + ".");
            }

            if (input.IsLocked)
            {
                Debug.LogError("A journey broken off by a tap ended without handing input back.");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  a press and release on node {0} partway through a walk to node {1} aimed, committed "
                + "and carried the walker off to node {2} at power {3}",
                second,
                first,
                walker.Run.PositionNodeId,
                walker.Run.Power);
        }

        static string TrailPreviewedOnAim(
            Walker walker,
            CameraRig rig,
            WorldBuilder builder,
            TapInput input,
            Camera lens,
            RunState opening)
        {
            walker.Begin(rig, builder, input, opening);
            input.Show(opening);
            builder.Floor.Show(opening);
            Settle(builder.Floor);
            Rest(rig);

            var trail = builder.Trail;

            if (trail.Showing != 0)
            {
                Debug.LogError(
                    "A run nobody has aimed at yet already lights " + trail.Showing + " trail dots.");
            }

            var safeDots = Tints.Of(Trail.Look(TrailMood.Safe).Tint);

            if (safeDots != WorldPalette.Of(PartStyle.Trail))
            {
                Debug.LogError(
                    "A safe preview paints its dots " + safeDots + " where the trail material is "
                    + WorldPalette.Of(PartStyle.Trail) + ".");
            }

            var navigation = NavigationMap.Of(opening);
            var safe = TapAim.Nothing;
            var dangerous = TapAim.Nothing;
            var safeRank = 0;
            var dangerRank = 0;

            foreach (var candidate in input.Candidates())
            {
                var preview = TargetPreview.Of(navigation, candidate.NodeId);

                if (!preview.IsLegal || preview.Route.Count < 2)
                {
                    continue;
                }

                if (!preview.IsDangerous)
                {
                    if (preview.Route.Count > safeRank)
                    {
                        safe = candidate.NodeId;
                        safeRank = preview.Route.Count;
                    }

                    continue;
                }

                var rank = preview.Route.Count
                    + (preview.BlockedByNodeId == candidate.NodeId ? 0 : 100);

                if (rank > dangerRank)
                {
                    dangerous = candidate.NodeId;
                    dangerRank = rank;
                }
            }

            if (safe == TapAim.Nothing || dangerous == TapAim.Nothing)
            {
                Debug.LogError(
                    "The check needs one safe and one dangerous route to tell apart, and found node "
                    + safe + " and node " + dangerous + ".");
                return "\n  no pair of a safe and a dangerous route was there to tell apart";
            }

            var blocked = TargetPreview.Of(navigation, dangerous);

            if (blocked.BlockedByNodeId == dangerous)
            {
                Debug.LogWarning(
                    "The deadliest route on this seed is blocked by the node the finger is on, so the "
                    + "photographs do not separate the corridor from the destination.");
            }

            var safely = Sketched(input, rig, builder, lens, opening, safe, TrailMood.Safe, SafeTrailPath);
            var deadly = Sketched(
                input, rig, builder, lens, opening, dangerous, TrailMood.Dangerous, DangerTrailPath);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0}\n  {1}, blocked by node {2} with {3} fights counted on the way\n  {4}\n  {5}",
                safely,
                deadly,
                blocked.BlockedByNodeId,
                blocked.FightsOnTheWay,
                SweptWithoutThrashing(input, opening),
                ConsumedOnCommit(walker, rig, builder, input, opening, safe));
        }

        static string Sketched(
            TapInput input,
            CameraRig rig,
            WorldBuilder builder,
            Camera lens,
            RunState state,
            int nodeId,
            TrailMood mood,
            string path)
        {
            var trail = builder.Trail;

            input.AimAt(FingerOn(input, state, nodeId));

            var preview = input.Preview;

            if (preview.NodeId != nodeId)
            {
                Debug.LogError("A finger on node " + nodeId + " aimed at " + preview.NodeId + " instead.");
            }

            var expected = Trail.Along(TileRoute.Of(state.Level, preview.Route)).Count;

            if (expected == 0 || trail.Showing != expected)
            {
                Debug.LogError(
                    "Aiming node " + nodeId + " lit " + trail.Showing + " trail dots where its route is "
                    + expected + " dots long.");
            }

            if (!trail.IsPreviewing)
            {
                Debug.LogError(
                    "The trail drawn on the aim at node " + nodeId + " does not read as a preview.");
            }

            if (trail.Mood != mood)
            {
                Debug.LogError(
                    "The route to node " + nodeId + " is drawn " + trail.Mood + " where it is " + mood + ".");
            }

            rig.CutTo(state.Level.Decisions.Node(preview.Route[preview.Route.Count / 2]).Position);
            PreviewFilm.Shoot(lens, path);
            rig.Release();
            Rest(rig);

            input.Cancel();

            if (trail.Showing != 0)
            {
                Debug.LogError(
                    "Cancelling the aim on node " + nodeId + " left " + trail.Showing + " trail dots lit.");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "aiming node {0} drew {1} {2} dots of {3} on the floor and cancelling put them away",
                nodeId,
                expected,
                mood,
                Tints.Of(Trail.Look(mood).Tint));
        }

        static string SweptWithoutThrashing(TapInput input, RunState state)
        {
            var candidates = input.Candidates();
            var from = default(TapCandidate);
            var to = default(TapCandidate);
            var bridged = false;
            var widest = 0f;
            var nearest = float.MaxValue;

            for (var index = 0; index < candidates.Count; index++)
            {
                for (var other = index + 1; other < candidates.Count; other++)
                {
                    var apart = ScreenPoint.Distance(candidates[index].Point, candidates[other].Point);

                    if (apart > 2f * input.Reach && apart <= (1f + TapAim.Hold) * input.Reach)
                    {
                        if (apart <= widest)
                        {
                            continue;
                        }

                        bridged = true;
                        widest = apart;
                    }
                    else
                    {
                        if (bridged || apart >= nearest)
                        {
                            continue;
                        }

                        nearest = apart;
                    }

                    from = candidates[index];
                    to = candidates[other];
                }
            }

            if (!bridged && nearest == float.MaxValue)
            {
                Debug.LogError("The check needs two targets to sweep the aim between, and found fewer.");
                return "no pair of targets was there to sweep the aim between";
            }

            const int Samples = 80;
            var swept = new List<int>();

            for (var sample = 0; sample <= Samples; sample++)
            {
                var along = (float)sample / Samples;

                input.AimAt(new ScreenPoint(
                    from.Point.X + (to.Point.X - from.Point.X) * along,
                    from.Point.Y + (to.Point.Y - from.Point.Y) * along));

                swept.Add(input.Preview.NodeId);
            }

            input.Cancel();

            var visited = new List<int>();
            var settled = 0;

            for (var sample = 0; sample < swept.Count; sample++)
            {
                if (swept[sample] == TapAim.Nothing)
                {
                    Debug.LogError(
                        "Sweeping the aim from node " + from.NodeId + " to node " + to.NodeId
                        + " let go of everything at sample " + sample
                        + ", so the preview blinked out between two targets.");
                    break;
                }

                if (sample != 0 && swept[sample] == swept[sample - 1])
                {
                    continue;
                }

                if (visited.Contains(swept[sample]))
                {
                    Debug.LogError(
                        "Sweeping the aim came back to node " + swept[sample] + " at sample " + sample
                        + " after having left it, so the preview thrashed between two targets.");
                    break;
                }

                visited.Add(swept[sample]);
                settled++;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "an aim swept in {0} steps across the {1:0.#} px between nodes {2} and {3}, {4} the "
                + "{5:0.#} px reach, settled on {6} routes in turn without ever blinking out",
                Samples,
                ScreenPoint.Distance(from.Point, to.Point),
                from.NodeId,
                to.NodeId,
                bridged ? "wider than twice" : "within twice",
                input.Reach,
                settled);
        }

        static string ConsumedOnCommit(
            Walker walker,
            CameraRig rig,
            WorldBuilder builder,
            TapInput input,
            RunState opening,
            int nodeId)
        {
            walker.Begin(rig, builder, input, opening);
            input.Show(opening);
            builder.Floor.Show(opening);

            var trail = builder.Trail;
            var finger = FingerOn(input, opening, nodeId);

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: finger);

            var aimed = trail.Showing;

            if (aimed == 0 || !trail.IsPreviewing)
            {
                Debug.LogError(
                    "A press on node " + nodeId + " drew " + aimed
                    + " dots before the commit, and the choice was made blind.");
            }

            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: finger);

            if (!walker.IsWalking)
            {
                Debug.LogError("Letting go on node " + nodeId + " started no walk.");
                return "letting go on node " + nodeId + " started no walk";
            }

            if (trail.IsPreviewing)
            {
                Debug.LogError("The trail a commit walks along still reads as a preview.");
            }

            var committed = trail.Showing;

            if (committed == 0)
            {
                Debug.LogError("Committing node " + nodeId + " left no trail to walk along.");
            }

            var lit = committed;
            var spent = 0;
            var frames = 0;

            while (walker.IsWalking && frames < 4000)
            {
                Step(rig, builder, walker);
                frames++;

                if (trail.Showing > lit)
                {
                    Debug.LogError(
                        "The trail grew from " + lit + " to " + trail.Showing + " dots on frame " + frames
                        + " of a walk that should only ever spend them.");
                    break;
                }

                if (walker.IsWalking && trail.Showing < lit)
                {
                    spent++;
                }

                lit = trail.Showing;
            }

            if (walker.IsWalking)
            {
                Debug.LogError("A committed walk was still running 4000 frames later.");
            }

            if (trail.Showing != 0)
            {
                Debug.LogError("The walk ended with " + trail.Showing + " trail dots still lit.");
            }

            if (spent == 0)
            {
                Debug.LogError(
                    "The committed trail to node " + nodeId
                    + " never gave a dot up while the walker was moving along it.");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "a press on node {0} drew {1} preview dots, letting go handed the same route to the walker "
                + "as {2} committed dots, and the walk spent them down to none over {3} frames",
                nodeId,
                aimed,
                committed,
                frames);
        }

        static void Rest(CameraRig rig)
        {
            for (var frame = 0; frame < 400 && rig.IsBusy; frame++)
            {
                rig.Advance(1f / 60f);
            }
        }

        static int Longest(RunState state)
        {
            var navigation = NavigationMap.Of(state);
            var found = TapAim.Nothing;
            var steps = 0;

            foreach (var nodeId in TapAim.Aimable(navigation))
            {
                var resolved = ActionResolver.Along(state, navigation.RouteTo(nodeId));

                if (resolved.Outcome == ActionOutcome.Rejected || resolved.Route.Count <= steps)
                {
                    continue;
                }

                found = nodeId;
                steps = resolved.Route.Count;
            }

            return found;
        }

        static int Roomiest(TapInput input, RunState state, int excluded)
        {
            var found = TapAim.Nothing;
            var clearest = 0f;

            foreach (var candidate in input.Candidates())
            {
                if (candidate.NodeId == excluded)
                {
                    continue;
                }

                var room = Room(input, state, candidate);

                if (room <= clearest || !TargetPreview.Of(state, candidate.NodeId).IsLegal)
                {
                    continue;
                }

                found = candidate.NodeId;
                clearest = room;
            }

            return found;
        }

        static void Step(CameraRig rig, WorldBuilder builder, Walker walker)
        {
            const float Frame = 1f / 60f;

            walker.Advance(Frame);
            rig.Advance(Frame);
            builder.Floor.Advance(Frame);
            builder.Pickups.Advance(Frame);

            if (builder.PlayerBadge != null)
            {
                builder.PlayerBadge.Advance(Frame);
            }
        }

        static string PanHeldAndGivenBack(TapInput input, CameraRig rig, RunState state)
        {
            input.Show(state);

            var resting = rig.Framing;
            for (var frame = 0; frame < 400; frame++)
            {
                rig.Advance(1f / 60f);
                if (rig.Framing.Equals(resting))
                {
                    break;
                }

                resting = rig.Framing;
            }

            var anchor = new ScreenPoint(input.FrameWidth * 0.5f, input.FrameHeight * 0.5f);
            var away = Nudged(anchor, input.Reach * 4f);
            var player = rig.Framing;
            var committed = 0;

            Action<TargetPreview> count = preview => committed++;
            input.Tapped += count;

            if (rig.IsAway)
            {
                Debug.LogError("A camera resting on the player at " + player + " already reads as away from it.");
            }

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: anchor);
            input.Reading(pressedNow: false, releasedNow: false, isPressed: true, hovers: false, finger: away);

            var panned = rig.Framing;

            if (panned.Equals(player))
            {
                Debug.LogError(
                    "A drag of " + ScreenPoint.Distance(anchor, away) + " px, past the " + input.Reach
                    + " px reach, left the camera on the player at " + player + ".");
            }

            if (input.Preview.IsAimed)
            {
                Debug.LogError("A drag past the reach still holds an aim on node " + input.Preview.NodeId + ".");
            }

            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: away);

            if (!rig.Framing.Equals(panned))
            {
                Debug.LogError(
                    "Letting go of a drag moved the camera from " + panned + " to " + rig.Framing
                    + " instead of leaving it where the finger left it.");
            }

            if (!rig.IsAway)
            {
                Debug.LogError("A camera held at " + rig.Framing + " off the player does not read as away from it.");
            }

            for (var frame = 0; frame < 180; frame++)
            {
                rig.Advance(1f / 60f);

                if (!rig.Framing.Equals(panned))
                {
                    Debug.LogError(
                        "A held pan crept from " + panned + " to " + rig.Framing + " after " + frame
                        + " frames of nobody touching it.");
                    break;
                }
            }

            var bound = HeldAtTheBound(input, rig);

            if (committed != 0)
            {
                Debug.LogError("Dragging committed " + committed + " taps and should commit none.");
            }

            input.LookBack();

            if (rig.IsAway)
            {
                Debug.LogError(
                    "Asking for the camera back left it reading as away from the player at " + rig.Framing + ".");
            }

            var homed = 0;
            while (!rig.Framing.Equals(player) && homed < 400)
            {
                rig.Advance(1f / 60f);
                homed++;

                if (rig.IsAway)
                {
                    Debug.LogError(
                        "The camera read as away again on frame " + homed + " of easing home to the player.");
                    break;
                }
            }

            if (homed <= 1)
            {
                Debug.LogError("Asking for the camera back cut to the player rather than easing.");
            }

            if (!rig.Framing.Equals(player))
            {
                Debug.LogError(
                    "Asking for the camera back left it at " + rig.Framing + " rather than on the player at "
                    + player + ".");
            }

            var aiming = Dragged(input, rig, away);
            var target = LegalTarget(input);

            if (!rig.IsAway)
            {
                Debug.LogError("A fresh drag to " + aiming + " does not read as away from the player.");
            }

            if (target == TapAim.Nothing)
            {
                Debug.LogError("The check needs one legal node to commit onto while panned, and found none.");
                input.Tapped -= count;
                return "\n  a pan held, and nothing legal was left to commit onto";
            }

            var finger = FingerOn(input, state, target);

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: finger);

            if (!rig.Framing.Equals(aiming))
            {
                Debug.LogError(
                    "Aiming node " + target + " while panned moved the camera from " + aiming + " to "
                    + rig.Framing + ".");
            }

            if (!rig.IsAway)
            {
                Debug.LogError("Aiming node " + target + " while panned gave the camera back to the player.");
            }

            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: finger);

            if (committed != 1)
            {
                Debug.LogError("A tap on legal node " + target + " committed " + committed + " taps, not one.");
            }

            if (!rig.Framing.Equals(aiming))
            {
                Debug.LogError("Committing node " + target + " cut the camera back rather than easing it.");
            }

            if (rig.IsAway)
            {
                Debug.LogError(
                    "Committing node " + target + " left the camera reading as away from the player at "
                    + rig.Framing + ".");
            }

            var came = 0;
            while (!rig.Framing.Equals(player) && came < 400)
            {
                rig.Advance(1f / 60f);
                came++;

                if (rig.IsAway)
                {
                    Debug.LogError(
                        "The camera read as away again on frame " + came + " of easing home after a commit.");
                    break;
                }
            }

            input.Tapped -= count;

            if (came <= 1)
            {
                Debug.LogError("Committing a tap cut back to the player rather than easing back.");
            }

            if (!rig.Framing.Equals(player))
            {
                Debug.LogError(
                    "Committing a tap left the camera at " + rig.Framing + " rather than back on the player at "
                    + player + ".");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  a drag of {0:0.#} px past the {1:0.#} px reach panned to {2} and held there untouched, "
                + "{3}, came home on request in {4} frames, then aimed node {5} while panned without giving the "
                + "camera back and eased home in {6} frames on the commit",
                ScreenPoint.Distance(anchor, away),
                input.Reach,
                panned.Target,
                bound,
                homed,
                target,
                came);
        }

        static string HeldAtTheBound(TapInput input, CameraRig rig)
        {
            var anchor = new ScreenPoint(input.FrameWidth * 0.5f, input.FrameHeight * 0.5f);
            var edge = Dragged(input, rig, Nudged(anchor, -input.FrameWidth * 20f));
            var harder = Dragged(input, rig, Nudged(anchor, -input.FrameWidth * 400f));

            if (ScreenFrame.PanPixels(edge, harder) >= 1f)
            {
                Debug.LogError(
                    "Pulling twenty times harder at the level bound bought more of the world: " + edge + " became "
                    + harder + ".");
            }

            for (var frame = 0; frame < 180; frame++)
            {
                rig.Advance(1f / 60f);

                if (!rig.Framing.Equals(harder))
                {
                    Debug.LogError(
                        "The camera sprang back off the level bound from " + harder + " to " + rig.Framing
                        + " after " + frame + " frames.");
                    break;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "stopped dead at the level bound {0} however hard it was pulled",
                harder.Target);
        }

        static CameraFraming Dragged(TapInput input, CameraRig rig, ScreenPoint to)
        {
            var anchor = new ScreenPoint(input.FrameWidth * 0.5f, input.FrameHeight * 0.5f);

            input.Reading(pressedNow: true, releasedNow: false, isPressed: true, hovers: false, finger: anchor);
            input.Reading(pressedNow: false, releasedNow: false, isPressed: true, hovers: false, finger: to);
            input.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false, finger: to);

            return rig.Framing;
        }

        static int LegalTarget(TapInput input)
        {
            var found = TapAim.Nothing;

            foreach (var candidate in input.Candidates())
            {
                input.AimAt(candidate.Point);

                if (input.Preview.IsLegal)
                {
                    found = candidate.NodeId;
                    break;
                }
            }

            input.Cancel();
            return found;
        }

        static ScreenPoint Nudged(ScreenPoint point, float pixels)
        {
            return new ScreenPoint(point.X + pixels, point.Y);
        }

        static void Settle(FloorState floor)
        {
            for (var frame = 0; frame < 200 && !floor.IsSettled; frame++)
            {
                floor.Advance(1f / 60f);
            }
        }

        static int TakenBehindAnEnemy(
            TapInput input, TargetBoard board, RunState state, NavigationMap navigation)
        {
            var aimable = new HashSet<int>(TapAim.Aimable(navigation));
            var behind = 0;

            foreach (var target in board.Targets)
            {
                if (target.NodeId == state.PositionNodeId || state.IsConsumed(target.NodeId))
                {
                    continue;
                }

                if (!aimable.Contains(target.NodeId))
                {
                    Debug.LogError(
                        "Drawn node " + target.NodeId + " is never offered to the finger.");
                    continue;
                }

                if (state.IsReachable(target.NodeId))
                {
                    continue;
                }

                behind++;

                if (navigation.FightsOnTheWayTo(target.NodeId) < 1)
                {
                    Debug.LogError(
                        "Node " + target.NodeId
                        + " stands out of passage yet navigation walks in without a fight.");
                }

                var preview = TargetPreview.Of(navigation, target.NodeId);

                if (!preview.IsLegal)
                {
                    Debug.LogError(
                        "A tap on node " + target.NodeId + " behind an enemy produced no route.");
                }
                else if (preview.Route[preview.Route.Count - 1] != target.NodeId)
                {
                    Debug.LogError(
                        "The route to node " + target.NodeId + " behind an enemy ends somewhere else.");
                }

                input.AimAt(FingerOn(input, state, target.NodeId));

                if (input.Preview.NodeId == TapAim.Nothing)
                {
                    Debug.LogError(
                        "A finger on node " + target.NodeId + " behind an enemy aimed at nothing at all.");
                }

                input.Cancel();
            }

            return behind;
        }

        static ScreenPoint FingerOn(TapInput input, RunState state, int nodeId)
        {
            return ScreenProjection.Of(
                input.Framing,
                TapAim.AnchorOf(state.Level.Decisions.Node(nodeId)),
                input.FrameWidth,
                input.FrameHeight);
        }

        static void WearsThePreview(TargetBoard board, RunState state, TargetPreview preview, int nodeId)
        {
            var target = board.Of(nodeId);
            var mark = TargetMarks.Of(state, nodeId, preview);

            if (target.Mark != mark)
            {
                Debug.LogError("Node " + nodeId + " should wear " + mark + " and wears " + target.Mark + ".");
            }

            var look = TargetMarks.Look(mark);
            var badge = target.Badge;

            if (badge == null)
            {
                var gate = target.Gate;

                if (gate == null)
                {
                    Debug.LogError(
                        "Node " + nodeId + " marks neither a badge nor a gate, so nothing shows the aim.");
                    return;
                }

                var glowing = Tints.Of(GateLook.Washed(gate.Tint, look));

                if (gate.Colour != glowing)
                {
                    Debug.LogError(
                        "Node " + nodeId + " wears " + mark + " but its arch glows " + gate.Colour
                        + " rather than " + glowing + ".");
                }

                return;
            }

            var washed = Tints.Of(BadgeTints.Washed(badge.Style, look));
            washed.a = look.Opacity;

            if (badge.Colour != washed)
            {
                Debug.LogError(
                    "Node " + nodeId + " wears " + mark + " but its badge is painted " + badge.Colour
                    + " rather than " + washed + ".");
            }

            if (TargetMarks.IsAimed(mark) && badge.Value != preview.Power)
            {
                Debug.LogError(
                    "Node " + nodeId + " is aimed at and reads " + badge.Value
                    + " rather than the " + preview.Power + " the walk arrives with.");
            }
        }

        static void Clearest(
            ref TapCandidate clearest, ref float clearestRoom, TapCandidate candidate, float room, bool worth)
        {
            if (!worth || room <= clearestRoom)
            {
                return;
            }

            clearest = candidate;
            clearestRoom = room;
        }

        static float Room(TapInput input, RunState state, TapCandidate candidate)
        {
            var room = float.MaxValue;

            foreach (var node in state.Level.Decisions.Nodes)
            {
                WorldPart prop;
                if (node.Id == candidate.NodeId || !LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                room = Math.Min(room, ScreenPoint.Distance(candidate.Point, FingerOn(input, state, node.Id)));
            }

            return room;
        }

        static string Photograph(
            TapInput input,
            CameraRig rig,
            Camera lens,
            string path,
            string leg,
            TargetBoard board,
            RunState state,
            TapCandidate candidate)
        {
            input.AimAt(candidate.Point);
            rig.CutTo(state.Level.Decisions.Node(candidate.NodeId).Position);
            PreviewFilm.Shoot(lens, path);
            rig.Release();

            for (var frame = 0; frame < 200 && rig.IsBusy; frame++)
            {
                rig.Advance(1f / 60f);
            }

            return Shot(leg, board, candidate);
        }

        static string Shot(string leg, TargetBoard board, TapCandidate candidate)
        {
            var target = board.Of(candidate.NodeId);

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  photographed the {0} preview on node {1} standing at {2}, wearing {3} in {4}",
                leg,
                candidate.NodeId,
                target.transform.position,
                target.Mark,
                target.GetComponent<NumberBadge>().Colour);
        }

        static string Row(TapInput input, RunState state, int move)
        {
            var candidates = TapAim.Candidates(
                state, input.Framing, ScreenFrame.Width, ScreenFrame.Height);
            var reach = TouchTargets.Reach;
            var narrowest = float.MaxValue;
            var full = 0;

            for (var index = 0; index < candidates.Count; index++)
            {
                var separation = float.MaxValue;
                for (var other = 0; other < candidates.Count; other++)
                {
                    if (other == index)
                    {
                        continue;
                    }

                    separation = Math.Min(
                        separation, ScreenPoint.Distance(candidates[index].Point, candidates[other].Point));
                }

                var target = TouchTargets.Millimetres(
                    2f * Math.Min(reach, separation * 0.5f), TouchTargets.ReferenceDotsPerInch);
                narrowest = Math.Min(narrowest, target);
                if (separation * 0.5f >= reach)
                {
                    full++;
                }
            }

            var unreachable = 0;
            foreach (var node in state.Level.Decisions.Nodes)
            {
                WorldPart prop;
                if (LevelBlueprintBuilder.TryProp(node, out prop) && !state.IsReachable(node.Id))
                {
                    unreachable++;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  move {0}: power {1}, {2} targets ({3} at the full {4:0.#} mm, narrowest {5:0.##} mm), "
                + "{6} drawn nodes unreachable",
                move,
                state.Power,
                candidates.Count,
                full,
                TouchTargets.MinimumMillimetres,
                narrowest,
                unreachable);
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
