using System;
using System.Collections.Generic;
using System.Text;
using Game.Flow;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTooling
{
    public static class BootCheckCommand
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        const string ShotPath = "dev/scratch/boot.png";

        const float Frame = 1f / 60f;

        const int Frames = 240;

        static int findings;

        public static void Check()
        {
            findings = 0;

            EveryShaderTheRuntimeAsksForSurvivesTheBuild();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var loop = TheShippedSceneBoots();
            if (loop == null)
            {
                Debug.LogError("t-boot: " + findings + " findings.");
                return;
            }

            TheLoopTurnsWithoutThrowing(loop);
            OnlyTheRigDrawsTheScreen();
            TheCutsceneIsOnFrame(loop);

            PreviewFilm.Shoot(loop.Rig.GetComponent<Camera>(), ShotPath);
            loop.Close();
            WorldObjects.Destroy(loop.gameObject);

            Debug.Log(findings == 0
                ? "t-boot: the shipped scene boots and draws itself."
                : "t-boot: " + findings + " findings.");
        }

        static void EveryShaderTheRuntimeAsksForSurvivesTheBuild()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settings.Length == 0)
            {
                Fail("GraphicsSettings.asset could not be read, so nothing proves the runtime shaders ship.");
                return;
            }

            var included = new SerializedObject(settings[0]).FindProperty("m_AlwaysIncludedShaders");
            var standing = new HashSet<string>();

            for (var slot = 0; slot < included.arraySize; slot++)
            {
                var shader = included.GetArrayElementAtIndex(slot).objectReferenceValue as Shader;
                if (shader != null)
                {
                    standing.Add(shader.name);
                }
            }

            var reachable = 0;

            foreach (var name in ProjectBootstrap.RuntimeShaderNames())
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    continue;
                }

                if (standing.Contains(shader.name))
                {
                    reachable++;
                    continue;
                }

                Fail(
                    "The runtime asks for " + name + " by name, and nothing in the build references it, "
                    + "so Shader.Find returns null in a player and every material built from it throws.");
            }

            if (reachable == 0)
            {
                Fail("No shader the runtime asks for is always-included, so the world cannot be coloured at all.");
            }
        }

        static GameLoop TheShippedSceneBoots()
        {
            try
            {
                GameBoot.Open();
            }
            catch (Exception opening)
            {
                Fail("GameBoot threw " + opening.GetType().Name + ": " + opening.Message);
                return null;
            }

            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if (loop == null)
            {
                Fail("The shipped scene booted without raising a loop.");
            }

            return loop;
        }

        static void TheLoopTurnsWithoutThrowing(GameLoop loop)
        {
            for (var frame = 0; frame < Frames; frame++)
            {
                try
                {
                    loop.Advance(Frame);
                }
                catch (Exception turning)
                {
                    Fail(
                        "Frame " + frame + " threw " + turning.GetType().Name + ": " + turning.Message
                        + " — a player would throw this every frame and draw nothing.");
                    return;
                }
            }

            if (loop.Phase != GamePhase.Cutscene)
            {
                Fail("Four seconds into an eighteen second reel the loop sits in " + loop.Phase + ".");
            }
        }

        static void OnlyTheRigDrawsTheScreen()
        {
            var drawing = new List<string>();

            foreach (var lens in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (lens.enabled && lens.gameObject.activeInHierarchy)
                {
                    drawing.Add(lens.gameObject.name);
                }
            }

            if (drawing.Count != 1)
            {
                Fail("The screen is drawn by " + drawing.Count + " cameras (" + string.Join(", ", drawing)
                    + ") where the rig alone should own it.");
                return;
            }

            if (drawing[0] != PartNames.Rig)
            {
                Fail("The screen is drawn by " + drawing[0] + " rather than the rig.");
            }
        }

        static void TheCutsceneIsOnFrame(GameLoop loop)
        {
            var stage = GameObject.Find(PillarCutscene.RootName);
            if (stage == null)
            {
                Fail("The cutscene raised no stage to look at.");
                return;
            }

            var lens = loop.Rig.GetComponent<Camera>();
            var seen = 0;

            foreach (var renderer in stage.GetComponentsInChildren<Renderer>(true))
            {
                var viewport = lens.WorldToViewportPoint(renderer.bounds.center);
                if (viewport.z > lens.nearClipPlane
                    && viewport.x >= 0f && viewport.x <= 1f
                    && viewport.y >= 0f && viewport.y <= 1f)
                {
                    seen++;
                }
            }

            if (seen == 0)
            {
                Fail("The cutscene built a stage the camera cannot see any of.");
            }
        }

        static void Fail(string finding)
        {
            findings++;
            Debug.LogError(finding);
        }
    }
}
