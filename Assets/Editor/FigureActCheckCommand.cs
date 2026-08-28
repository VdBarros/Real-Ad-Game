using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.EditorTooling
{
    public static class FigureActCheckCommand
    {
        const long Seed = 20250824L;

        const float Frame = 1f / 60f;

        const int FrameCap = 4000;

        const int Moves = 6;

        const float Epsilon = 1e-4f;

        const float StirFloor = 1f;

        const string AbsentClip = "no_such_clip_the_pack_never_carried";

        const string FramePrefix = "dev/scratch/t-34-walk-";

        const int Sequence = 8;

        const int EveryNthFrame = 4;

        const int CostFrames = 60;

        const int CostRounds = 3;

        const float CloseRange = 6f;

        const float CloseFraming = 0.6f;

        const float LimbTurn = 20f;

        const int Limbs = 16;

        static readonly ActionOutcome[] FoughtOutcomes =
        {
            ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss
        };

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-34 characters animate, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            using (var models = new WorldModels())
            {
                failures += Imported(models, report);
                failures += TheWholeCastCarriesTheSameClips(models, report);
                failures += Cut(models, report);
                failures += Answered(models, report);
                failures += Degrades(models, report);
                failures += Walked(models, report);
            }

            report.Append("\n  t-34: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError("The figure act check failed " + failures + " assertions. Read the report above.");
            }
        }

        static int Imported(WorldModels models, StringBuilder report)
        {
            var worn = PartModels.Of(PartStyle.Start);
            var path = "Assets/Resources/" + WorldModels.AssetPathOf(worn) + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
            {
                return Assert(report, false, "the character mesh has a model importer", path + " has none");
            }

            var takes = importer.importedTakeInfos ?? new TakeInfo[0];
            var baked = new HashSet<string>(StringComparer.Ordinal);
            foreach (var take in takes)
            {
                baked.Add(take.name);
            }

            var absentTakes = new List<string>();
            foreach (var wanted in AdventurerClips.Names)
            {
                if (!baked.Contains(wanted))
                {
                    absentTakes.Add(wanted);
                }
            }

            var failures = Assert(
                report,
                takes.Length > 0 && absentTakes.Count == 0,
                "the character FBX the art library already carries holds the animation takes this ticket needs, "
                + "so no further download is owed to anyone",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} takes are baked into {1}, and all {2} the pure clip table names are among them{3}",
                    takes.Length,
                    path,
                    AdventurerClips.Count,
                    absentTakes.Count == 0
                        ? string.Empty
                        : " except " + string.Join(", ", absentTakes.ToArray())));

            failures += Assert(
                report,
                importer.importAnimation
                && importer.animationType == CharacterArtPostprocessor.Rig
                && importer.animationCompression == CharacterArtPostprocessor.AnimationCompression
                && importer.removeConstantScaleCurves
                && importer.resampleCurves
                && !importer.importAnimatedCustomProperties,
                "the character postprocessor turns animation import on from code, reversing for this pack the "
                + "decision the dungeon postprocessor makes to leave it off",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "importAnimation {0}, rig {1}, compression {2}, constant scale curves removed {3}, "
                    + "resampled {4}, animated custom properties {5}",
                    importer.importAnimation,
                    importer.animationType,
                    importer.animationCompression,
                    importer.removeConstantScaleCurves,
                    importer.resampleCurves,
                    importer.importAnimatedCustomProperties));

            var narrowed = importer.clipAnimations ?? new ModelImporterClipAnimation[0];
            var unwanted = new List<string>();
            var mislooped = new List<string>();
            foreach (var clip in narrowed)
            {
                if (!AdventurerClips.Wants(clip.name))
                {
                    unwanted.Add(clip.name);
                }
                else if (clip.loopTime != AdventurerClips.LoopsOf(clip.name))
                {
                    mislooped.Add(clip.name);
                }
            }

            failures += Assert(
                report,
                narrowed.Length == AdventurerClips.Count && unwanted.Count == 0 && mislooped.Count == 0,
                "the postprocessor narrows the import to exactly the clips the pure table names, and loops only "
                + "the ones the table says loop, so the takes nothing plays never reach the player",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of the {1} takes are imported against the {2} wanted{3}{4}",
                    narrowed.Length,
                    takes.Length,
                    AdventurerClips.Count,
                    unwanted.Count == 0 ? string.Empty : ", including unwanted " + string.Join(", ", unwanted.ToArray()),
                    mislooped.Count == 0 ? string.Empty : ", mislooped " + string.Join(", ", mislooped.ToArray())));

            var loaded = new List<string>();
            var absent = new List<string>();
            var shortest = float.MaxValue;
            var longest = 0f;

            foreach (var name in AdventurerClips.Names)
            {
                var clip = models.ClipOf(worn, name);
                if (clip == null)
                {
                    absent.Add(name);
                    continue;
                }

                loaded.Add(string.Format(
                    CultureInfo.InvariantCulture, "{0} {1:0.###}s", clip.name, clip.length));
                shortest = Math.Min(shortest, clip.length);
                longest = Math.Max(longest, clip.length);
            }

            failures += Assert(
                report,
                absent.Count == 0 && shortest > 0f && models.ClipCountOf(worn) == AdventurerClips.Count,
                "every clip the pure table names loads out of the resources tree with a length to play, and "
                + "nothing else does",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1} loaded out of the {2} the resources tree carries, {3:0.###}s to {4:0.###}s{5}",
                    loaded.Count,
                    AdventurerClips.Count,
                    models.ClipCountOf(worn),
                    shortest == float.MaxValue ? 0f : shortest,
                    longest,
                    absent.Count == 0 ? string.Empty : ", missing " + string.Join(", ", absent.ToArray())));

            report.Append("\n  clips: ").Append(string.Join(", ", loaded.ToArray()));

            var stillOff = new List<string>();
            var animated = new List<string>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None || ArtPacks.IsRigged(model))
                {
                    continue;
                }

                var other = AssetImporter.GetAtPath(
                    "Assets/Resources/" + WorldModels.AssetPathOf(model) + ".fbx") as ModelImporter;
                if (other == null)
                {
                    continue;
                }

                if (other.importAnimation)
                {
                    animated.Add(model.ToString());
                }
                else
                {
                    stillOff.Add(model.ToString());
                }
            }

            failures += Assert(
                report,
                animated.Count == 0 && stillOff.Count > 0,
                "the dungeon pack's models keep animation import off, so the reversal is scoped to the character "
                + "pack alone",
                stillOff.Count + " dungeon models still import no animation: "
                + string.Join(", ", stillOff.ToArray())
                + (animated.Count == 0
                    ? string.Empty
                    : "; but " + string.Join(", ", animated.ToArray()) + " do"));

            return failures;
        }

        static int Cut(WorldModels models, StringBuilder report)
        {
            var worn = PartModels.Of(PartStyle.Start);
            var failures = 0;
            var rows = new List<string>();

            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                failures += Fits(models, report, worn, FigureCues.Striking(fight), fight.Seconds, outcome + " fight");
                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} plays {1} for {2:0.###}s",
                    outcome,
                    FigureCues.Striking(fight).Act,
                    fight.Seconds));
            }

            failures += Fits(
                models,
                report,
                worn,
                FigureCue.Within(FigureAct.Take, Take.Seconds),
                Take.Seconds,
                "pickup");

            rows.Add(string.Format(
                CultureInfo.InvariantCulture, "a pickup plays {0} for {1:0.###}s", FigureAct.Take, Take.Seconds));

            failures += Assert(
                report,
                FigureCues.Of(null).Act == FigureAct.Idle
                && FigureCue.Still.Loops
                && FigureCue.Walking.Act == FigureAct.Walk
                && FigureCue.Walking.Loops
                && FigureCue.Looping(FigureAct.Retreat).Loops
                && !FigureCue.Within(FigureAct.Take, Take.Seconds).Loops,
                "the standing, walking and falling-back cues loop while every cue cut to a beat does not",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "no journey cues {0}, walking cues {1} looping {2}, a take cues looping {3}",
                    FigureCues.Of(null).Act,
                    FigureCue.Walking.Act,
                    FigureCue.Walking.Loops,
                    FigureCue.Within(FigureAct.Take, Take.Seconds).Loops));

            report.Append("\n  beats: ").Append(string.Join("; ", rows.ToArray()));

            return failures;
        }

        static int Fits(
            WorldModels models,
            StringBuilder report,
            PartModel worn,
            FigureCue cue,
            float beat,
            string leg)
        {
            var clip = models.ClipOf(worn, cue.Clip);
            var seconds = clip == null ? 0f : clip.length;

            return Assert(
                report,
                clip != null
                && Math.Abs(cue.Beat - beat) <= Epsilon
                && cue.EndsWithin(seconds)
                && Math.Abs(cue.TimeIn(seconds, beat) - seconds) <= Epsilon,
                "the " + leg + " clip ends inside the beat the pure logic already defines, which is the clip "
                + "being cut to the beat rather than the beat to the clip",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} runs {1:0.###}s and is played at {2:0.###}x over the beat's own {3:0.###}s, reaching "
                    + "{4:0.###}s of the clip by the last frame of the beat",
                    cue.Clip,
                    seconds,
                    cue.SpeedIn(seconds),
                    beat,
                    cue.TimeIn(seconds, beat)));
        }

        static int Degrades(WorldModels models, StringBuilder report)
        {
            var meshes = Rigged();
            var warnings = 0;
            Application.LogCallback watch = (message, trace, type) =>
            {
                if (type == LogType.Warning && message.IndexOf(AbsentClip, StringComparison.Ordinal) >= 0)
                {
                    warnings++;
                }
            };

            Application.logMessageReceived += watch;

            var nothing = 0;
            try
            {
                foreach (var mesh in meshes)
                {
                    var first = models.ClipOf(mesh, AbsentClip);
                    var second = models.ClipOf(mesh, AbsentClip);

                    if (first == null && second == null)
                    {
                        nothing++;
                    }
                }
            }
            finally
            {
                Application.logMessageReceived -= watch;
            }

            return Assert(
                report,
                meshes.Count > 0 && nothing == meshes.Count && warnings == meshes.Count,
                "a clip the pack does not carry resolves to nothing and warns once per mesh rather than once a "
                + "frame, on every mesh the cast wears rather than the player's alone, so any figure that wants "
                + "it holds its static pose instead of throwing",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "two lookups of {0} on each of the {1} rigged meshes all resolved to nothing and the log "
                    + "carried {2} warning{3}",
                    AbsentClip,
                    meshes.Count,
                    warnings,
                    warnings == 1 ? string.Empty : "s"));
        }

        static int TheWholeCastCarriesTheSameClips(WorldModels models, StringBuilder report)
        {
            var meshes = Rigged();
            var complete = 0;
            var complaint = new List<string>();
            var census = new List<string>();

            foreach (var mesh in meshes)
            {
                var missing = new List<string>();

                foreach (var name in AdventurerClips.Names)
                {
                    var clip = models.ClipOf(mesh, name);

                    if (clip == null || clip.length <= 0f)
                    {
                        missing.Add(name);
                    }
                }

                if (missing.Count == 0 && models.ClipCountOf(mesh) == AdventurerClips.Count)
                {
                    complete++;
                }
                else if (complaint.Count < 6)
                {
                    complaint.Add(mesh + " is missing " + string.Join(", ", missing.ToArray()));
                }

                census.Add(mesh + " " + models.ClipCountOf(mesh));
            }

            return Assert(
                report,
                meshes.Count > 0 && complete == meshes.Count,
                "every rigged mesh in the cast, the four skeletons as well as the player's knight, carries "
                + "exactly the clips the pure table names, so an enemy animates off the same table the player "
                + "does with no branch per pack",
                complete + " of " + meshes.Count + " do (" + string.Join(", ", census.ToArray()) + ")"
                + (complaint.Count == 0 ? string.Empty : "; " + string.Join("; ", complaint.ToArray())));
        }

        static int Answered(WorldModels models, StringBuilder report)
        {
            var meshes = Rigged();
            var pairs = 0;
            var fitted = 0;
            var mirrored = 0;
            var complaint = new List<string>();
            var rows = new List<string>();

            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                var blow = FigureCues.Striking(fight);
                var reply = FigureCues.Answering(fight);

                if (reply.Act == FigureCues.Answered(blow.Act)
                    && Math.Abs(reply.Beat - fight.Seconds) <= Epsilon
                    && (reply.Act == FigureAct.Clash) == (blow.Act == FigureAct.Clash))
                {
                    mirrored++;
                }

                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "a {0} is {1} on the player and {2} on the enemy over the same {3:0.###}s",
                    outcome,
                    blow.Act,
                    reply.Act,
                    fight.Seconds));

                foreach (var mesh in meshes)
                {
                    pairs++;
                    var clip = models.ClipOf(mesh, reply.Clip);
                    var seconds = clip == null ? 0f : clip.length;

                    if (clip != null
                        && reply.EndsWithin(seconds)
                        && Math.Abs(reply.TimeIn(seconds, fight.Seconds) - seconds) <= Epsilon)
                    {
                        fitted++;
                    }
                    else if (complaint.Count < 6)
                    {
                        complaint.Add(
                            mesh + " answering a " + outcome + " with " + reply.Clip + " runs "
                            + seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s against a beat of "
                            + fight.Seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s");
                    }
                }
            }

            var failures = Assert(
                report,
                mirrored == FoughtOutcomes.Length,
                "the enemy's cue is the mirror of the player's over the same beat - a win is answered by a "
                + "recoil, a loss by a strike, a tie by a clash on both sides - and that mirroring is a pure "
                + "decision the Unity side only plays",
                mirrored + " of " + FoughtOutcomes.Length + " outcomes mirror: "
                + string.Join("; ", rows.ToArray()));

            failures += Assert(
                report,
                pairs > 0 && fitted == pairs,
                "every answering clip ends inside the fight's own beat on every mesh in the cast, which is the "
                + "clip being cut to the beat rather than the beat to the clip",
                fitted + " of " + pairs + " mesh and outcome pairs do"
                + (complaint.Count == 0 ? string.Empty : "; " + string.Join("; ", complaint.ToArray())));

            return failures;
        }

        static List<PartModel> Rigged()
        {
            var meshes = new List<PartModel>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (ArtPacks.IsRigged(model))
                {
                    meshes.Add(model);
                }
            }

            return meshes;
        }

        static int Walked(WorldModels models, StringBuilder report)
        {
            var worn = PartModels.Of(PartStyle.Start);
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            PreviewFilm.Sun();

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);

            var figure = builder.Player;
            var driven = figure == null ? null : figure.GetComponent<FigureAnimator>();
            var failures = 0;

            failures += Assert(
                report,
                driven != null && driven.IsRigged && driven.HasClipsToPlay,
                "the world raises an animator on the player figure and it has a clip loaded before a step is taken",
                driven == null
                    ? "the player carries no animator"
                    : "rigged " + driven.IsRigged + ", playing "
                        + (driven.Playing == null ? "nothing" : driven.Playing.name) + " as " + driven.Act);

            if (driven == null || figure == null)
            {
                WorldObjects.Destroy(root);
                WorldObjects.Destroy(rig.gameObject);
                builder.Dispose();
                return failures;
            }

            var animator = figure.GetComponentInChildren<Animator>(true);

            failures += Assert(
                report,
                animator != null && !animator.applyRootMotion && animator.runtimeAnimatorController == null
                && animator.cullingMode == AnimatorCullingMode.AlwaysAnimate,
                "the rig applies no root motion and carries no controller, so a clip can never say where the "
                + "figure stands or when a beat is over, and it is never culled, so what it plays never "
                + "depends on who is looking",
                animator == null
                    ? "there is no animator"
                    : "applyRootMotion " + animator.applyRootMotion + ", controller "
                        + (animator.runtimeAnimatorController == null ? "none" : "one")
                        + ", culling " + animator.cullingMode);

            failures += Assert(
                report,
                driven.Act == FigureAct.Idle,
                "a figure nobody has asked to move stands on its idle clip",
                "it plays " + driven.Act + " as " + (driven.Playing == null ? "nothing" : driven.Playing.name));

            PreviewFilm.Warm(lens);

            var joints = Joints(figure.transform);
            var idleLoop = Math.Max(2, (int)(driven.PlayingSeconds / Frame));
            var strayed = Strayed(joints, models.Of(worn));

            Step(rig, builder, walker, driven);

            var standing = Pose(joints);
            var posed = Strayed(joints, models.Of(worn));
            var idleSpread = 0f;

            for (var frame = 1; frame < idleLoop; frame++)
            {
                Step(rig, builder, walker, driven);

                idleSpread = Math.Max(idleSpread, Spread(joints, standing));
            }

            var pose = Pose(joints);

            failures += Assert(
                report,
                driven.HasClipsToPlay && strayed <= Epsilon && posed > StirFloor,
                "the world hands back a figure with its clip loaded but not yet sampled, so it stands in the "
                + "pose the import gave it until something advances it, which is the pose the pack's pinned "
                + "fit was measured from",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it carried {0} loaded and its {1} joints sat {2:0.####} degrees off the imported asset's "
                    + "own pose before a step, and {3:0.##} degrees off it after one",
                    driven.Playing == null ? "nothing" : driven.Playing.name,
                    joints.Length,
                    strayed,
                    posed));

            failures += Assert(
                report,
                idleSpread > StirFloor,
                "the idle clip runs rather than freezing, so a figure standing still is still alive",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "one whole {0:0.###}s idle loop of {1} frames carried its {2} joints {3:0.##} degrees away "
                    + "from the pose it opened on, measured against that opening pose because an idle breathes "
                    + "far too slowly for a frame-to-frame angle to survive single precision",
                    driven.PlayingSeconds,
                    idleLoop,
                    joints.Length,
                    idleSpread));

            failures += FellBack(rig, builder, walker, driven, report);

            var walkFrames = 0;
            var walkStir = 0f;
            var walkActs = new HashSet<FigureAct>();
            var settleActs = new HashSet<FigureAct>();
            var drift = 0f;
            var shot = 0;
            var shots = new List<string>();
            var beats = new List<string>();
            var overrun = new List<string>();
            var unfinished = new List<string>();
            var moves = 0;

            var walkOpening = Pose(joints);
            var widest = new float[joints.Length];
            var close = PreviewFilm.Rig(Vector3.zero, CloseRange, CloseFraming);
            Aim(close, figure.transform.position);
            PreviewFilm.Warm(close);

            for (var move = 0; move < Moves && !walker.Run.IsLevelComplete; move++)
            {
                var target = Furthest(walker.Run);
                if (target == TapAim.Nothing)
                {
                    break;
                }

                walker.WalkTo(target);
                if (!walker.IsWalking)
                {
                    break;
                }

                moves++;

                var beatAct = FigureAct.Idle;
                var beatFrames = 0;
                var beatFill = 0f;

                for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
                {
                    Step(rig, builder, walker, driven);

                    var moving = !walker.Walk.IsSettled && !walker.Walk.IsWaiting;
                    var stir = Stir(joints, ref pose);

                    if (moving)
                    {
                        for (var slot = 0; slot < joints.Length; slot++)
                        {
                            widest[slot] = Math.Max(
                                widest[slot],
                                Quaternion.Angle(walkOpening[slot], joints[slot].localRotation));
                        }

                        var here = figure.Ground;
                        var wanted = walker.Walk.Position;
                        drift = Math.Max(
                            drift,
                            Math.Max(
                                Math.Abs(here.X - wanted.X),
                                Math.Max(Math.Abs(here.Y - wanted.Y), Math.Abs(here.Z - wanted.Z))));

                        walkFrames++;
                        walkStir += stir;
                        walkActs.Add(driven.Act);

                        if (shot < Sequence && walkFrames % EveryNthFrame == 0)
                        {
                            var path = FramePrefix + shot.ToString("00", CultureInfo.InvariantCulture) + ".png";
                            Aim(close, figure.transform.position);
                            PreviewFilm.Shoot(close, path);
                            shots.Add(string.Format(
                                CultureInfo.InvariantCulture,
                                "{0:00} at {1:0.##} tiles on {2} {3:0.###}s in",
                                shot,
                                walker.Walk.Travelled,
                                driven.Act,
                                driven.PlayingTime));
                            shot++;
                        }
                    }

                    if (driven.Act != FigureAct.Idle && driven.Act != FigureAct.Walk
                        && driven.Act != FigureAct.Retreat)
                    {
                        if (driven.Act != beatAct)
                        {
                            beatAct = driven.Act;
                            beatFrames = 0;
                        }

                        beatFrames++;
                        beatFill = driven.PlayingSeconds <= 0f
                            ? 0f
                            : driven.PlayingTime / driven.PlayingSeconds;
                    }
                    else if (beatAct != FigureAct.Idle)
                    {
                        var held = beatFrames * Frame;
                        var allowed = BeatOf(beatAct);
                        var lastFrame = allowed <= 0f ? 1f : Frame / allowed;

                        beats.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} held {1} frames ({2:0.###}s of {3:0.###}s) and reached {4:0.###} of its clip",
                            beatAct,
                            beatFrames,
                            held,
                            allowed,
                            beatFill));

                        if (held > allowed + Frame + Epsilon)
                        {
                            overrun.Add(beatAct + " ran " + held.ToString("0.###", CultureInfo.InvariantCulture)
                                + "s over a beat of " + allowed.ToString("0.###", CultureInfo.InvariantCulture)
                                + "s");
                        }

                        if (beatFill < 1f - lastFrame - 1e-3f)
                        {
                            unfinished.Add(beatAct + " only reached "
                                + beatFill.ToString("0.###", CultureInfo.InvariantCulture)
                                + " of its clip, more than the "
                                + lastFrame.ToString("0.###", CultureInfo.InvariantCulture)
                                + " one frame of the beat is worth short of the end");
                        }

                        beatAct = FigureAct.Idle;
                        beatFrames = 0;
                    }
                }

                for (var frame = 0; frame < 60; frame++)
                {
                    Step(rig, builder, walker, driven);
                    settleActs.Add(driven.Act);
                }
            }

            failures += Assert(
                report,
                moves > 0 && walkFrames > 0 && walkActs.Count == 1 && walkActs.Contains(FigureAct.Walk),
                "a figure walking between tiles plays the walk clip on every frame it is moving forward",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} moves over {1} walking frames, all of them on {2}",
                    moves,
                    walkFrames,
                    string.Join("/", Names(walkActs))));

            failures += Assert(
                report,
                walkFrames > 0 && walkStir > StirFloor * walkFrames,
                "its joints turn while it walks, so it moves instead of sliding",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} walking frames turned its joints {1:0.##} degrees in total, {2:0.###} a frame",
                    walkFrames,
                    walkStir,
                    walkFrames == 0 ? 0f : walkStir / walkFrames));

            failures += Assert(
                report,
                settleActs.Count == 1 && settleActs.Contains(FigureAct.Idle),
                "it returns to the idle clip once it stops",
                "the frames after each walk played " + string.Join("/", Names(settleActs)));

            failures += Assert(
                report,
                drift <= Epsilon,
                "the figure stands exactly where the pure walk puts it on every animated walking frame, so no "
                + "clip became a second source of truth about where it is",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the worst drift over {0} walking frames was {1:0.#######}",
                    walkFrames,
                    drift));

            failures += Assert(
                report,
                beats.Count > 0 && overrun.Count == 0 && unfinished.Count == 0,
                "every encounter and pickup beat the run threw up played a clip that ran out inside that beat, "
                + "to within the last frame of it, and handed the figure back to idle",
                beats.Count == 0
                    ? "the run threw up no beat to play"
                    : beats.Count + " beats: " + string.Join("; ", beats.ToArray())
                        + (overrun.Count == 0 ? string.Empty : "; overran: " + string.Join("; ", overrun.ToArray()))
                        + (unfinished.Count == 0
                            ? string.Empty
                            : "; unfinished: " + string.Join("; ", unfinished.ToArray())));

            failures += Assert(
                report,
                shot == Sequence,
                "the walk is photographed as a frame sequence a human can read the stride off",
                shot + " of " + Sequence + " frames, " + FramePrefix + "NN.png: "
                + string.Join(", ", shots.ToArray()));

            var swung = 0;
            var swungWidest = 0f;

            foreach (var turned in widest)
            {
                if (turned > LimbTurn)
                {
                    swung++;
                }

                swungWidest = Math.Max(swungWidest, turned);
            }

            failures += Assert(
                report,
                swung >= Limbs,
                "the walk turns the whole rig rather than one bone of it, so the figure swings its arms as "
                + "well as its legs instead of skating along in the pose the import gave it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of its {1} joints turned more than {2:0.#} degrees off the pose it set out in, the "
                    + "widest by {3:0.#} degrees, against the {4} joints this asks for",
                    swung,
                    joints.Length,
                    LimbTurn,
                    swungWidest,
                    Limbs));

            report.Append(Cost(rig, builder, walker, figure, driven));

            WorldObjects.Destroy(close.gameObject);
            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return failures;
        }

        static int FellBack(
            CameraRig rig,
            WorldBuilder builder,
            Walker walker,
            FigureAnimator driven,
            StringBuilder report)
        {
            var target = Furthest(walker.Run);
            if (target == TapAim.Nothing)
            {
                return Assert(
                    report, false, "a figure falling back plays the backwards walk", "nothing was reachable");
            }

            walker.WalkTo(target);

            for (var frame = 0; frame < 8 && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker, driven);
            }

            walker.Cancel();

            var acts = new HashSet<FigureAct>();
            var frames = 0;

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker, driven);

                if (walker.Walk.IsRetreating && !walker.Walk.IsSettled)
                {
                    acts.Add(driven.Act);
                    frames++;
                }
            }

            for (var frame = 0; frame < 30; frame++)
            {
                Step(rig, builder, walker, driven);
            }

            var back = driven.Act;

            return Assert(
                report,
                frames > 0 && acts.Count == 1 && acts.Contains(FigureAct.Retreat) && back == FigureAct.Idle,
                "a figure falling back to the tile it came from plays the backwards walk rather than striding "
                + "forwards while it slides the other way",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} falling-back frames played {1}, and it stood on {2} afterwards",
                    frames,
                    string.Join("/", Names(acts)),
                    back));
        }

        static float BeatOf(FigureAct act)
        {
            switch (act)
            {
                case FigureAct.Strike:
                    return Fight.Of(ActionOutcome.Win).Seconds;
                case FigureAct.Clash:
                    return Fight.Of(ActionOutcome.Tie).Seconds;
                case FigureAct.Recoil:
                    return Fight.Of(ActionOutcome.Loss).Seconds;
                case FigureAct.Take:
                    return Take.Seconds;
                default:
                    return 0f;
            }
        }

        static string Cost(
            CameraRig rig, WorldBuilder builder, Walker walker, PlayerFigure figure, FigureAnimator driven)
        {
            var sampled = new double[CostRounds];
            var unsampled = new double[CostRounds];

            for (var round = 0; round < CostRounds; round++)
            {
                sampled[round] = Milliseconds(rig, builder, walker, driven);
                unsampled[round] = Milliseconds(rig, builder, walker, null);
            }

            var skins = figure.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bones = 0;
            var vertices = 0;

            foreach (var skin in skins)
            {
                bones += skin.bones == null ? 0 : skin.bones.Length;
                vertices += skin.sharedMesh == null ? 0 : skin.sharedMesh.vertexCount;
            }

            WorldObjects.Destroy(driven);

            var bare = new double[CostRounds];

            for (var round = 0; round < CostRounds; round++)
            {
                bare[round] = Milliseconds(rig, builder, walker, null);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  frame cost, {0} rounds of {1} frames each, alternated so drift is shared: advancing and "
                + "sampling the figure costs {2} against {3} for the same figure left unsampled and {4} for "
                + "the pre-animation figure carrying no animator at all, so the animation adds about "
                + "{5:0.####} ms of processor work a frame to a 16.667 ms budget"
                + "\n  what it skins: {6} skinned meshes over {7} bone bindings and {8} vertices"
                + "\n  what this does not prove: no Android device is attached to this machine, so these are "
                + "editor numbers on a desktop processor. They bound the per-frame work the animation adds "
                + "on the CPU and nothing else - they are not a device frame rate, they say nothing about "
                + "the device's own skinning path, and the pre-animation figure they are measured against is "
                + "the same skinned mesh T-32 already shipped rather than the primitive capsule before it",
                CostRounds,
                CostFrames,
                Spans(sampled),
                Spans(unsampled),
                Spans(bare),
                Mean(sampled) - Mean(bare),
                skins.Length,
                bones,
                vertices);
        }

        static string Spans(double[] rounds)
        {
            var lowest = double.MaxValue;
            var highest = double.MinValue;

            foreach (var round in rounds)
            {
                lowest = Math.Min(lowest, round);
                highest = Math.Max(highest, round);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.####} ms a frame ({1:0.####} to {2:0.####})",
                Mean(rounds),
                lowest,
                highest);
        }

        static double Mean(double[] rounds)
        {
            var total = 0d;

            foreach (var round in rounds)
            {
                total += round;
            }

            return rounds.Length == 0 ? 0d : total / rounds.Length;
        }

        static double Milliseconds(
            CameraRig rig, WorldBuilder builder, Walker walker, FigureAnimator driven)
        {
            var clock = Stopwatch.StartNew();

            for (var frame = 0; frame < CostFrames; frame++)
            {
                Step(rig, builder, walker, driven);
            }

            clock.Stop();

            return clock.Elapsed.TotalMilliseconds / CostFrames;
        }

        static Transform[] Joints(Transform figure)
        {
            var joints = new List<Transform>();

            foreach (var node in figure.GetComponentsInChildren<Transform>(true))
            {
                if (!ReferenceEquals(node, figure))
                {
                    joints.Add(node);
                }
            }

            return joints.ToArray();
        }

        static Quaternion[] Pose(Transform[] joints)
        {
            var pose = new Quaternion[joints.Length];

            for (var slot = 0; slot < joints.Length; slot++)
            {
                pose[slot] = joints[slot].localRotation;
            }

            return pose;
        }

        static float Strayed(Transform[] joints, GameObject asset)
        {
            if (asset == null)
            {
                return float.MaxValue;
            }

            var imported = new Dictionary<string, Quaternion>(StringComparer.Ordinal);

            foreach (var node in asset.GetComponentsInChildren<Transform>(true))
            {
                imported[node.name] = node.localRotation;
            }

            var turned = 0f;

            foreach (var joint in joints)
            {
                Quaternion authored;
                if (imported.TryGetValue(joint.name, out authored))
                {
                    turned += Quaternion.Angle(authored, joint.localRotation);
                }
            }

            return turned;
        }

        static void Aim(Camera lens, Vector3 centre)
        {
            lens.transform.position = centre - lens.transform.forward * CloseRange;
        }

        static float Spread(Transform[] joints, Quaternion[] opening)
        {
            var turned = 0f;

            for (var slot = 0; slot < joints.Length; slot++)
            {
                turned += Quaternion.Angle(opening[slot], joints[slot].localRotation);
            }

            return turned;
        }

        static float Stir(Transform[] joints, ref Quaternion[] pose)
        {
            var turned = 0f;

            for (var slot = 0; slot < joints.Length; slot++)
            {
                turned += Quaternion.Angle(pose[slot], joints[slot].localRotation);
            }

            pose = Pose(joints);

            return turned;
        }

        static void Step(CameraRig rig, WorldBuilder builder, Walker walker, FigureAnimator driven)
        {
            walker.Advance(Frame);
            rig.Advance(Frame);
            builder.Floor.Advance(Frame);
            builder.Pickups.Advance(Frame);

            if (builder.PlayerBadge != null)
            {
                builder.PlayerBadge.Advance(Frame);
            }

            if (driven != null)
            {
                driven.Advance(Frame);
            }
        }

        static int Furthest(RunState state)
        {
            var furthest = TapAim.Nothing;
            var doomed = TapAim.Nothing;
            var steps = 0;
            var doomedSteps = 0;

            foreach (var nodeId in TapAim.Aimable(state))
            {
                var resolved = ActionResolver.Resolve(state, nodeId);
                if (resolved.Outcome == ActionOutcome.Rejected)
                {
                    continue;
                }

                if (resolved.State.ConsumedNodes.Count == state.ConsumedNodes.Count)
                {
                    if (resolved.Route.Count > doomedSteps)
                    {
                        doomed = nodeId;
                        doomedSteps = resolved.Route.Count;
                    }

                    continue;
                }

                if (resolved.Route.Count > steps)
                {
                    furthest = nodeId;
                    steps = resolved.Route.Count;
                }
            }

            return furthest != TapAim.Nothing ? furthest : doomed;
        }

        static string[] Names(HashSet<FigureAct> acts)
        {
            var names = new List<string>();

            foreach (var act in acts)
            {
                names.Add(act.ToString());
            }

            names.Sort(StringComparer.Ordinal);

            return names.Count == 0 ? new[] { "nothing" } : names.ToArray();
        }

        static int Assert(StringBuilder report, bool held, string claim, string detail)
        {
            report.Append("\n  ").Append(held ? "ok   " : "FAIL ").Append(claim).Append(" - ").Append(detail);

            return held ? 0 : 1;
        }
    }
}
