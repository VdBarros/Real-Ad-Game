using System;
using System.Collections.Generic;
using Game.Presentation;
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

            KeepRuntimeShadersInTheBuild();

            AssetDatabase.SaveAssets();

            AssertNewInputSystemIsActive();
        }

        public static IEnumerable<string> RuntimeShaderNames()
        {
            foreach (var name in WorldMaterials.ShaderNames)
            {
                yield return name;
            }

            foreach (var name in BadgeAssets.ShaderNames)
            {
                yield return name;
            }

            foreach (var name in WorldBackdrop.ShaderNames)
            {
                yield return name;
            }
        }

        public static void KeepRuntimeShadersInTheBuild()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settings.Length == 0)
            {
                throw new InvalidOperationException("Could not load GraphicsSettings.asset to include runtime shaders.");
            }

            var serialized = new SerializedObject(settings[0]);
            var included = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (included == null)
            {
                throw new InvalidOperationException("GraphicsSettings.asset has no m_AlwaysIncludedShaders property.");
            }

            var standing = new HashSet<Shader>();
            for (var slot = 0; slot < included.arraySize; slot++)
            {
                var shader = included.GetArrayElementAtIndex(slot).objectReferenceValue as Shader;
                if (shader != null)
                {
                    standing.Add(shader);
                }
            }

            var added = 0;
            foreach (var name in RuntimeShaderNames())
            {
                var shader = Shader.Find(name);
                if (shader == null || !standing.Add(shader))
                {
                    continue;
                }

                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            if (added == 0)
            {
                return;
            }

            serialized.ApplyModifiedProperties();
            Debug.Log("Always Included Shaders gained " + added + " shader(s) the runtime asks for by name.");
        }

        static void AssertNewInputSystemIsActive()
        {
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length == 0)
            {
                throw new InvalidOperationException("Could not load ProjectSettings.asset to verify active input handling.");
            }

            var activeInputHandler = new SerializedObject(playerSettings[0]).FindProperty("activeInputHandler");
            if (activeInputHandler == null)
            {
                throw new InvalidOperationException("ProjectSettings.asset has no activeInputHandler property.");
            }

            if (activeInputHandler.intValue != NewInputSystemOnly)
            {
                throw new InvalidOperationException(
                    "Active input handling is not 'Input System Package (New)'. " +
                    "Change it in Project Settings > Player and restart the Editor.");
            }
        }
    }
}
