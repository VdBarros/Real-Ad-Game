using System;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class EncounterCheckCommand
    {
        const long Seed = 20250824L;

        const string Preset = "ship";

        const string ShotPath = "dev/scratch/t-15-";

        const int Doorstep = 1;

        const int Home = 0;

        const int Power = 3;

        const int Prize = 5;

        const float Frame = 1f / 60f;

        const int FrameCap = 4000;

        sealed class Tally
        {
            public ActionOutcome Outcome;
            public float PlayerPeak;
            public float EnemyPeak;
            public int LitFrames;
            public bool WeaponFlew;
            public bool EnemyFell;
            public bool Rebanded;
            public int Cleared;
            public int ClearedBefore;
            public RunState Settled;
        }

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreviewFilm.Sun();

            var report = new StringBuilder("encounter at power ")
                .Append(Power.ToString(CultureInfo.InvariantCulture))
                .Append(", against the same doorstep enemy worth one less, the same and one more:");

            var win = Fought(Power, Power - 1, "win");
            var tie = Fought(Power, Power, "tie");
            var loss = Fought(Power, Power + 1, "loss");

            report.Append(Row(win, Power, Power - 1));
            report.Append(Row(tie, Power, Power));
            report.Append(Row(loss, Power, Power + 1));

            Expect(win.Outcome, ActionOutcome.Win);
            Expect(tie.Outcome, ActionOutcome.Tie);
            Expect(loss.Outcome, ActionOutcome.Loss);

            TieAndLossAreToldApart(tie, loss);
            NothingMoved(tie, Power);
            NothingMoved(loss, Power);
            TheWinPaidExactly(win, Power, Power - 1);

            Debug.Log(report.ToString());
        }

        static LevelGraph Arena(int enemyValue)
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            for (var x = 0; x < 5; x++)
            {
                builder.AddTile(At(x), regionId: 0);
            }

            builder.AddNode(At(0), NodeType.Start);
            builder.AddNode(At(2), NodeType.Enemy, enemyValue);
            builder.AddNode(At(4), NodeType.Additive, Prize);

            builder.Connect(At(0), At(2), new[] { At(1) });
            builder.Connect(At(2), At(4), new[] { At(3) });

            return builder.Build();
        }

        static TilePosition At(int x)
        {
            return new TilePosition(floor: 0, x: x, y: 0);
        }

        static Tally Fought(int startingPower, int enemyValue, string leg)
        {
            var graph = Arena(enemyValue);
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, startingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);

            var reel = new Tally { Outcome = ActionOutcome.Rejected, ClearedBefore = builder.Floor.ClearedCount };
            walker.Arrived += result => reel.Outcome = result.Outcome;

            var figure = builder.Fights.Of(Doorstep);
            var post = IsoProjection.Of(graph.Decisions.Node(Doorstep).Position);
            var enemyPost = figure != null ? figure.Ground : post;
            var band = figure != null ? figure.Band : default(EnemyBand);

            if (figure == null)
            {
                Debug.LogError("The arena raised no figure for its one enemy.");
            }

            walker.WalkTo(Doorstep);

            if (!walker.IsWalking)
            {
                Debug.LogError("A tap on the doorstep enemy at power " + startingPower + " started no walk.");
            }

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);

                if (!walker.Walk.IsWaiting)
                {
                    continue;
                }

                reel.PlayerPeak = Further(reel.PlayerPeak, builder.Player.Ground, post);
                reel.EnemyPeak = figure == null
                    ? reel.EnemyPeak
                    : Further(reel.EnemyPeak, figure.Ground, enemyPost);
                reel.WeaponFlew |= builder.Player.IsFlying;
                reel.EnemyFell |= figure != null && figure.HasFallen;

                if (!builder.Fights.IsSparkLit)
                {
                    continue;
                }

                reel.LitFrames++;
                PreviewFilm.Shoot(lens, ShotPath + leg + ".png");
            }

            if (walker.IsWalking)
            {
                Debug.LogError("The " + leg + " was still playing after " + FrameCap + " frames.");
            }

            for (var frame = 0; frame < 120; frame++)
            {
                Step(rig, builder, walker);
                reel.WeaponFlew |= builder.Player.IsFlying;
                reel.EnemyFell |= figure != null && figure.HasFallen;
            }

            if (builder.Fights.IsSparkLit)
            {
                Debug.LogError("The " + leg + " ended with its spark still lit.");
            }

            if (input.IsLocked)
            {
                Debug.LogError("The " + leg + " ended without handing input back.");
            }

            reel.Rebanded = figure != null && figure.Band != band;
            reel.Settled = walker.Run;
            reel.Cleared = builder.Floor.ClearedCount;

            ThePlayerStandsOnItsNode(builder, walker.Run, leg);

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return reel;
        }

        static void Expect(ActionOutcome fought, ActionOutcome wanted)
        {
            if (fought != wanted)
            {
                Debug.LogError("A fight staged as a " + wanted + " resolved as a " + fought + ".");
            }
        }

        static void TieAndLossAreToldApart(Tally tie, Tally loss)
        {
            if (loss.PlayerPeak <= tie.PlayerPeak)
            {
                Debug.LogError(
                    "A loss threw the player " + loss.PlayerPeak + " tiles and a tie threw them "
                    + tie.PlayerPeak + ". A loss has to read as the heavier blow.");
            }

            if (tie.EnemyPeak <= 0f)
            {
                Debug.LogError("A tie left the enemy standing perfectly still, so it reads as a loss.");
            }

            if (loss.EnemyPeak > 0f)
            {
                Debug.LogError(
                    "A loss moved the enemy " + loss.EnemyPeak + " tiles, so it reads as a tie.");
            }

            if (tie.LitFrames == 0 || loss.LitFrames == 0)
            {
                Debug.LogError("A fight that struck no spark is a fight the player never saw.");
            }
        }

        static void NothingMoved(Tally reel, int startingPower)
        {
            if (reel.Settled.Power != startingPower
                || reel.Settled.PositionNodeId != Home
                || reel.Settled.ConsumedNodes.Count != 0)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " left the run on node " + reel.Settled.PositionNodeId
                    + " at power " + reel.Settled.Power + " with "
                    + reel.Settled.ConsumedNodes.Count + " consumed, where it began on node "
                    + Home + " at power " + startingPower + " with nothing consumed.");
            }

            if (reel.EnemyFell)
            {
                Debug.LogError("A " + reel.Outcome + " took the enemy off the board.");
            }

            if (reel.WeaponFlew)
            {
                Debug.LogError("A " + reel.Outcome + " dropped a weapon.");
            }

            if (reel.Cleared != reel.ClearedBefore)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " cleared the floor from " + reel.ClearedBefore + " tiles to "
                    + reel.Cleared + ", where a fight that changes nothing opens nothing.");
            }
        }

        static void TheWinPaidExactly(Tally win, int startingPower, int enemyValue)
        {
            if (win.Settled.Power != startingPower + enemyValue)
            {
                Debug.LogError(
                    "A win over an enemy worth " + enemyValue + " took power " + startingPower
                    + " to " + win.Settled.Power + " rather than " + (startingPower + enemyValue) + ".");
            }

            if (!win.EnemyFell)
            {
                Debug.LogError("A win left the enemy standing.");
            }

            if (!win.WeaponFlew)
            {
                Debug.LogError("A win dropped no weapon.");
            }

            if (win.Rebanded)
            {
                Debug.LogError(
                    "A win rebanded the enemy it was killing, so the corpse popped to a new size and "
                    + "colour on the frame the power landed, before the dissolve had begun.");
            }

            if (win.Cleared <= win.ClearedBefore)
            {
                Debug.LogError(
                    "A win left the floor at " + win.Cleared + " cleared tiles, so the corridor the enemy "
                    + "was guarding never opened.");
            }
        }

        static void ThePlayerStandsOnItsNode(WorldBuilder builder, RunState state, string leg)
        {
            var expected = IsoProjection.Of(state.Level.Decisions.Node(state.PositionNodeId).Position);
            var standing = builder.Player.Ground;

            if (Mathf.Abs(standing.X - expected.X) > 0.001f
                || Mathf.Abs(standing.Y - expected.Y) > 0.001f
                || Mathf.Abs(standing.Z - expected.Z) > 0.001f)
            {
                Debug.LogError(
                    "The " + leg + " ended with the run on node " + state.PositionNodeId + " at "
                    + expected + " while the figure stands at " + standing + ".");
            }
        }

        static float Further(float peak, WorldPoint standing, WorldPoint post)
        {
            var x = standing.X - post.X;
            var z = standing.Z - post.Z;
            var away = (float)Math.Sqrt(x * x + z * z);

            return away > peak ? away : peak;
        }

        static void Step(CameraRig rig, WorldBuilder builder, Walker walker)
        {
            walker.Advance(Frame);
            rig.Advance(Frame);
            builder.Floor.Advance(Frame);
            builder.Pickups.Advance(Frame);

            if (builder.PlayerBadge != null)
            {
                builder.PlayerBadge.Advance(Frame);
            }
        }

        static string Row(Tally reel, int startingPower, int enemyValue)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0} against {1}: {2}, player thrown {3:0.###} tiles, enemy {4:0.###}, {5} lit frames,"
                + " ending on node {6} at power {7}, floor {8} to {9}{10}{11}",
                startingPower,
                enemyValue,
                reel.Outcome,
                reel.PlayerPeak,
                reel.EnemyPeak,
                reel.LitFrames,
                reel.Settled.PositionNodeId,
                reel.Settled.Power,
                reel.ClearedBefore,
                reel.Cleared,
                reel.EnemyFell ? ", enemy dissolved" : string.Empty,
                reel.WeaponFlew ? ", weapon dropped" : string.Empty);
        }
    }
}
