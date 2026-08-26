using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Flow;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTooling
{
    public static class CutsceneCheckCommand
    {
        const long Seed = 20250826L;

        const float Frame = 1f / 60f;

        const int Ceiling = 3000;

        const float Budget = 20f;

        const string ShotPath = "dev/scratch/t-18-";

        sealed class Run
        {
            public string Name;
            public float SkipAt;
            public bool Skips;
            public int Frames;
            public float Seconds;
            public float ReelAt;
            public int Carriers;
            public int BadgeTextures;
            public int WorldMaterials;
            public bool Stalled;
        }

        static int findings;

        public static void Check()
        {
            findings = 0;

            var runs = new List<Run>
            {
                Played(),
                Skipped("skipped-at-frame-zero", 0f),
                Skipped("skipped-mid-reel", PillarStage.Cross),
                Skipped("skipped-on-the-last-frame", PillarStage.Total - Frame * 1.5f)
            };

            EveryRunHandedOverTheSameScene(runs);
            TheRigStopsStagingWhenSomethingElseHoldsIt();
            AClosedLoopDoesNotTurnAgain();

            Debug.Log(Report(runs));
            Debug.Log(findings == 0
                ? "t-18: the pillars played and handed over clean on all " + runs.Count + " runs."
                : "t-18: " + findings + " findings across " + runs.Count + " runs.");
        }

        static Run Played()
        {
            var run = new Run { Name = "played-right-through", Skips = false };
            Turn(run, false, 0f);
            return run;
        }

        static Run Skipped(string name, float skipAt)
        {
            var run = new Run { Name = name, Skips = true, SkipAt = skipAt };
            Turn(run, true, skipAt);
            return run;
        }

        static void Turn(Run run, bool skips, float skipAt)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreviewFilm.Sun();

            var scene = new PillarCutscene();
            var loop = GameLoop.Raise(Seed, MazePreset.Ship, scene);
            var persistent = Identified(Live());
            var announced = new List<GamePhase>();
            loop.Turned += turned => announced.Add(turned.Phase);

            loop.Advance(Frame);
            run.Frames = 1;
            run.Seconds = Frame;

            TheGameOpensOnThePillars(run, loop, scene);

            var mintedTexture = Minted<Texture2D>(BadgeAssets.NamePrefix);
            var mintedMaterial = Minted<Material>(BadgeAssets.NamePrefix);
            var stage = scene.Root;

            TheCutsceneMintedWhatItDraws(run, mintedTexture, mintedMaterial, stage);

            var shot = false;
            var skipped = false;

            while (loop.Phase == GamePhase.Cutscene && run.Frames < Ceiling)
            {
                if (!shot && scene.Reel.Beat == PillarBeat.Cross)
                {
                    PreviewFilm.Shoot(loop.Rig.GetComponent<Camera>(), ShotPath + run.Name + ".png");
                    shot = true;
                }

                if (skips && scene.Reel.Elapsed >= skipAt)
                {
                    run.ReelAt = scene.Reel.Elapsed;
                    loop.Skip();
                    skips = false;
                    skipped = true;
                }

                loop.Advance(Frame);
                run.Frames++;
                run.Seconds += Frame;
            }

            if (run.Frames >= Ceiling)
            {
                run.Stalled = true;
                Fail(run.Name + " never came out of the cutscene.");
            }
            else
            {
                if (run.Skips && !skipped)
                {
                    Fail(run.Name + " never got the chance to skip, so it proves nothing about skipping.");
                }

                if (!run.Skips)
                {
                    run.ReelAt = PillarStage.Total;
                }

                ThePillarsRanInsideTheBudget(run);
                TheHandoffReachedAFreshLevel(run, loop, announced);
                NothingTheCutsceneMadeOutlivedIt(run, mintedTexture, mintedMaterial, stage);
                TheLevelIsTheOnlyThingLeftStanding(run, loop, persistent);
                Measure(run);

                if (!shot)
                {
                    PreviewFilm.Shoot(loop.Rig.GetComponent<Camera>(), ShotPath + run.Name + ".png");
                }
            }

            loop.Close();
            WorldObjects.Destroy(loop.gameObject);
        }

        static void TheRigStopsStagingWhenSomethingElseHoldsIt()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            rig.Begin(graph);

            if (!rig.IsBusy)
            {
                Fail("A rig handed a level was expected to open on a fly-through.");
            }

            rig.Hold(PillarStage.Wide);

            if (rig.IsBusy)
            {
                Fail("A held rig still reports a fly-through, so the loop would sit in preview forever.");
            }

            if (!rig.Framing.Equals(PillarStage.Wide))
            {
                Fail("A held rig frames " + rig.Framing + " rather than what it was handed.");
            }

            rig.Begin(graph);

            if (!rig.IsBusy)
            {
                Fail("A rig handed a level after being held does not fly over it.");
            }

            WorldObjects.Destroy(rig.gameObject);
        }

        static void AClosedLoopDoesNotTurnAgain()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreviewFilm.Sun();

            var scene = new PillarCutscene();
            var loop = GameLoop.Raise(Seed, MazePreset.Ship, scene);

            loop.Advance(Frame);

            if (loop.Phase != GamePhase.Cutscene)
            {
                Fail("A closing loop was expected to be mid-cutscene, not in " + loop.Phase + ".");
            }

            loop.Close();

            if (scene.IsPlaying)
            {
                Fail("Closing the loop left the cutscene playing.");
            }

            if (scene.Root != null)
            {
                Fail("Closing the loop left the pillars standing.");
            }

            var refused = false;

            try
            {
                loop.Advance(Frame);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            catch (Exception broken)
            {
                Fail("A closed loop broke with " + broken.GetType().Name + " rather than refusing to turn.");
                refused = true;
            }

            if (!refused)
            {
                Fail("A closed loop turned again rather than refusing.");
            }

            WorldObjects.Destroy(loop.gameObject);
        }

        static void TheGameOpensOnThePillars(Run run, GameLoop loop, PillarCutscene scene)
        {
            if (loop.Phase != GamePhase.Cutscene)
            {
                Fail(run.Name + " opened in " + loop.Phase + " rather than on the cutscene.");
                return;
            }

            if (!scene.IsPlaying)
            {
                Fail(run.Name + " reached the cutscene phase with nothing playing.");
            }

            if (loop.LevelRoot != null)
            {
                Fail(run.Name + " raised a level before the cutscene had finished.");
            }

            if (scene.Root == null)
            {
                Fail(run.Name + " played the cutscene without raising anything to look at.");
            }
        }

        static void TheCutsceneMintedWhatItDraws(
            Run run, Texture2D texture, Material material, GameObject stage)
        {
            if (texture == null)
            {
                Fail(run.Name + " drew badges off no texture, so nothing proves one was freed.");
            }

            if (material == null)
            {
                Fail(run.Name + " drew badges through no material, so nothing proves one was freed.");
            }

            if (stage == null)
            {
                Fail(run.Name + " has no cutscene root to watch for.");
            }
        }

        static void ThePillarsRanInsideTheBudget(Run run)
        {
            if (PillarStage.Total >= Budget)
            {
                Fail(
                    "The reel is scripted to run " + Seconds(PillarStage.Total)
                    + "s, which is not under " + Seconds(Budget) + "s.");
            }

            if (run.Seconds >= Budget)
            {
                Fail(
                    run.Name + " took " + Seconds(run.Seconds) + "s of frames, which is not under "
                    + Seconds(Budget) + "s.");
            }

        }

        static void TheHandoffReachedAFreshLevel(Run run, GameLoop loop, List<GamePhase> announced)
        {
            if (loop.Phase != GamePhase.Preview)
            {
                Fail(run.Name + " handed over into " + loop.Phase + " rather than a fly-through.");
                return;
            }

            if (loop.LevelNumber != 1)
            {
                Fail(run.Name + " handed over onto level " + loop.LevelNumber + ".");
            }

            if (loop.LevelRoot == null)
            {
                Fail(run.Name + " reached a fly-through with no level under it.");
            }

            if (loop.Run == null || loop.Run.Power != loop.Level.StartingPower)
            {
                Fail(run.Name + " carried the cutscene into the level rather than the preset opening power.");
            }

            var expected = new[] { GamePhase.Cutscene, GamePhase.Preview };

            if (announced.Count != expected.Length)
            {
                Fail(
                    run.Name + " announced " + string.Join(", ", announced)
                    + " where the loop turns cutscene then preview.");
                return;
            }

            for (var step = 0; step < expected.Length; step++)
            {
                if (announced[step] != expected[step])
                {
                    Fail(
                        run.Name + " announced " + string.Join(", ", announced)
                        + " where the loop turns cutscene then preview.");
                    return;
                }
            }
        }

        static void NothingTheCutsceneMadeOutlivedIt(
            Run run, Texture2D texture, Material material, GameObject stage)
        {
            if (stage != null)
            {
                Fail(run.Name + " left the cutscene root standing after the handoff.");
            }

            if (texture != null)
            {
                Fail(run.Name + " left the badge texture it minted alive after the handoff.");
            }

            if (material != null)
            {
                Fail(run.Name + " left the badge material it minted alive after the handoff.");
            }
        }

        static void TheLevelIsTheOnlyThingLeftStanding(Run run, GameLoop loop, HashSet<EntityId> persistent)
        {
            if (loop.LevelRoot == null)
            {
                return;
            }

            foreach (var carrier in Live())
            {
                if (persistent.Contains(carrier.GetEntityId()))
                {
                    continue;
                }

                if (carrier.transform.root.gameObject != loop.LevelRoot)
                {
                    Fail(run.Name + " left " + Trail(carrier) + " outside the level root after the handoff.");
                }
            }
        }

        static void EveryRunHandedOverTheSameScene(List<Run> runs)
        {
            var first = Baseline(runs);

            if (first == null)
            {
                Fail("Every run stalled, so nothing compares against anything.");
                return;
            }

            foreach (var run in runs)
            {
                if (run.Stalled)
                {
                    continue;
                }

                if (run.Skips && run.Frames > first.Frames)
                {
                    Fail(
                        run.Name + " took " + run.Frames + " frames where playing right through took "
                        + first.Frames + ".");
                }

                if (run.Carriers != first.Carriers)
                {
                    Fail(
                        run.Name + " settled on " + run.Carriers + " carriers where "
                        + first.Name + " settled on " + first.Carriers + ".");
                }

                if (run.BadgeTextures != first.BadgeTextures)
                {
                    Fail(
                        run.Name + " settled on " + run.BadgeTextures + " badge textures where "
                        + first.Name + " settled on " + first.BadgeTextures + ".");
                }

                if (run.WorldMaterials != first.WorldMaterials)
                {
                    Fail(
                        run.Name + " settled on " + run.WorldMaterials + " world materials where "
                        + first.Name + " settled on " + first.WorldMaterials + ".");
                }
            }

            if (first.BadgeTextures != 1)
            {
                Fail(
                    "A handoff settles on the one badge texture the level draws off, not "
                    + first.BadgeTextures + ".");
            }

            if (first.WorldMaterials > Enum.GetValues(typeof(PartStyle)).Length)
            {
                Fail(
                    "A handoff settles on " + first.WorldMaterials
                    + " world materials where a style is coloured at most once.");
            }
        }

        static Run Baseline(List<Run> runs)
        {
            foreach (var run in runs)
            {
                if (!run.Stalled)
                {
                    return run;
                }
            }

            return null;
        }

        static void Measure(Run run)
        {
            run.Carriers = Live().Count;
            run.BadgeTextures = Counted<Texture2D>(BadgeAssets.NamePrefix);
            run.WorldMaterials = Counted<Material>(Presentation.WorldMaterials.NamePrefix);
        }

        static string Report(List<Run> runs)
        {
            var report = new StringBuilder("the pillar cutscene, ")
                .Append(Seconds(PillarStage.Total))
                .Append("s scripted, four ways in and one way out:")
                .Append("\n  run                        skipAt  reelAt  frames  seconds  carriers  badgeTex  world");

            foreach (var run in runs)
            {
                report.Append("\n  ")
                    .Append(run.Name.PadRight(26))
                    .Append(Column(run.Skips ? Seconds(run.SkipAt) : "-", 8))
                    .Append(Column(Seconds(run.ReelAt), 8))
                    .Append(Column(run.Frames.ToString(CultureInfo.InvariantCulture), 8))
                    .Append(Column(Seconds(run.Seconds), 9))
                    .Append(Column(run.Carriers.ToString(CultureInfo.InvariantCulture), 10))
                    .Append(Column(run.BadgeTextures.ToString(CultureInfo.InvariantCulture), 10))
                    .Append(run.WorldMaterials.ToString(CultureInfo.InvariantCulture));
            }

            return report.ToString();
        }

        static string Column(string value, int width)
        {
            return value.PadRight(width);
        }

        static string Seconds(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static T Minted<T>(string prefix) where T : UnityEngine.Object
        {
            foreach (var asset in Resources.FindObjectsOfTypeAll<T>())
            {
                if (asset.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return asset;
                }
            }

            return null;
        }

        static int Counted<T>(string prefix) where T : UnityEngine.Object
        {
            var counted = 0;

            foreach (var asset in Resources.FindObjectsOfTypeAll<T>())
            {
                if (asset.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    counted++;
                }
            }

            return counted;
        }

        static List<GameObject> Live()
        {
            var live = new List<GameObject>();

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Gather(root.transform, live);
            }

            return live;
        }

        static void Gather(Transform carrier, List<GameObject> live)
        {
            live.Add(carrier.gameObject);

            for (var child = 0; child < carrier.childCount; child++)
            {
                Gather(carrier.GetChild(child), live);
            }
        }

        static HashSet<EntityId> Identified(List<GameObject> carriers)
        {
            var identified = new HashSet<EntityId>();

            foreach (var carrier in carriers)
            {
                identified.Add(carrier.GetEntityId());
            }

            return identified;
        }

        static string Trail(GameObject carrier)
        {
            var trail = carrier.name;

            for (var parent = carrier.transform.parent; parent != null; parent = parent.parent)
            {
                trail = parent.name + "/" + trail;
            }

            return trail;
        }

        static void Fail(string finding)
        {
            findings++;
            Debug.LogError(finding);
        }
    }
}
