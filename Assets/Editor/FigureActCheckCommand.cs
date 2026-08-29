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

        const int DriftRuns = 200;

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

        const int SwungLimbs = 12;

        const int GuardLimbs = 4;

        const string BaseMap = "_BaseMap";

        const string BaseColour = "_BaseColor";

        const float YawEpsilon = 0.5f;

        const int SettleFrames = 20;

        const PlayerWeapon Gripped = PlayerWeapon.Axe;

        static readonly ActionOutcome[] FoughtOutcomes =
        {
            ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss
        };

        static readonly PlayerWeapon[] Grips =
        {
            PlayerWeapon.None,
            PlayerWeapon.Shortsword,
            PlayerWeapon.Axe,
            PlayerWeapon.Spear,
            PlayerWeapon.Greatsword
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
                failures += Executed(models, report);
                failures += Moved(models, report);
                failures += Degrades(models, report);
                failures += Walked(models, report);
                failures += Faced(report);
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
                if (model == PartModel.None || ArtPacks.ShipsWithTheCast(model))
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
                var landing = fight.Advanced(fight.ContactAt);
                var blow = FigureCues.Striking(landing, Gripped);
                var reply = FigureCues.Answering(landing);

                failures += Fits(models, report, worn, blow, blow.Beat, outcome + " fight, the player");
                failures += Fits(models, report, worn, reply, reply.Beat, outcome + " fight, the enemy");

                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} lands {1:0.###}s into a {2:0.###}s fight, the player on {3} cut to {4:0.###}s "
                    + "and the enemy on {5} cut to {6:0.###}s",
                    outcome,
                    fight.ContactAt,
                    fight.Seconds,
                    blow.Act,
                    blow.Beat,
                    reply.Act,
                    reply.Beat));
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
                FigureCues.Of(null, Gripped).Act == FigureAct.Idle
                && FigureCue.Still.Loops
                && FigureCue.Walking.Act == FigureAct.Walk
                && FigureCue.Walking.Loops
                && FigureCue.Looping(FigureAct.Retreat).Loops
                && !FigureCue.Within(FigureAct.Take, Take.Seconds).Loops,
                "the standing, walking and falling-back cues loop while every cue cut to a beat does not",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "no journey cues {0}, walking cues {1} looping {2}, a take cues looping {3}",
                    FigureCues.Of(null, Gripped).Act,
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

        static int Executed(WorldModels models, StringBuilder report)
        {
            var win = Fight.Of(ActionOutcome.Win);

            var failures = Assert(
                report,
                Math.Abs(VictoryStages.SecondsOf(VictoryStage.Clash) - 0.9f) <= Epsilon
                && Math.Abs(VictoryStages.SecondsOf(VictoryStage.Dissolve) - 0.3f) <= Epsilon
                && VictoryStages.BlocksInput(VictoryStage.Clash)
                && VictoryStages.BlocksInput(VictoryStage.Dissolve)
                && !VictoryStages.BlocksInput(VictoryStage.Done)
                && Math.Abs(VictoryStages.BlockingSeconds - 1.2f) <= Epsilon
                && Math.Abs(win.Seconds - VictoryStages.BlockingSeconds) <= Epsilon,
                "the victory names a clash and a dissolve, each with its own duration and each holding the "
                + "controls, and a won fight is held for exactly the two of them and nothing more",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "clash {0:0.###}s holding {1}, dissolve {2:0.###}s holding {3}, {4:0.###}s of held "
                    + "movement against a won fight of {5:0.###}s",
                    VictoryStages.SecondsOf(VictoryStage.Clash),
                    VictoryStages.BlocksInput(VictoryStage.Clash),
                    VictoryStages.SecondsOf(VictoryStage.Dissolve),
                    VictoryStages.BlocksInput(VictoryStage.Dissolve),
                    VictoryStages.BlockingSeconds,
                    win.Seconds));

            var blows = new List<FigureAct>();
            var replies = new List<FigureAct>();
            var struck = FigureMotion.Still;
            var restarts = 0;
            var counters = 0;

            for (var frame = 0; frame * Frame < VictoryStages.ClashSeconds; frame++)
            {
                var playing = win.Advanced(frame * Frame);
                var blow = FigureCues.Striking(playing, Gripped);
                var reply = FigureCues.Answering(playing);

                struck = struck.Cued(blow);

                if (frame > 0 && struck.Elapsed == 0f)
                {
                    restarts++;
                }

                if (IsBlow(reply.Act))
                {
                    counters++;
                }

                if (blows.Count == 0 || blows[blows.Count - 1] != blow.Act)
                {
                    blows.Add(blow.Act);
                }

                if (replies.Count == 0 || replies[replies.Count - 1] != reply.Act)
                {
                    replies.Add(reply.Act);
                }

                struck = struck.Advanced(Frame);
            }

            failures += Assert(
                report,
                restarts == 0
                && counters == 0
                && blows.Count == 1
                && blows[0] == FigureCues.FinisherOf(Gripped)
                && replies.Count == 2
                && replies[0] == FigureAct.Idle
                && replies[1] == FigureAct.Fall,
                "a won clash is one finisher the player throws and never restarts, and the enemy answers it "
                + "with no blow of its own - it stands until the blow lands and then falls",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "over {0:0.###}s the player played {1} restarting {2} times while the enemy played {3}, "
                    + "throwing {4} blows back",
                    VictoryStages.ClashSeconds,
                    string.Join("/", Named(blows)),
                    restarts,
                    string.Join("/", Named(replies)),
                    counters));

            var loss = Fight.Of(ActionOutcome.Loss);
            var floored = new List<FigureAct>();
            var swung = new List<FigureAct>();

            for (var frame = 0; frame * Frame < loss.Seconds; frame++)
            {
                var playing = loss.Advanced(frame * Frame);
                var blow = FigureCues.Striking(playing, Gripped);
                var reply = FigureCues.Answering(playing);

                if (floored.Count == 0 || floored[floored.Count - 1] != blow.Act)
                {
                    floored.Add(blow.Act);
                }

                if (swung.Count == 0 || swung[swung.Count - 1] != reply.Act)
                {
                    swung.Add(reply.Act);
                }
            }

            failures += Assert(
                report,
                floored.Count == 2
                && floored[0] == FigureAct.Idle
                && floored[1] == FigureAct.Fall
                && swung.Count == 1
                && swung[0] == FigureAct.Strike,
                "a lost fight is the enemy's blow landing on the player, and the player answers it with "
                + "neither a block nor a hit reaction - it goes down",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "over {0:0.###}s the player played {1} while the enemy played {2}",
                    loss.Seconds,
                    string.Join("/", Named(floored)),
                    string.Join("/", Named(swung))));

            var tie = Fight.Of(ActionOutcome.Tie);

            failures += Assert(
                report,
                FigureCues.Striking(tie, Gripped).Act == FigureAct.Clash
                && FigureCues.Answering(tie).Act == FigureAct.Clash
                && Math.Abs(FigureCues.Striking(tie, Gripped).Beat - tie.BlowBeat) <= Epsilon
                && Math.Abs(FigureCues.Answering(tie).Beat - tie.BlowBeat) <= Epsilon,
                "a tie is the one outcome still traded, because neither number beat the other and neither "
                + "figure falls",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the player plays {0} and the enemy plays {1} over the same {2:0.###}s",
                    FigureCues.Striking(tie, Gripped).Act,
                    FigureCues.Answering(tie).Act,
                    tie.BlowBeat));

            var meshes = Rigged();
            var cues = new List<FigureCue>();

            foreach (var weapon in Grips)
            {
                cues.Add(FigureCues.Striking(win.Advanced(win.ContactAt), weapon));
            }

            cues.Add(FigureCues.Answering(win.Advanced(win.ContactAt)));
            cues.Add(FigureCues.Striking(loss.Advanced(loss.ContactAt), Gripped));
            cues.Add(FigureCues.Answering(loss));
            cues.Add(FigureCues.Striking(tie, Gripped));
            cues.Add(FigureCues.Answering(tie));

            var pairs = 0;
            var fitted = 0;
            var complaint = new List<string>();

            foreach (var mesh in meshes)
            {
                foreach (var cue in cues)
                {
                    pairs++;
                    var fits = Cued(models, mesh, cue);
                    fitted += fits;

                    if (fits == 0 && complaint.Count < 6)
                    {
                        var clip = models.ClipOf(mesh, cue.Clip);

                        complaint.Add(
                            mesh + " playing " + cue.Clip + " runs "
                            + (clip == null ? 0f : clip.length).ToString("0.###", CultureInfo.InvariantCulture)
                            + "s against a beat of "
                            + cue.Beat.ToString("0.###", CultureInfo.InvariantCulture) + "s");
                    }
                }
            }

            failures += Assert(
                report,
                pairs > 0 && fitted == pairs,
                "every clip either side of a fight can be cued - a finisher for each of the "
                + Grips.Length + " grips, the loser's death, the enemy's blow and the tie's clash - ends "
                + "inside its own beat on every mesh the cast wears",
                fitted + " of " + pairs + " mesh and cue pairs do"
                + (complaint.Count == 0 ? string.Empty : "; " + string.Join("; ", complaint.ToArray())));

            var solid = true;
            for (var at = 0f; at < VictoryStages.ClashSeconds; at += Frame)
            {
                var during = win.Advanced(at);
                solid &= during.Fade >= 1f && FigureCues.Striking(during, Gripped).Act != FigureAct.Idle;
            }

            var opening = win.Advanced(VictoryStages.ClashSeconds);
            var closing = win.Advanced(VictoryStages.BlockingSeconds - Frame);
            var gone = win.Advanced(VictoryStages.BlockingSeconds);

            failures += Assert(
                report,
                solid
                && !opening.IsExecuting
                && FigureCues.Striking(opening, Gripped).Equals(FigureCue.Still)
                && FigureCues.Answering(opening).Act == FigureAct.Fall
                && FigureCues.Answering(gone).Equals(FigureCue.Still)
                && closing.Fade < 0.1f
                && closing.Fade > 0f
                && gone.Fade <= 0f,
                "the enemy holds its own skin for the whole clash and only then fades out over the "
                + "dissolve, which is the stage that stands the player down and leaves the enemy falling",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the enemy reads at {0:0.###} through the clash, {1:0.###} as the dissolve opens, "
                    + "{2:0.###} a frame from its end and {3:0.###} at the end, playing {4} over it",
                    solid ? 1f : 0f,
                    opening.Fade,
                    closing.Fade,
                    gone.Fade,
                    FigureCues.Answering(opening).Act));

            var clock = 0d;
            var carried = 0f;
            var shortest = float.MaxValue;
            var longest = 0f;

            for (var run = 0; run < DriftRuns; run++)
            {
                var contact = clock - carried;
                var fight = Fight.Of(ActionOutcome.Win).Advanced(carried);
                var delta = Frame * (1f + 0.25f * (run % 5 - 2));

                for (var frame = 0; frame < FrameCap && !fight.IsSettled; frame++)
                {
                    fight = fight.Advanced(delta);
                    clock += delta;
                }

                carried = fight.Timeline.Overrun;

                var span = (float)(clock - carried - contact);
                shortest = span < shortest ? span : shortest;
                longest = span > longest ? span : longest;
            }

            failures += Assert(
                report,
                longest - shortest <= 0.001f
                && Math.Abs(shortest - VictoryStages.BlockingSeconds) <= 0.001f,
                "a run of " + DriftRuns + " fights back to back accumulates no drift, so the last one holds "
                + "the controls for the same span the first one did",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "held between {0:0.#####}s and {1:0.#####}s against the {2:0.###}s the stages add up to",
                    shortest,
                    longest,
                    VictoryStages.BlockingSeconds));

            return failures;
        }

        static bool IsBlow(FigureAct act)
        {
            if (act == FigureAct.Strike || act == FigureAct.Clash || act == FigureAct.Recoil)
            {
                return true;
            }

            foreach (var weapon in Grips)
            {
                if (FigureCues.FinisherOf(weapon) == act)
                {
                    return true;
                }
            }

            return false;
        }

        static int Moved(WorldModels models, StringBuilder report)
        {
            var worn = PartModels.Of(PartStyle.Start);
            var asset = models.Of(worn);

            if (asset == null)
            {
                return Assert(
                    report, false, "the cast's own mesh loads to be animated", worn + " loaded nothing");
            }

            var stand = UnityEngine.Object.Instantiate(asset);
            var driven = FigureAnimator.Raise(stand, worn, models);

            if (driven == null)
            {
                WorldObjects.Destroy(stand);

                return Assert(
                    report, false, "the cast's own mesh carries a rig to animate", "it carries none");
            }

            var joints = Joints(stand.transform);
            var acts = new List<FigureAct>();

            foreach (var weapon in Grips)
            {
                acts.Add(FigureCues.FinisherOf(weapon));
            }

            acts.Add(FigureAct.Fall);
            acts.Add(FigureAct.Clash);

            var poses = new List<Quaternion[]>();
            var rows = new List<string>();
            var frozen = new List<string>();
            var guarded = 0;

            foreach (var act in acts)
            {
                var beat = VictoryStages.ClashSeconds;
                var cue = FigureCue.Within(act, beat);
                var frames = (int)(beat / Frame) - 1;

                driven.Cue(cue);
                driven.Advance(Frame);

                var opening = Pose(joints);
                var widest = new float[joints.Length];
                var halfway = opening;

                for (var frame = 1; frame <= frames; frame++)
                {
                    driven.Advance(Frame);

                    for (var slot = 0; slot < joints.Length; slot++)
                    {
                        widest[slot] = Math.Max(
                            widest[slot], Quaternion.Angle(opening[slot], joints[slot].localRotation));
                    }

                    if (frame == frames / 2)
                    {
                        halfway = Pose(joints);
                    }
                }

                var turned = 0;
                var deepest = 0f;

                foreach (var swung in widest)
                {
                    if (swung > LimbTurn)
                    {
                        turned++;
                    }

                    deepest = Math.Max(deepest, swung);
                }

                if (act == FigureAct.Clash)
                {
                    guarded = turned;
                }
                else if (turned < SwungLimbs)
                {
                    frozen.Add(act + " turned only " + turned + " joints");
                }

                poses.Add(halfway);
                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} on {1} swung {2} joints past {3:0.#} degrees, the widest by {4:0.#}",
                    act,
                    AdventurerClips.NameOf(act),
                    turned,
                    LimbTurn,
                    deepest));
            }

            var failures = Assert(
                report,
                frozen.Count == 0,
                "every clip either outcome plays - a finisher for each of the " + Grips.Length
                + " grips and the loser's death - turns at least " + SwungLimbs
                + " of the rig's own bones past " + LimbTurn + " degrees rather than sliding a figure "
                + "frozen in the pose the import gave it, measured as joint rotation off the clip's first "
                + "frame rather than off the clip's name",
                string.Join("; ", rows.ToArray())
                + (frozen.Count == 0 ? string.Empty : "; " + string.Join("; ", frozen.ToArray())));

            failures += Assert(
                report,
                guarded >= GuardLimbs,
                "the tie's block moves the rig too, though it is a guard rather than a swing and turns "
                + "fewer bones than any finisher does",
                guarded + " joints past " + LimbTurn + " degrees against the " + GuardLimbs
                + " a guard owes and the " + SwungLimbs + " a finisher owes");

            var closest = float.MaxValue;
            var nearest = string.Empty;

            for (var first = 0; first < Grips.Length; first++)
            {
                for (var second = first + 1; second < Grips.Length; second++)
                {
                    var apart = Apart(poses[first], poses[second]);

                    if (apart >= closest)
                    {
                        continue;
                    }

                    closest = apart;
                    nearest = Grips[first] + " against " + Grips[second];
                }
            }

            failures += Assert(
                report,
                closest > joints.Length,
                "the finisher differs across every pair of weapon tiers by the pose it puts the rig in "
                + "halfway through, not only by the clip it names",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the closest pair, {0}, still stands {1:0.#} degrees apart summed over {2} joints, "
                    + "against the {2} degrees this asks for",
                    nearest,
                    closest,
                    joints.Length));

            WorldObjects.Destroy(driven);
            WorldObjects.Destroy(stand);

            return failures;
        }

        static float Apart(Quaternion[] one, Quaternion[] other)
        {
            var turned = 0f;

            for (var slot = 0; slot < one.Length && slot < other.Length; slot++)
            {
                turned += Quaternion.Angle(one[slot], other[slot]);
            }

            return turned;
        }

        static int Cued(WorldModels models, PartModel mesh, FigureCue cue)
        {
            var clip = models.ClipOf(mesh, cue.Clip);
            var seconds = clip == null ? 0f : clip.length;

            return clip != null
                && cue.EndsWithin(seconds)
                && Math.Abs(cue.TimeIn(seconds, cue.Beat) - seconds) <= Epsilon
                ? 1
                : 0;
        }

        static string[] Named(List<FigureAct> acts)
        {
            var names = new string[acts.Count];

            for (var slot = 0; slot < acts.Count; slot++)
            {
                names[slot] = acts[slot].ToString();
            }

            return names;
        }

        static List<PartModel> Rigged()
        {
            var meshes = new List<PartModel>();

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (ArtPacks.IsRiggedCharacter(model))
                {
                    meshes.Add(model);
                }
            }

            return meshes;
        }

        static int NothingTintsTheCast(GameObject root, StringBuilder report)
        {
            var skins = 0;
            var atlassed = 0;
            var white = 0;
            var overridden = 0;
            var complaint = new List<string>();

            foreach (var cast in root.GetComponentsInChildren<Figure>(true))
            {
                foreach (var renderer in cast.GetComponentsInChildren<Renderer>(true))
                {
                    var material = renderer.sharedMaterial;

                    if (material == null
                        || !material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (renderer.HasPropertyBlock())
                    {
                        overridden++;
                    }

                    if (!(renderer is SkinnedMeshRenderer))
                    {
                        continue;
                    }

                    skins++;

                    if (material.HasProperty(BaseMap) && material.GetTexture(BaseMap) != null)
                    {
                        atlassed++;
                    }

                    if (!material.HasProperty(BaseColour) || material.GetColor(BaseColour) == Color.white)
                    {
                        white++;
                    }
                    else if (complaint.Count < 6)
                    {
                        complaint.Add(cast.name + " reads " + material.GetColor(BaseColour));
                    }
                }
            }

            var failures = 0;

            failures += Assert(
                report,
                skins > 0 && atlassed == skins,
                "every animated figure the world raised wears a material bound to its pack atlas, so the "
                + "mesh a clip poses is the mesh the pack drew",
                atlassed + " of " + skins + " do");

            failures += Assert(
                report,
                skins > 0 && white == skins && overridden == 0,
                "no flat colour sits over any of them, neither on the material nor in a property block, so "
                + "a pose reads against the pack's own texture",
                white + " of " + skins + " keep the atlas unmultiplied and " + overridden
                + " renderers carry a property block"
                + (complaint.Count == 0 ? "" : "; " + string.Join("; ", complaint.ToArray())));

            return failures;
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

            failures += NothingTintsTheCast(root, report);

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

                void Record()
                {
                    var held = beatFrames * Frame;
                    var allowed = BeatOf(beatAct, held);
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
                }

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

                    var playing = driven.Act;
                    var beating = playing != FigureAct.Idle && playing != FigureAct.Walk
                        && playing != FigureAct.Retreat;

                    if (beatAct != FigureAct.Idle && playing != beatAct)
                    {
                        Record();
                        beatAct = FigureAct.Idle;
                        beatFrames = 0;
                    }

                    if (beating)
                    {
                        if (beatAct == FigureAct.Idle)
                        {
                            beatAct = playing;
                            beatFrames = 0;
                        }

                        beatFrames++;
                        beatFill = driven.PlayingSeconds <= 0f
                            ? 0f
                            : driven.PlayingTime / driven.PlayingSeconds;
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

        static int Faced(StringBuilder report)
        {
            var worn = PartModels.Of(PartStyle.Start);
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);
            var figure = builder.Player;
            var driven = figure == null ? null : figure.GetComponent<FigureAnimator>();

            if (figure == null)
            {
                WorldObjects.Destroy(root);
                WorldObjects.Destroy(rig.gameObject);
                builder.Dispose();

                return Assert(report, false, "the world raises a player figure to turn", "it raised none");
            }

            var failures = Assert(
                report,
                Math.Abs(FigureFacing.Shortest(figure.RestYaw, ArtPacks.FacingOf(worn))) <= YawEpsilon
                && Math.Abs(FigureFacing.Shortest(FigureFacing.RestYaw, ArtPacks.FacingOf(worn)))
                    <= YawEpsilon,
                "the world still stands a figure on the pack's own authored offset, and that offset is the "
                + "yaw the heading back to the camera already carries, which is what a live yaw composes "
                + "with rather than replacing",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it was built on {0:0.###} against the pack's pinned {1:0.###} and the {2:0.###} the "
                    + "rest heading carries",
                    figure.RestYaw,
                    ArtPacks.FacingOf(worn),
                    FigureFacing.RestYaw));

            failures += TurnedToEveryCardinal(rig, builder, walker, driven, figure, worn, report);
            failures += TurnedAlongItsWalk(rig, builder, walker, driven, figure, report);
            failures += SquaredUpWithTheEnemy(rig, builder, walker, driven, figure, report);

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return failures;
        }

        static int TurnedToEveryCardinal(
            CameraRig rig,
            WorldBuilder builder,
            Walker walker,
            FigureAnimator driven,
            PlayerFigure figure,
            PartModel worn,
            StringBuilder report)
        {
            var cardinals = new List<string>();
            var off = 0f;
            var mirrored = 0f;
            var slowest = 0f;

            foreach (var side in TileSides.All)
            {
                var heading = TileSides.Toward(side);
                figure.Face(heading);

                var frames = 0;
                for (; frames < FrameCap && figure.IsTurning; frames++)
                {
                    Step(rig, builder, walker, driven);
                }

                var yaw = Yaw(figure);
                var pointed = FigureFacing.HeadingOf(yaw);

                off = Math.Max(
                    off, Math.Abs(FigureFacing.Shortest(yaw, FigureFacing.Of(worn, heading))));
                mirrored = Math.Max(mirrored, 1f - (pointed.X * heading.X + pointed.Z * heading.Z));
                slowest = Math.Max(slowest, frames * Frame);

                cardinals.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} on {1:0.##} in {2:0.###}s",
                    side,
                    yaw,
                    frames * Frame));
            }

            var failures = Assert(
                report,
                off <= YawEpsilon && mirrored <= 1e-3f,
                "a figure told to face each of the four cardinals comes round flush along that heading, the "
                + "pack's offset composed with the live yaw rather than either of them winning outright",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "worst {0:0.####} degrees off the composed yaw and {1:0.####} off flush: {2}",
                    off,
                    mirrored,
                    string.Join(", ", cardinals.ToArray())));

            return failures + Assert(
                report,
                slowest > 0f && slowest <= FigureTurn.TileSeconds + Frame,
                "the widest of those turns eases out inside the time it takes to cross one tile, so a "
                + "switchback never leaves a figure still swinging when it reaches the next tile",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the slowest took {0:0.###}s against the {1:0.###}s of one tile, at {2:0.#} degrees a "
                    + "second over a {3:0.###}s half turn",
                    slowest,
                    FigureTurn.TileSeconds,
                    FigureTurn.DegreesPerSecond,
                    FigureTurn.SecondsToTurn(FigureFacing.HalfTurn)));
        }

        static int TurnedAlongItsWalk(
            CameraRig rig,
            WorldBuilder builder,
            Walker walker,
            FigureAnimator driven,
            PlayerFigure figure,
            StringBuilder report)
        {
            var headings = new HashSet<float>();
            var wanted = float.NaN;
            var since = 0;
            var late = 0;
            var worst = 0f;
            var widest = 0f;
            var walking = 0;
            var moves = 0;
            var grace = FigureTurn.TileSeconds + Frame * 2f;
            var was = Yaw(figure);

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

                for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
                {
                    Step(rig, builder, walker, driven);

                    var yaw = Yaw(figure);
                    widest = Math.Max(widest, Math.Abs(FigureFacing.Shortest(was, yaw)));
                    was = yaw;

                    var heading = walker.Walk.Facing;
                    if (walker.Walk.IsRetreating || !FigureFacing.IsAimed(heading))
                    {
                        continue;
                    }

                    var aim = FigureFacing.Composed(figure.RestYaw, heading);

                    if (float.IsNaN(wanted) || Math.Abs(FigureFacing.Shortest(wanted, aim)) > YawEpsilon)
                    {
                        wanted = aim;
                        since = 0;
                        continue;
                    }

                    since++;

                    if (driven == null || driven.Act != FigureAct.Walk)
                    {
                        continue;
                    }

                    walking++;
                    headings.Add((float)Math.Round(FigureFacing.YawOf(heading)));

                    if (since * Frame <= grace)
                    {
                        continue;
                    }

                    var strayed = Math.Abs(FigureFacing.Shortest(yaw, aim));
                    if (strayed > YawEpsilon)
                    {
                        late++;
                        worst = Math.Max(worst, strayed);
                    }
                }

                if (headings.Count > 1)
                {
                    break;
                }
            }

            for (var frame = 0; frame < SettleFrames; frame++)
            {
                Step(rig, builder, walker, driven);
            }

            var stopped = Yaw(figure);
            var held = 0f;

            for (var frame = 0; frame < SettleFrames * 4; frame++)
            {
                Step(rig, builder, walker, driven);
                held = Math.Max(held, Math.Abs(FigureFacing.Shortest(stopped, Yaw(figure))));
            }

            var failures = Assert(
                report,
                walking > 0 && headings.Count > 1 && late == 0,
                "every frame the walk clip runs on, once the heading has been held for one tile, finds the "
                + "mesh turned onto that heading rather than sliding sideways along it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} walking frames over {1} moves and {2} headings left {3} of them facing the wrong "
                    + "way, the worst by {4:0.##} degrees, measured {5:0.###}s after each change of heading",
                    walking,
                    moves,
                    headings.Count,
                    late,
                    worst,
                    grace));

            failures += Assert(
                report,
                widest > YawEpsilon
                && widest <= FigureTurn.DegreesPerSecond * Frame * 1.6f,
                "the turn is eased rather than snapped, so no single frame of the walk swings the mesh "
                + "further than an eased turn at its rate could carry it",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the widest single frame turned {0:0.##} degrees against the {1:0.##} allowed",
                    widest,
                    FigureTurn.DegreesPerSecond * Frame * 1.6f));

            return failures + Assert(
                report,
                held <= YawEpsilon
                && Math.Abs(FigureFacing.Shortest(stopped, figure.RestYaw)) > YawEpsilon,
                "a figure that stops holds the facing it walked in on over the whole idle loop rather than "
                + "swinging back to the camera-facing pose the world built it in",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it stopped on {0:0.##}, {1:0.##} degrees off the {2:0.##} it was built in, and drifted "
                    + "{3:0.####} degrees over {4} idle frames",
                    stopped,
                    Math.Abs(FigureFacing.Shortest(stopped, figure.RestYaw)),
                    figure.RestYaw,
                    held,
                    SettleFrames * 4));
        }

        static int SquaredUpWithTheEnemy(
            CameraRig rig,
            WorldBuilder builder,
            Walker walker,
            FigureAnimator driven,
            PlayerFigure figure,
            StringBuilder report)
        {
            EnemyFigure enemy = null;
            var approach = default(WorldPoint);

            for (var move = 0; move < Moves && enemy == null && !walker.Run.IsLevelComplete; move++)
            {
                var target = Furthest(walker.Run);
                if (target == TapAim.Nothing)
                {
                    break;
                }

                walker.WalkTo(target);

                for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
                {
                    Step(rig, builder, walker, driven);

                    if (enemy != null || !walker.Walk.IsWaiting)
                    {
                        continue;
                    }

                    var standing = builder.Fights.Of(walker.Walk.ArrivedNodeId);
                    if (standing == null || standing.HasFallen
                        || !FigureFacing.IsAimed(walker.Walk.Facing))
                    {
                        continue;
                    }

                    enemy = standing;
                    approach = walker.Walk.Facing;
                    break;
                }
            }

            if (enemy == null)
            {
                return Assert(
                    report,
                    false,
                    "an enemy the player walks into is facing the player when the fight opens",
                    "no fight came up over " + Moves + " greedy moves");
            }

            var atThePlayer = FigureFacing.Composed(enemy.RestYaw, FigureFacing.Reversed(approach));
            var atTheEnemy = FigureFacing.Composed(figure.RestYaw, approach);
            var asked = Math.Abs(FigureFacing.Shortest(enemy.RestYaw, atThePlayer));
            var closestEnemy = float.MaxValue;
            var closestPlayer = float.MaxValue;
            var acts = new HashSet<FigureAct>();

            for (var frame = 0; frame < SettleFrames + 5; frame++)
            {
                Step(rig, builder, walker, driven);

                closestEnemy = Math.Min(
                    closestEnemy, Math.Abs(FigureFacing.Shortest(Yaw(enemy), atThePlayer)));
                closestPlayer = Math.Min(
                    closestPlayer, Math.Abs(FigureFacing.Shortest(Yaw(figure), atTheEnemy)));

                if (driven != null)
                {
                    acts.Add(driven.Act);
                }
            }

            return Assert(
                report,
                closestEnemy <= YawEpsilon && closestPlayer <= YawEpsilon && asked > YawEpsilon,
                "a fight turns the two of them onto each other inside a tile of it opening, so the blows "
                + "land between two figures that are looking at one another",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "the enemy came {0:0.####} degrees off the {1:0.##} that faces the player, {2:0.##} "
                    + "degrees round from the {3:0.##} it was built in, while the player came {4:0.####} "
                    + "off the {5:0.##} that faces it, over {6}",
                    closestEnemy,
                    atThePlayer,
                    asked,
                    enemy.RestYaw,
                    closestPlayer,
                    atTheEnemy,
                    string.Join("/", Names(acts))));
        }

        static float Yaw(Figure figure)
        {
            return FigureFacing.Normalised(figure.transform.localEulerAngles.y);
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

            var figure = builder.Player;
            var forward = walker.Walk.Facing;
            var aiming = figure == null ? float.NaN : figure.FacingYaw;
            var backwards = figure == null || !FigureFacing.IsAimed(forward)
                ? float.NaN
                : FigureFacing.Composed(figure.RestYaw, FigureFacing.Reversed(forward));

            walker.Cancel();

            var acts = new HashSet<FigureAct>();
            var frames = 0;
            var retargeted = 0;

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker, driven);

                if (walker.Walk.IsRetreating && !walker.Walk.IsSettled)
                {
                    acts.Add(driven.Act);
                    frames++;

                    if (figure != null
                        && Math.Abs(FigureFacing.Shortest(aiming, figure.FacingYaw)) > YawEpsilon)
                    {
                        retargeted++;
                    }
                }
            }

            for (var frame = 0; frame < 30; frame++)
            {
                Step(rig, builder, walker, driven);
            }

            var back = driven.Act;
            var ended = figure == null ? float.NaN : Yaw(figure);

            var failures = Assert(
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

            return failures + Assert(
                report,
                figure != null
                && frames > 0
                && retargeted == 0
                && !float.IsNaN(backwards)
                && Math.Abs(FigureFacing.Shortest(ended, aiming)) <= YawEpsilon
                && Math.Abs(FigureFacing.Shortest(ended, backwards)) > 90f,
                "and it holds the facing it set out with the whole way back, so the backwards clip reads as "
                + "walking backwards rather than as a figure that turned round and strode off",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "it was aimed at {0:0.##} when it was sent back, was aimed somewhere new on {1} of its "
                    + "{2} falling-back frames, and ended on {3:0.##} against the {4:0.##} the retreat "
                    + "heading would have wanted",
                    aiming,
                    retargeted,
                    frames,
                    ended,
                    backwards));
        }

        static float BeatOf(FigureAct act, float held)
        {
            var wanted = 0f;
            var away = float.MaxValue;

            foreach (var beat in BeatsOf(act))
            {
                var gap = Math.Abs(beat - held);
                if (gap >= away)
                {
                    continue;
                }

                away = gap;
                wanted = beat;
            }

            return wanted;
        }

        static List<float> BeatsOf(FigureAct act)
        {
            var beats = new List<float>();

            switch (act)
            {
                case FigureAct.Clash:
                    beats.Add(Fight.Of(ActionOutcome.Tie).BlowBeat);
                    break;
                case FigureAct.Strike:
                    beats.Add(Fight.Of(ActionOutcome.Loss).BlowBeat);
                    break;
                case FigureAct.Fall:
                    beats.Add(Fight.Of(ActionOutcome.Loss).FallBeat);
                    beats.Add(Fight.Of(ActionOutcome.Win).FallBeat);
                    break;
                case FigureAct.Take:
                    beats.Add(Take.Seconds);
                    break;
                default:
                    if (IsBlow(act))
                    {
                        beats.Add(Fight.Of(ActionOutcome.Win).BlowBeat);
                    }

                    break;
            }

            return beats;
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
