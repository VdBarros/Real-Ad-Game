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

        public static void Check()
        {
            Wipe(IdlePath);
            Wipe(WinPath);
            Wipe(LossPath);

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

            var state = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
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

                foreach (var candidate in input.Candidates())
                {
                    input.AimAt(candidate.Point);

                    var preview = input.Preview;
                    var resolved = ActionResolver.Resolve(state, candidate.NodeId);

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

                    var room = Photogenic(state, candidate.NodeId) ? Room(input, state, candidate) : 0f;
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

                RefuseTheUnreachable(input, builder.Targets, state);

                if (stepped == TapAim.Nothing)
                {
                    break;
                }

                var before = tapped.Count;
                input.AimAt(FingerOn(input, state, stepped));
                input.ReleaseAt(FingerOn(input, state, stepped));

                if (tapped.Count != before + 1 || tapped[tapped.Count - 1].NodeId != stepped)
                {
                    Debug.LogError("Releasing over node " + stepped + " did not commit that node.");
                }

                state = ActionResolver.Resolve(state, stepped).State;
                builder.Floor.Show(state);
                builder.PlayerBadge.Show(state.Power);
            }

            report.Append("\n  ")
                .Append(previewed.ToString(CultureInfo.InvariantCulture))
                .Append(" previews agreed with the resolver, ")
                .Append(multiHop.ToString(CultureInfo.InvariantCulture))
                .Append(" of them multi-hop, ")
                .Append(tapped.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" taps committed");

            if (previewed == 0 || multiHop == 0 || tapped.Count == 0)
            {
                Debug.LogError(
                    "The check needs previews, a multi-hop one and a committed tap, and got "
                    + previewed + ", " + multiHop + " and " + tapped.Count + ".");
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

        static void Settle(FloorState floor)
        {
            for (var frame = 0; frame < 200 && !floor.IsSettled; frame++)
            {
                floor.Advance(1f / 60f);
            }
        }

        static void RefuseTheUnreachable(TapInput input, TargetBoard board, RunState state)
        {
            var aimable = new HashSet<int>(TapAim.Aimable(state));

            foreach (var target in board.Targets)
            {
                if (aimable.Contains(target.NodeId) || target.NodeId == state.PositionNodeId)
                {
                    continue;
                }

                if (state.IsReachable(target.NodeId))
                {
                    continue;
                }

                if (target.Mark != TargetMark.Unreachable)
                {
                    Debug.LogError(
                        "Unreachable node " + target.NodeId + " wears " + target.Mark + " and reads as tappable.");
                }

                input.AimAt(FingerOn(input, state, target.NodeId));

                if (input.Preview.NodeId == target.NodeId)
                {
                    Debug.LogError("A finger on unreachable node " + target.NodeId + " aimed at it anyway.");
                }

                input.Cancel();
            }
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

            var badge = target.GetComponent<NumberBadge>();
            var look = TargetMarks.Look(mark);
            var washed = Color.Lerp(BadgePalette.Of(badge.Style), Tints.Of(look.Tint), look.Weight);

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

        static bool Photogenic(RunState state, int nodeId)
        {
            var position = state.Level.Decisions.Node(nodeId).Position;
            var above = new TilePosition(position.Elevation + Terraces.Rise, position.X, position.Y);

            return !state.Level.Tiles.Contains(above) || !state.Level.Tiles.AreAdjacent(position, above);
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
