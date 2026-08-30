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

        const float Drift = 0.0005f;

        const float Seated = 0.05f;

        sealed class Run
        {
            public readonly HashSet<string> Told = new HashSet<string>();

            public string Name;
            public float SkipAt;
            public bool Skips;
            public int Frames;
            public float Seconds;
            public float ReelAt;
            public int Carriers;
            public int BadgeTextures;
            public int WorldMaterials;
            public List<string> WorldMaterialNames = new List<string>();
            public int Samples;
            public int PackWorn;
            public int PackParts;
            public int Slides;
            public bool Stalled;
        }

        sealed class Mark
        {
            public bool Seen;
            public CastLook Look;
            public Vector3 Stood;
        }

        static int findings;

        static WorldModels library;

        public static void Check()
        {
            findings = 0;
            library = new WorldModels();

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

            library.Dispose();
            library = null;
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
            var watched = Watching();
            var beat = PillarBeat.Over;

            Act(scene);
            NobodySlides(run, scene, watched);
            TheStageWearsThePack(run, scene);
            TheCastWearsThePack(run, scene);
            beat = scene.Reel.Beat;

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

                if (loop.Phase != GamePhase.Cutscene || scene.Root == null)
                {
                    continue;
                }

                Act(scene);
                NobodySlides(run, scene, watched);

                if (scene.Reel.Beat == beat)
                {
                    continue;
                }

                beat = scene.Reel.Beat;
                TheStageWearsThePack(run, scene);
                TheCastWearsThePack(run, scene);
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
                NoPackInstanceOutlivedTheCutscene(run, loop);
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

        static Mark[] Watching()
        {
            var watched = new Mark[Enum.GetValues(typeof(PillarRole)).Length];

            for (var slot = 0; slot < watched.Length; slot++)
            {
                watched[slot] = new Mark();
            }

            return watched;
        }

        static void Act(PillarCutscene scene)
        {
            if (scene.Root == null)
            {
                return;
            }

            foreach (var acting in scene.Root.GetComponentsInChildren<FigureAnimator>(false))
            {
                acting.Advance(Frame);
            }
        }

        static void NobodySlides(Run run, PillarCutscene scene, Mark[] watched)
        {
            foreach (var role in PillarDress.Roles)
            {
                var mark = watched[(int)role];
                var look = PillarDress.MarkOf(scene.Reel, role).Look;
                var figure = scene.FigureOf(role);

                if (figure == null)
                {
                    mark.Seen = false;
                    continue;
                }

                var stood = figure.position;

                if (mark.Seen && mark.Look == look)
                {
                    var crossed = new Vector2(stood.x - mark.Stood.x, stood.z - mark.Stood.z).magnitude;

                    if (crossed > Drift)
                    {
                        run.Slides++;
                        var acting = scene.ActingOf(role);

                        if (acting == null || acting.Act != FigureAct.Walk)
                        {
                            Once(
                                run,
                                "slid-" + role,
                                run.Name + " slid " + role + " " + crossed.ToString("0.####", CultureInfo.InvariantCulture)
                                + "m across the ground at " + Seconds(scene.Reel.Elapsed) + "s while playing "
                                + (acting == null ? "no clip at all" : acting.Act.ToString()) + ".");
                        }
                    }
                }

                mark.Seen = true;
                mark.Look = look;
                mark.Stood = stood;
            }
        }

        static void TheCastWearsThePack(Run run, PillarCutscene scene)
        {
            foreach (var role in PillarDress.Roles)
            {
                var mark = PillarDress.MarkOf(scene.Reel, role);
                var mesh = PillarDress.MeshOf(mark.Look);
                var figure = scene.FigureOf(role);
                run.Samples++;

                if (figure == null)
                {
                    Once(
                        run,
                        "raised-" + role,
                        run.Name + " raised nobody for " + role + " to wear " + mark.Look + " with.");
                    continue;
                }

                if (scene.WornBy(role) != mesh)
                {
                    Once(
                        run,
                        "worn-" + role + "-" + mark.Look,
                        run.Name + " dressed " + role + " in " + scene.WornBy(role) + " where " + mark.Look
                        + " wears " + mesh + ".");
                }

                var pack = PackMesh.Of(library.Of(mesh));
                var worn = 0;

                foreach (var renderer in figure.GetComponentsInChildren<Renderer>(true))
                {
                    var skin = PackMesh.On(renderer);

                    if (skin == null)
                    {
                        continue;
                    }

                    if (pack.Contains(skin))
                    {
                        worn++;
                    }
                    else
                    {
                        Once(
                            run,
                            "stranger-" + role + "-" + mark.Look,
                            run.Name + " has " + role + " wearing " + skin.name + ", which the " + mesh
                            + " pack does not carry.");
                    }
                }

                if (worn == 0)
                {
                    Once(
                        run,
                        "pack-" + role + "-" + mark.Look,
                        run.Name + " has " + role + " wearing nothing the " + mesh + " pack carries at "
                        + scene.Reel.Beat + ", so the cast is not dressed in the pack.");
                }
                else
                {
                    run.PackWorn++;
                }

                TheCastPlaysWhatItIsCued(run, scene, role, mark, mesh);
                TheBadgeSitsAboveTheHead(run, scene, role, mark);
            }
        }

        static void TheCastPlaysWhatItIsCued(
            Run run, PillarCutscene scene, PillarRole role, CastMark mark, PartModel mesh)
        {
            var acting = scene.ActingOf(role);
            var cue = PillarDress.CueOf(role, scene.Reel.Elapsed);

            if (acting == null || !acting.IsRigged)
            {
                Once(
                    run,
                    "rig-" + role + "-" + mark.Look,
                    run.Name + " has " + role + " wearing " + mesh + " with no rig to move it.");
                return;
            }

            if (!acting.HasClipsToPlay || acting.Playing == null)
            {
                Once(
                    run,
                    "clip-" + role + "-" + mark.Look,
                    run.Name + " has " + role + " holding a static pose where " + cue.Clip + " was cued.");
                return;
            }

            if (acting.Act == cue.Act || (!cue.Loops && acting.Act == FigureAct.Idle))
            {
                return;
            }

            Once(
                run,
                "act-" + role + "-" + scene.Reel.Beat,
                run.Name + " has " + role + " playing " + acting.Act + " on " + scene.Reel.Beat + " where "
                + cue.Act + " was cued.");
        }

        static void TheBadgeSitsAboveTheHead(
            Run run, PillarCutscene scene, PillarRole role, CastMark mark)
        {
            var badge = scene.BadgeOf(role);

            if (badge == null)
            {
                Once(run, "badge-" + role, run.Name + " hung no badge over " + role + ".");
                return;
            }

            if (badge.Value != mark.Number)
            {
                Once(
                    run,
                    "number-" + role + "-" + scene.Reel.Beat,
                    run.Name + " shows " + badge.Value + " over " + role + " on " + scene.Reel.Beat
                    + " where the reel asks for " + mark.Number + ".");
            }

            if (badge.Style != mark.Badge)
            {
                Once(
                    run,
                    "tier-" + role,
                    run.Name + " draws " + role + "'s badge as " + badge.Style + " where the reel asks for "
                    + mark.Badge + ".");
            }

            var head = mark.Position.Y + PillarDress.StandingHeightOf(mark);

            if (badge.transform.localPosition.y >= head)
            {
                return;
            }

            Once(
                run,
                "above-" + role + "-" + scene.Reel.Beat,
                run.Name + " hangs " + role + "'s badge at "
                + badge.transform.localPosition.y.ToString("0.###", CultureInfo.InvariantCulture)
                + " where the head it labels reaches " + head.ToString("0.###", CultureInfo.InvariantCulture)
                + " on " + scene.Reel.Beat + ".");
        }

        static void TheStageWearsThePack(Run run, PillarCutscene scene)
        {
            var pack = PackMesh.Of(library.Of(PillarDress.StageModel));
            run.PackParts++;

            if (Bare(run, "the ground", scene.Ground, pack))
            {
                var box = PackMesh.Wearing(scene.Ground, pack);

                if (Math.Abs(box.max.y) > Seated)
                {
                    Fail(
                        run.Name + " lays the ground with its top face at "
                        + box.max.y.ToString("0.###", CultureInfo.InvariantCulture) + " rather than underfoot.");
                }

                if (Math.Abs(box.size.x - PillarDress.GroundReach) > Seated)
                {
                    Fail(
                        run.Name + " lays a ground " + box.size.x.ToString("0.###", CultureInfo.InvariantCulture)
                        + "m across where the stage reaches " + PillarDress.GroundReach + "m.");
                }
            }

            foreach (var role in PillarDress.Roles)
            {
                var pillar = scene.PillarOf(role);

                if (!Bare(run, role + "'s pillar", pillar, pack))
                {
                    continue;
                }

                var height = PillarDress.MarkOf(scene.Reel, role).PillarHeight;
                var box = PackMesh.Wearing(pillar, pack);

                if (Math.Abs(box.min.y) > Seated)
                {
                    Once(
                        run,
                        "seated-" + role,
                        run.Name + " seats " + role + "'s pillar at "
                        + box.min.y.ToString("0.###", CultureInfo.InvariantCulture) + " rather than on the ground.");
                }

                if (Math.Abs(box.size.y - height) > Seated)
                {
                    Once(
                        run,
                        "tall-" + role,
                        run.Name + " stands " + role + "'s pillar "
                        + box.size.y.ToString("0.###", CultureInfo.InvariantCulture) + "m tall on "
                        + scene.Reel.Beat + " where the reel asks for "
                        + height.ToString("0.###", CultureInfo.InvariantCulture) + "m.");
                }
            }
        }

        static bool Bare(Run run, string what, Transform part, ISet<Mesh> pack)
        {
            if (part == null)
            {
                Once(run, "part-" + what, run.Name + " raised nothing at all for " + what + ".");
                return false;
            }

            var worn = 0;

            foreach (var renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                var skin = PackMesh.On(renderer);

                if (skin == null)
                {
                    continue;
                }

                if (pack.Contains(skin))
                {
                    worn++;
                }
                else
                {
                    Once(
                        run,
                        "strange-" + what,
                        run.Name + " built " + what + " out of " + skin.name + ", which the "
                        + PillarDress.StageModel + " pack mesh does not carry.");
                }
            }

            if (worn > 0)
            {
                return true;
            }

            Once(
                run,
                "bare-" + what,
                run.Name + " built " + what + " out of no pack mesh at all, so the stage is still primitives.");
            return false;
        }

        static void NoPackInstanceOutlivedTheCutscene(Run run, GameLoop loop)
        {
            foreach (var acting in Resources.FindObjectsOfTypeAll<FigureAnimator>())
            {
                if (loop.LevelRoot != null && acting.transform.root.gameObject == loop.LevelRoot)
                {
                    continue;
                }

                Fail(run.Name + " left " + Trail(acting.gameObject) + " animating a pack mesh after the handoff.");
            }

            foreach (var carrier in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!carrier.name.EndsWith(PillarCutscene.MeshSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                Fail(run.Name + " left the pack instance " + Trail(carrier) + " standing after the handoff.");
            }
        }

        static void Once(Run run, string key, string finding)
        {
            if (!run.Told.Add(key))
            {
                return;
            }

            Fail(finding);
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

            var strays = MintedAssets.StraysAmong(first.WorldMaterialNames);

            if (strays.Length != 0)
            {
                Fail(
                    "A handoff settles on " + first.WorldMaterials
                    + " world materials, and a style is coloured at most once, but it holds " + strays
                    + ".");
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
            run.WorldMaterialNames = MintedAssets.WorldMaterialNames();
            run.WorldMaterials = run.WorldMaterialNames.Count;
        }

        static string Report(List<Run> runs)
        {
            var report = new StringBuilder("the pillar cutscene, ")
                .Append(Seconds(PillarStage.Total))
                .Append("s scripted, four ways in and one way out:")
                .Append("\n  run                        skipAt  reelAt  frames  seconds  carriers  badgeTex  world"
                    + "  cast  packWorn  stage  slides");

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
                    .Append(Column(run.WorldMaterials.ToString(CultureInfo.InvariantCulture), 7))
                    .Append(Column(run.Samples.ToString(CultureInfo.InvariantCulture), 6))
                    .Append(Column(run.PackWorn.ToString(CultureInfo.InvariantCulture), 10))
                    .Append(Column(run.PackParts.ToString(CultureInfo.InvariantCulture), 7))
                    .Append(run.Slides.ToString(CultureInfo.InvariantCulture));
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
