using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class DesktopBuildCommand
    {
        const string OutputPath = "Builds/Windows/RealAdGame.exe";

        [MenuItem("Tools/Real Ad Game/Build Windows")]
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

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Windows build {report.summary.result} with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Windows build succeeded: {OutputPath}");
        }
    }
}
