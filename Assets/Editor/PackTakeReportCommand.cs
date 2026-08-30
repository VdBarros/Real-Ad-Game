using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class PackTakeReportCommand
    {
        public const string FolderArgument = "-packFolder";

        public const string PathArgument = "-packReport";

        const string DefaultFolder = "Assets/Resources/" + WorldModels.CharacterFolder;

        const string DefaultPath = "dev/scratch/pack-takes.txt";

        const float LoopFloor = 0.0005f;

        const float LoopShare = 0.02f;

        public static void Report()
        {
            var folder = Argument(FolderArgument, DefaultFolder);
            var path = Argument(PathArgument, DefaultPath);
            var report = new StringBuilder();
            var assets = Models(folder);

            report.AppendLine("pack take report over " + folder);
            report.AppendLine();

            foreach (var asset in assets)
            {
                Sheet(asset, report);
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, report.ToString());

            Debug.Log("pack-takes: " + assets.Count + " model assets under " + folder + " written to " + path);
        }

        static void Sheet(string asset, StringBuilder report)
        {
            report.AppendLine("MODEL " + asset);

            var importer = AssetImporter.GetAtPath(asset) as ModelImporter;
            if (importer == null)
            {
                report.AppendLine("  no model importer");
                report.AppendLine();
                return;
            }

            Takes(importer, report);
            Clips(asset, report);
            Nodes(asset, report);

            report.AppendLine();
        }

        static void Takes(ModelImporter importer, StringBuilder report)
        {
            var takes = importer.importedTakeInfos;
            if (takes == null || takes.Length == 0)
            {
                report.AppendLine("  TAKES 0");
                return;
            }

            report.AppendLine("  TAKES " + takes.Length);

            foreach (var take in takes)
            {
                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "    take {0} {1:0.###}s at {2:0.#}fps, frames {3:0.#} to {4:0.#}, wanted {5}",
                    take.name,
                    take.stopTime - take.startTime,
                    take.sampleRate,
                    Math.Round(take.startTime * take.sampleRate),
                    Math.Round(take.stopTime * take.sampleRate),
                    AdventurerClips.Wants(take.name)));
            }
        }

        static void Clips(string asset, StringBuilder report)
        {
            var clips = new List<AnimationClip>();
            var paths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var loaded in AssetDatabase.LoadAllAssetsAtPath(asset))
            {
                var clip = loaded as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    clips.Add(clip);
                }
            }

            report.AppendLine("  CLIPS " + clips.Count);

            foreach (var clip in clips)
            {
                float residual;
                var closes = Closes(clip, paths, out residual);

                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "    clip {0} {1:0.###}s, marked loop {2}, pose closes {3} worst {4:0.#####}",
                    clip.name,
                    clip.length,
                    clip.isLooping,
                    closes,
                    residual));
            }

            report.AppendLine("  BINDINGS " + paths.Count);

            foreach (var bound in paths)
            {
                report.AppendLine("    binds " + bound);
            }
        }

        static bool Closes(AnimationClip clip, ISet<string> paths, out float residual)
        {
            var closes = true;
            residual = 0f;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                paths.Add(binding.path);

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                var lowest = float.MaxValue;
                var highest = float.MinValue;

                foreach (var key in curve.keys)
                {
                    lowest = Math.Min(lowest, key.value);
                    highest = Math.Max(highest, key.value);
                }

                var drift = Math.Abs(curve.Evaluate(clip.length) - curve.Evaluate(0f));
                var allowed = Math.Max(LoopFloor, (highest - lowest) * LoopShare);

                residual = Math.Max(residual, drift);

                if (drift > allowed)
                {
                    closes = false;
                }
            }

            return closes;
        }

        static void Nodes(string asset, StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            if (prefab == null)
            {
                report.AppendLine("  NODES 0");
                return;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            var named = new List<string>();

            foreach (var node in instance.GetComponentsInChildren<Transform>(true))
            {
                if (node != instance.transform)
                {
                    named.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} rests at {1:0.#####} {2:0.#####} {3:0.#####}",
                        Trail(instance.transform, node),
                        node.localPosition.x,
                        node.localPosition.y,
                        node.localPosition.z));
                }
            }

            named.Sort(StringComparer.Ordinal);

            var box = PackMesh.Bare(prefab);

            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "  BOUNDS {0:0.#####} by {1:0.#####} by {2:0.#####} based at {3:0.#####}",
                box.size.x,
                box.size.y,
                box.size.z,
                box.min.y));

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = PackMesh.On(renderer);
                if (mesh == null)
                {
                    continue;
                }

                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "  MESH {0} {1} vertices {2} submeshes, local {3:0.#####} by {4:0.#####} by {5:0.#####}, "
                    + "turned {6:0.##} {7:0.##} {8:0.##}, scaled {9:0.####} {10:0.####} {11:0.####}",
                    mesh.name,
                    mesh.vertexCount,
                    mesh.subMeshCount,
                    mesh.bounds.size.x,
                    mesh.bounds.size.y,
                    mesh.bounds.size.z,
                    renderer.transform.localEulerAngles.x,
                    renderer.transform.localEulerAngles.y,
                    renderer.transform.localEulerAngles.z,
                    renderer.transform.lossyScale.x,
                    renderer.transform.lossyScale.y,
                    renderer.transform.lossyScale.z));
            }

            var scale = ArtPacks.CastImportScale;

            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "  PACK width {0:0.#####} height {1:0.#####} depth {2:0.#####} base {3:0.#####}",
                box.size.x / scale,
                box.size.y / scale,
                box.size.z / scale,
                box.min.y / scale));

            report.AppendLine("  NODES " + named.Count);

            foreach (var trail in named)
            {
                report.AppendLine("    node " + trail);
            }

            WorldObjects.Destroy(instance);
        }

        static string Trail(Transform root, Transform node)
        {
            var trail = node.name;

            for (var walk = node.parent; walk != null && walk != root; walk = walk.parent)
            {
                trail = walk.name + "/" + trail;
            }

            return trail;
        }

        static List<string> Models(string folder)
        {
            var assets = new List<string>();

            if (!Directory.Exists(folder))
            {
                return assets;
            }

            foreach (var file in Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories))
            {
                assets.Add(file.Replace('\\', '/'));
            }

            assets.Sort(StringComparer.Ordinal);

            return assets;
        }

        static string Argument(string flag, string fallback)
        {
            var given = Environment.GetCommandLineArgs();

            for (var slot = 0; slot < given.Length - 1; slot++)
            {
                if (string.Equals(given[slot], flag, StringComparison.Ordinal))
                {
                    return given[slot + 1];
                }
            }

            return fallback;
        }
    }
}
