using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class AndroidBuildCommand
    {
        const string OutputPath = "Builds/Android/RealAdGame.apk";

        [MenuItem("Tools/Real Ad Game/Build Android")]
        public static void Build()
        {
            ProjectBootstrap.Apply();

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("EditorBuildSettings has no enabled scenes to build.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Android build {report.summary.result} with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Android build succeeded: {OutputPath} ({new FileInfo(OutputPath).Length} bytes)");
        }
    }
}
