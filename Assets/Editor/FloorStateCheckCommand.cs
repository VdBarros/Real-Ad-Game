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
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
                .Append(Row("opening", state, root, Painted(graph.Tiles, root), 0, 0));

            var midflip = false;
            var won = 0;

            for (var win = 1; win <= Wins; win++)
            {
                var target = NextWin(state);
                if (target < 0)
                {
                    break;
                }

                var before = Painted(graph.Tiles, root);
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

            var acrossTheCut = Painted(graph.Tiles, root);
            rig.CutTo(state.Level.Decisions.Node(state.PositionNodeId).Position);
            rig.Release();

            for (var frame = 0; frame < Ceiling && rig.IsBusy; frame++)
            {
                rig.Advance(Frame);
            }

            report.Append(Row("after a cut and the flight back", state, root, acrossTheCut, 0, 0));
            report.Append(TheSignalRidesOnChroma(root));

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
            var painted = Painted(state.Level.Tiles, root);
            var disagreeing = 0;

            foreach (var tile in state.Level.Tiles.Tiles)
            {
                string material;
                if (!painted.TryGetValue(
                        LevelBlueprintBuilder.WalkingSurfaceOf(state.Level.Tiles, tile.Position), out material)
                    || material != Expected(reading.IsCleared(tile.Position)))
                {
                    disagreeing++;
                }
            }

            if (disagreeing > 0)
            {
                Debug.LogError(
                    disagreeing + " of " + state.Level.Tiles.Tiles.Count
                    + " tiles wear the wrong material after the " + leg
                    + " leg, so the surface a tile is walked on is not the one the floor state paints.");
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

        static string TheSignalRidesOnChroma(GameObject root)
        {
            var cursed = Worn(root, PartStyle.Floor);
            var cleared = Worn(root, PartStyle.Cleared);

            if (!cursed.HasValue || !cleared.HasValue)
            {
                Debug.LogError(
                    "The built floor wears no material for one of its two states, so the cleared signal "
                    + "cannot be read off the world it is drawn in.");

                return "\n  the cleared signal: unreadable, a floor material is missing";
            }

            var cursedTint = Read(cursed.Value);
            var clearedTint = Read(cleared.Value);
            var hueApart = Tint.HueApart(cursedTint, clearedTint);
            var valueApart = Tint.Contrast(cursedTint, clearedTint);
            var midway = Read(Color.Lerp(cursed.Value, cleared.Value, 0.5f));

            if (hueApart > WorldTints.SharedFloorHue)
            {
                Debug.LogError(
                    "The two floors sit " + hueApart.ToString("0.##", CultureInfo.InvariantCulture)
                    + " degrees of hue apart, so a tile changes colour rather than saturation when it clears.");
            }

            if (clearedTint.Chroma < cursedTint.Chroma * WorldTints.LeastClearedChromaLift)
            {
                Debug.LogError(
                    "A cleared tile carries only " + clearedTint.Chroma.ToString("0.###", CultureInfo.InvariantCulture)
                    + " of chroma against a cursed tile's "
                    + cursedTint.Chroma.ToString("0.###", CultureInfo.InvariantCulture)
                    + ", so saturation is not what tells the two apart.");
            }

            if (valueApart > WorldTints.MostClearedValueShift)
            {
                Debug.LogError(
                    "The two floors stand " + valueApart.ToString("0.###", CultureInfo.InvariantCulture)
                    + ":1 apart in value, so the progress signal is a grey shift and it eats the separation "
                    + "the cast reads against.");
            }

            if (Tint.HueApart(midway, cursedTint) > WorldTints.SharedFloorHue)
            {
                Debug.LogError(
                    "Halfway through the sweep a tile sits " + midway.Hue.ToString("0.##", CultureInfo.InvariantCulture)
                    + " degrees of hue from the floor it started on, so the blend leaves the shared hue.");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  the cleared signal: hue {0:0.##} against {1:0.##} ({2:0.##} apart), chroma {3:0.###} "
                + "against {4:0.###}, value {5:0.###}:1 apart, midway hue {6:0.##}",
                cursedTint.Hue,
                clearedTint.Hue,
                hueApart,
                cursedTint.Chroma,
                clearedTint.Chroma,
                valueApart,
                midway.Hue);
        }

        static Tint Read(Color colour)
        {
            return new Tint(colour.r, colour.g, colour.b);
        }

        static Color? Worn(GameObject root, PartStyle style)
        {
            var wanted = WorldMaterials.NamePrefix + style;

            foreach (var skin in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = skin.sharedMaterial;

                if (material == null || material.name != wanted || !material.HasProperty(BaseColorId))
                {
                    continue;
                }

                return material.GetColor(BaseColorId);
            }

            return null;
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

        static IReadOnlyDictionary<string, string> Painted(TileGrid tiles, GameObject root)
        {
            var byName = new Dictionary<string, Transform>();

            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            var painted = new Dictionary<string, string>();

            foreach (var tile in tiles.Tiles)
            {
                var name = LevelBlueprintBuilder.WalkingSurfaceOf(tiles, tile.Position);
                Transform surface;

                if (!byName.TryGetValue(name, out surface))
                {
                    continue;
                }

                var skin = surface.GetComponentInChildren<Renderer>(true);

                if (skin != null && skin.sharedMaterial != null)
                {
                    painted.Add(name, skin.sharedMaterial.name);
                }
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
