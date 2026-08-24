using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class ProjectBootstrap
    {
        const string CompanyName = "VdBarros";
        const string ProductName = "Real Ad Game";
        const string AndroidApplicationIdentifier = "com.vdbarros.realadgame";
        const int NewInputSystemOnly = 1;

        [MenuItem("Tools/Real Ad Game/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidApplicationIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            AssetDatabase.SaveAssets();

            AssertNewInputSystemIsActive();
        }

        static void AssertNewInputSystemIsActive()
        {
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length == 0)
            {
                Debug.LogError("Could not load ProjectSettings.asset, so active input handling went unverified.");
                return;
            }

            var activeInputHandler = new SerializedObject(playerSettings[0]).FindProperty("activeInputHandler");
            if (activeInputHandler == null)
            {
                Debug.LogError("ProjectSettings.asset has no activeInputHandler property, so active input handling went unverified.");
                return;
            }

            if (activeInputHandler.intValue != NewInputSystemOnly)
            {
                Debug.LogError(
                    "Active input handling is not 'Input System Package (New)'. " +
                    "Change it in Project Settings > Player and restart the Editor.");
            }
        }
    }
}
