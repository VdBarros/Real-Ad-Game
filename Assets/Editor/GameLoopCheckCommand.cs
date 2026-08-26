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
    public static class GameLoopCheckCommand
    {
        const long Seed = 20250825L;

        const int Cycles = 20;

        const float Frame = 1f / 60f;

        const int FlightCap = 600;

        const int WalkCap = 3000;

        const int MoveCap = 80;

        const string ShotPath = "dev/scratch/t-17-";

        static readonly MazePreset Unbuildable =
            new MazePreset("tiny", 4, 3, 1, 2, 0.25, 0, 11, 10000, 3);

        sealed class Turn
        {
            public int Number;
            public bool Skipped;
            public long AttemptSeed;
            public int Attempts;
            public int FinalPower;
            public int Moves;
            public int BeatsSkipped;
            public int Carriers;
            public int BadgeTextures;
            public int BadgeMaterials;
            public int BadgeSprites;
            public int WorldMaterials;
        }

        static int findings;

        public static void Check()
        {
            findings = 0;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TheRuntimeBootstrapStayedOutOfEditMode();
            PreviewFilm.Sun();

            var loop = GameLoop.Raise(Seed, MazePreset.Ship, null);
            var persistent = Identified(Live());
            var persistentCount = persistent.Count;
            var announced = new List<GamePhase>();
            loop.Turned += turned => announced.Add(turned.Phase);

            var badgeTexture = default(EntityId);
            var badgeMaterial = default(EntityId);
            var turns = new Turn[Cycles];

            for (var index = 0; index < Cycles; index++)
            {
                var turn = new Turn { Number = index + 1, Skipped = index % 2 == 0 };
                turns[index] = turn;

                if (index == 0)
                {
                    loop.Advance(Frame);
                }

                TheLoopIsPreviewingItsLevel(loop, turn);
                ALevelIsOneRoot(loop, persistent, turn);
                EnterPlay(loop, turn);
                Win(loop, turn);
                TheResultShowsTheFinalPower(loop, turn);
                Measure(loop, turn);

                if (index == 0)
                {
                    badgeTexture = InstanceOf<Texture2D>(BadgeAssets.NamePrefix);
                    badgeMaterial = InstanceOf<Material>(BadgeAssets.NamePrefix);
                    PreviewFilm.Shoot(loop.Rig.GetComponent<Camera>(), ShotPath + "first.png");
                }

                TheBadgeAssetsWereNotMinted(turn, badgeTexture, badgeMaterial);

                if (index < Cycles - 1)
                {
                    loop.Next();
                }
            }

            PreviewFilm.Shoot(loop.Rig.GetComponent<Camera>(), ShotPath + "twentieth.png");

            loop.Tear();
            TeardownLeavesOnlyWhatOutlivesALevel(loop, persistent, persistentCount);
            TheLoopIsNeverCaughtGenerating(announced);

            WorldObjects.Destroy(loop.gameObject);

            AFailedDrawLeavesTheLoopWhereItWas();

            Debug.Log(Report(turns, persistentCount));
            Debug.Log(findings == 0
                ? "t-17: the loop turned " + Cycles + " times clean."
                : "t-17: " + findings + " findings across " + Cycles + " turns.");
        }

        static void TheLoopIsNeverCaughtGenerating(List<GamePhase> announced)
        {
            foreach (var phase in announced)
            {
                if (phase == GamePhase.Generating)
                {
                    Fail("The loop announced itself sitting in Generating, where nothing can move it on.");
                    return;
                }
            }

            if (announced.Count == 0)
            {
                Fail("The loop announced no turns at all, so this guard went blind.");
            }
        }

        static void AFailedDrawLeavesTheLoopWhereItWas()
        {
            var loop = GameLoop.Raise(Seed, Unbuildable, null);
            var thrown = Drew(loop);

            if (thrown == null)
            {
                Fail("A preset no level survives still produced one, so this guard went blind.");
            }

            if (loop.Phase == GamePhase.Generating)
            {
                Fail("A failed draw stranded the loop in Generating with nothing to move it on.");
            }

            if (loop.Phase != GamePhase.Cutscene)
            {
                Fail("A failed draw left the loop in " + loop.Phase + " rather than where it stood.");
            }

            if (loop.LevelRoot != null)
            {
                Fail("A failed draw still raised a level root.");
            }

            if (loop.Supply.SeedsSpent != 1 || loop.Supply.DrawsFailed != 1)
            {
                Fail(
                    "A failed draw spent " + loop.Supply.SeedsSpent + " seeds and recorded "
                    + loop.Supply.DrawsFailed + " failures.");
            }

            Drew(loop);

            if (loop.Supply.SeedsSpent != 2)
            {
                Fail("A second attempt re-drew the seed that had already failed.");
            }

            WorldObjects.Destroy(loop.gameObject);
        }

        static Exception Drew(GameLoop loop)
        {
            try
            {
                loop.Advance(Frame);
                return null;
            }
            catch (LevelGenerationException failed)
            {
                return failed;
            }
        }

        static void TheRuntimeBootstrapStayedOutOfEditMode()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameLoop>() != null)
            {
                Fail("The runtime bootstrap raised a loop inside an edit-mode check.");
            }

            if (GameObject.Find(GameBoot.SunName) != null)
            {
                Fail("The runtime bootstrap lit an edit-mode scene it does not own.");
            }
        }

        static void TheLoopIsPreviewingItsLevel(GameLoop loop, Turn turn)
        {
            if (loop.Phase != GamePhase.Preview)
            {
                Fail("Turn " + turn.Number + " opened in " + loop.Phase + " rather than a fly-through.");
                return;
            }

            if (loop.LevelNumber != turn.Number)
            {
                Fail("Turn " + turn.Number + " is flying over level " + loop.LevelNumber + ".");
            }

            if (!loop.Rig.IsBusy)
            {
                Fail("Turn " + turn.Number + " began with the fly-through already landed.");
            }

            turn.AttemptSeed = loop.Level.AttemptSeed;
            turn.Attempts = loop.Supply.LastReport.Attempts;
        }

        static void EnterPlay(GameLoop loop, Turn turn)
        {
            if (turn.Skipped)
            {
                loop.Input.ReleaseAt(new ScreenPoint(0f, 0f));
                loop.Advance(Frame);

                if (loop.Phase != GamePhase.Play)
                {
                    Fail(
                        "Turn " + turn.Number + " sat in " + loop.Phase
                        + " for a frame after a tap that should have entered play.");
                }
            }
            else
            {
                var frames = 0;
                while (loop.Phase == GamePhase.Preview && frames < FlightCap)
                {
                    loop.Rig.Advance(Frame);
                    loop.Advance(Frame);
                    frames++;
                }

                if (frames >= FlightCap)
                {
                    Fail("Turn " + turn.Number + " never came out of the fly-through.");
                    return;
                }
            }

            if (loop.Phase != GamePhase.Play)
            {
                Fail("Turn " + turn.Number + " reached " + loop.Phase + " instead of play.");
                return;
            }

            if (!loop.Rig.Framing.Equals(loop.Rig.Following))
            {
                Fail(
                    "Turn " + turn.Number + " entered play framed at " + loop.Rig.Framing
                    + " rather than back on the player at " + loop.Rig.Following + ".");
            }

            if (loop.Input.IsLocked)
            {
                Fail("Turn " + turn.Number + " handed the player a locked screen.");
            }
        }

        static void Win(GameLoop loop, Turn turn)
        {
            while (loop.Phase == GamePhase.Play && turn.Moves < MoveCap)
            {
                var target = NextMove(loop.Run);
                if (target < 0)
                {
                    Fail(
                        "Turn " + turn.Number + " stalled at power " + loop.Run.Power
                        + " with " + loop.Run.ConsumedNodes.Count + " nodes taken.");
                    return;
                }

                loop.Walker.WalkTo(target);
                turn.Moves++;

                var frames = 0;
                while (loop.Walker.IsWalking && frames < WalkCap)
                {
                    Step(loop);
                    frames++;

                    if (loop.Rig.IsBusy && turn.BeatsSkipped == 0)
                    {
                        ATapDuringABeatReturnsControlImmediately(loop, turn);
                    }
                }

                if (frames >= WalkCap)
                {
                    Fail("Turn " + turn.Number + " never finished walking to node " + target + ".");
                    return;
                }

                var settling = 0;
                while (loop.Phase == GamePhase.Play && loop.Run.IsLevelComplete && settling < WalkCap)
                {
                    Step(loop);
                    settling++;
                }
            }

            if (loop.Phase != GamePhase.Result)
            {
                Fail("Turn " + turn.Number + " ran " + turn.Moves + " moves without a result.");
            }
        }

        static void ATapDuringABeatReturnsControlImmediately(GameLoop loop, Turn turn)
        {
            turn.BeatsSkipped++;
            loop.Input.ReleaseAt(new ScreenPoint(0f, 0f));

            if (loop.Rig.IsBusy)
            {
                Fail("Turn " + turn.Number + " held its beat through a tap that should have ended it.");
                return;
            }

            if (!loop.Rig.Framing.Equals(loop.Rig.Following))
            {
                Fail(
                    "Turn " + turn.Number + " left a skipped beat framed at " + loop.Rig.Framing
                    + " rather than back on the player at " + loop.Rig.Following + ".");
            }
        }

        static void TheResultShowsTheFinalPower(GameLoop loop, Turn turn)
        {
            if (loop.Phase != GamePhase.Result)
            {
                return;
            }

            turn.FinalPower = loop.Cycle.FinalPower;

            if (!loop.Run.IsLevelComplete)
            {
                Fail("Turn " + turn.Number + " showed a result without the boss falling.");
            }

            if (turn.FinalPower != loop.Run.Power)
            {
                Fail(
                    "Turn " + turn.Number + " ended on " + loop.Run.Power
                    + " but the cycle carries " + turn.FinalPower + ".");
            }

            if (!loop.Screen.IsShowing)
            {
                Fail("Turn " + turn.Number + " reached a result with the screen still hidden.");
            }

            if (loop.Screen.Power != turn.FinalPower)
            {
                Fail(
                    "Turn " + turn.Number + " reads " + loop.Screen.Power
                    + " on a screen that should read " + turn.FinalPower + ".");
            }

            if (loop.Screen.Next == null || !loop.Screen.Next.IsInteractable())
            {
                Fail("Turn " + turn.Number + " showed a result with no Next to press.");
            }
        }

        static void ALevelIsOneRoot(GameLoop loop, HashSet<EntityId> persistent, Turn turn)
        {
            if (loop.LevelRoot == null)
            {
                Fail("Turn " + turn.Number + " raised no level root.");
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
                    Fail(
                        "Turn " + turn.Number + " parented " + Trail(carrier)
                        + " outside the level root " + loop.LevelRoot.name + ".");
                }
            }
        }

        static void TeardownLeavesOnlyWhatOutlivesALevel(
            GameLoop loop, HashSet<EntityId> persistent, int persistentCount)
        {
            if (loop.LevelRoot != null)
            {
                Fail("Teardown left the level root standing.");
            }

            var live = Live();

            foreach (var carrier in live)
            {
                if (!persistent.Contains(carrier.GetEntityId()))
                {
                    Fail("Teardown left " + Trail(carrier) + " behind.");
                }
            }

            if (live.Count != persistentCount)
            {
                Fail(
                    "Teardown settled on " + live.Count + " carriers where the loop opened with "
                    + persistentCount + ".");
            }
        }

        static void TheBadgeAssetsWereNotMinted(Turn turn, EntityId texture, EntityId material)
        {
            if (turn.BadgeTextures != 1)
            {
                Fail(
                    "Turn " + turn.Number + " left " + turn.BadgeTextures
                    + " badge textures alive where the loop mints one.");
            }

            if (turn.BadgeMaterials != 1)
            {
                Fail(
                    "Turn " + turn.Number + " left " + turn.BadgeMaterials
                    + " badge materials alive where the loop mints one.");
            }

            if (turn.BadgeSprites > Enum.GetValues(typeof(BadgeShape)).Length)
            {
                Fail(
                    "Turn " + turn.Number + " left " + turn.BadgeSprites
                    + " badge sprites alive where a shape is cut at most once.");
            }

            if (turn.WorldMaterials > Enum.GetValues(typeof(PartStyle)).Length)
            {
                Fail(
                    "Turn " + turn.Number + " left " + turn.WorldMaterials
                    + " world materials alive where a style is coloured at most once.");
            }

            if (turn.Number == 1)
            {
                return;
            }

            if (!InstanceOf<Texture2D>(BadgeAssets.NamePrefix).Equals(texture))
            {
                Fail("Turn " + turn.Number + " draws badges off a texture the first turn did not.");
            }

            if (!InstanceOf<Material>(BadgeAssets.NamePrefix).Equals(material))
            {
                Fail("Turn " + turn.Number + " draws badges through a material the first turn did not.");
            }
        }

        static void Measure(GameLoop loop, Turn turn)
        {
            turn.Carriers = Live().Count;
            turn.BadgeTextures = Counted<Texture2D>(BadgeAssets.NamePrefix);
            turn.BadgeMaterials = Counted<Material>(BadgeAssets.NamePrefix);
            turn.BadgeSprites = Counted<Sprite>(BadgeAssets.NamePrefix);
            turn.WorldMaterials = Counted<Material>(Presentation.WorldMaterials.NamePrefix);
        }

        static int NextMove(RunState run)
        {
            var boss = -1;
            var any = -1;

            foreach (var nodeId in TapAim.Aimable(run))
            {
                var outcome = ActionResolver.Resolve(run, nodeId).Outcome;
                if (outcome != ActionOutcome.Walked && outcome != ActionOutcome.Win)
                {
                    continue;
                }

                if (run.Level.Decisions.Node(nodeId).Type == NodeType.Boss)
                {
                    boss = nodeId;
                }
                else if (any < 0)
                {
                    any = nodeId;
                }
            }

            return boss >= 0 ? boss : any;
        }

        static void Step(GameLoop loop)
        {
            loop.Walker.Advance(Frame);
            loop.Rig.Advance(Frame);
            loop.World.Floor.Advance(Frame);
            loop.World.Pickups.Advance(Frame);

            if (loop.World.PlayerBadge != null)
            {
                loop.World.PlayerBadge.Advance(Frame);
            }

            loop.Advance(Frame);
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

        static EntityId InstanceOf<T>(string prefix) where T : UnityEngine.Object
        {
            foreach (var asset in Resources.FindObjectsOfTypeAll<T>())
            {
                if (asset.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return asset.GetEntityId();
                }
            }

            return default(EntityId);
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

        static string Report(Turn[] turns, int persistentCount)
        {
            var report = new StringBuilder("twenty turns of the loop off seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(persistentCount.ToString(CultureInfo.InvariantCulture))
                .Append(" carriers outlive a level:")
                .Append(Environment.NewLine)
                .Append("  turn      entry  attempts  moves  power  carriers  badgeTex  badgeMat  sprites  world  seed");

            foreach (var turn in turns)
            {
                report.Append(Environment.NewLine)
                    .Append(Pad(turn.Number, 6))
                    .Append(Pad(turn.Skipped ? "tapped" : "flown", 11))
                    .Append(Pad(turn.Attempts, 10))
                    .Append(Pad(turn.Moves, 7))
                    .Append(Pad(turn.FinalPower, 7))
                    .Append(Pad(turn.Carriers, 10))
                    .Append(Pad(turn.BadgeTextures, 10))
                    .Append(Pad(turn.BadgeMaterials, 10))
                    .Append(Pad(turn.BadgeSprites, 9))
                    .Append(Pad(turn.WorldMaterials, 7))
                    .Append("  ")
                    .Append(turn.AttemptSeed.ToString(CultureInfo.InvariantCulture));
            }

            return report.ToString();
        }

        static string Pad(int value, int width)
        {
            return Pad(value.ToString(CultureInfo.InvariantCulture), width);
        }

        static string Pad(string value, int width)
        {
            return value.PadLeft(width);
        }
    }
}
