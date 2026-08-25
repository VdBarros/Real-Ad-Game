using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class FloorStateCheckCommand
    {
        const long Seed = 20250824L;

        const float Frame = 1f / 60f;

        const int Ceiling = 200;

        const int Wins = 4;

        const string CursedPath = "dev/scratch/t-12-floor-cursed.png";

        const string MidflipPath = "dev/scratch/t-12-floor-midflip.png";

        const string ClearedPath = "dev/scratch/t-12-floor-cleared.png";

        public static void Check()
        {
            Wipe(CursedPath);
            Wipe(MidflipPath);
            Wipe(ClearedPath);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var floor = builder.Floor;
            PreviewFilm.Sun();

            rig.Begin(graph);
            rig.Skip();
            PreviewFilm.Shoot(lens, CursedPath);

            var state = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var report = new StringBuilder("floor state on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(Row("opening", state, root, Painted(root), 0, 0));

            var midflip = false;
            var won = 0;

            for (var win = 1; win <= Wins; win++)
            {
                var target = NextWin(state);
                if (target < 0)
                {
                    break;
                }

                var before = Painted(root);
                var reading = FloorReading.Of(state);
                state = ActionResolver.Resolve(state, target).State;
                var flipping = FloorReading.Of(state).Since(reading);

                floor.Show(state);

                var frames = 0;
                while (!floor.IsSettled && frames < Ceiling)
                {
                    floor.Advance(Frame);
                    frames++;

                    if (midflip || frames * Frame < FloorSweep.Seconds * 0.5f)
                    {
                        continue;
                    }

                    PreviewFilm.Shoot(lens, MidflipPath);
                    midflip = true;
                }

                won = win;
                report.Append(Row("win " + win, state, root, before, flipping.Count, frames));
            }

            PreviewFilm.Shoot(lens, ClearedPath);

            var acrossTheCut = Painted(root);
            rig.CutTo(state.Level.Decisions.Node(state.PositionNodeId).Position);
            rig.Release();

            for (var frame = 0; frame < Ceiling && rig.IsBusy; frame++)
            {
                rig.Advance(Frame);
            }

            report.Append(Row("after a cut and the flight back", state, root, acrossTheCut, 0, 0));

            if (won < Wins)
            {
                Debug.LogError(
                    "The check needs " + Wins + " winnable enemies to flip the floor with and found " + won + ".");
            }

            Debug.Log(report.ToString());

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();
        }

        static string Row(
            string leg,
            RunState state,
            GameObject root,
            IReadOnlyDictionary<string, string> before,
            int expected,
            int frames)
        {
            var reading = FloorReading.Of(state);
            var painted = Painted(root);
            var disagreeing = 0;

            foreach (var tile in state.Level.Tiles.Tiles)
            {
                string material;
                if (!painted.TryGetValue(PartNames.Tile(tile.Position), out material)
                    || material != Expected(reading.IsCleared(tile.Position)))
                {
                    disagreeing++;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0}: the reading covers {1} of {2} tiles, {3} repainted against {4} newly read, "
                + "{5} wear the wrong material, settled in {6} frames ({7:0.###}s of {8:0.###}s)",
                leg,
                reading.Cleared.Count,
                state.Level.Tiles.Tiles.Count,
                Changed(before, painted),
                expected,
                disagreeing,
                frames,
                frames * Frame,
                FloorSweep.Seconds);
        }

        static string Expected(bool cleared)
        {
            return WorldMaterials.NamePrefix + (cleared ? PartStyle.Cleared : PartStyle.Floor);
        }

        static int Changed(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
        {
            var changed = 0;

            foreach (var pair in after)
            {
                string was;
                if (!before.TryGetValue(pair.Key, out was) || was != pair.Value)
                {
                    changed++;
                }
            }

            return changed;
        }

        static IReadOnlyDictionary<string, string> Painted(GameObject root)
        {
            var painted = new Dictionary<string, string>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.name.StartsWith("Tile", StringComparison.Ordinal))
                {
                    continue;
                }

                painted.Add(renderer.name, renderer.sharedMaterial.name);
            }

            return painted;
        }

        static int NextWin(RunState state)
        {
            foreach (var nodeId in state.ReachableNodes)
            {
                if (!state.BlocksPassage(nodeId))
                {
                    continue;
                }

                if (ActionResolver.Resolve(state, nodeId).Outcome == ActionOutcome.Win)
                {
                    return nodeId;
                }
            }

            return -1;
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
