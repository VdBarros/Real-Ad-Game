using System;
using System.Collections.Generic;
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

        const string SidesPath = "dev/scratch/t-43-encounter-";

        const int Sequence = 6;

        const int EveryNthFrame = 8;

        const float CloseRange = 6f;

        const float CloseFraming = 1.1f;

        const int Doorstep = 1;

        const int Home = 0;

        const int Prizehold = 2;

        const int Power = 3;

        const int Prize = 5;

        const int Standing = 60;

        const int Wall = 90;

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
            public readonly HashSet<FigureAct> PlayerActs = new HashSet<FigureAct>();
            public readonly HashSet<FigureAct> EnemyActs = new HashSet<FigureAct>();
            public int BlowFrames;
            public int HeldFrames;
            public bool WentGhost;
            public float GhostHigh;
            public float GhostLow = 1f;
            public int Shot;
            public bool PlayerActing;
            public bool EnemyActing;
            public int DrainFrames;
            public int ShownAtContact = -1;
            public int ShownSteps;
            public int WidestShownStep;
            public bool ShownRose;
            public bool EnemyNumbersMoved;
            public float DrainSeconds;
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
            report.Append(Sides(win, "win"));
            report.Append(Sides(tie, "tie"));
            report.Append(Sides(loss, "loss"));

            Expect(win.Outcome, ActionOutcome.Win);
            Expect(tie.Outcome, ActionOutcome.Tie);
            Expect(loss.Outcome, ActionOutcome.Loss);

            report.Append(Bled(tie, "tie"));
            report.Append(Bled(loss, "loss"));

            BothSidesRead(
                win,
                "win",
                new[] { FigureAct.Strike, FigureAct.Recoil },
                new[] { FigureAct.Recoil, FigureAct.Strike });
            BothSidesRead(tie, "tie", new[] { FigureAct.Clash }, new[] { FigureAct.Clash });
            BothSidesRead(loss, "loss", new[] { FigureAct.Recoil }, new[] { FigureAct.Strike });

            report.Append(PastTheEnemy(Power, Power - 1, "win"));
            report.Append(PastTheEnemy(Power, Power + 1, "loss"));

            TieAndLossAreToldApart(tie, loss);
            OnlyThePowerMoved(tie, Power);
            OnlyThePowerMoved(loss, Power);
            TheWinPaidExactly(win, Power, Power - 1);
            TheWinCostNothing(win);
            TheWinHeldTheControlsForTheWholeCeremony(win);
            TheWinDissolvedAGhost(win);
            NoGhostHaunts(tie);
            NoGhostHaunts(loss);

            report.Append(TheBrushCostsLessThanTheLean());

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
            return new TilePosition(elevation: 0, x: x, y: 0);
        }

        static Tally Fought(int startingPower, int enemyValue, string leg)
        {
            var graph = Arena(enemyValue);
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph, startingPower);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, startingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);

            var reel = new Tally { Outcome = ActionOutcome.Rejected, ClearedBefore = builder.Floor.ClearedCount };
            walker.Arrived += result => reel.Outcome = result.Outcome;

            var figure = builder.Fights.Of(Doorstep);
            var acting = root.GetComponentsInChildren<FigureAnimator>(true);
            var striking = builder.Player == null ? null : builder.Player.GetComponent<FigureAnimator>();
            var answering = figure == null ? null : figure.GetComponent<FigureAnimator>();

            reel.PlayerActing = striking != null && striking.IsRigged && striking.HasClipsToPlay;
            reel.EnemyActing = answering != null && answering.IsRigged && answering.HasClipsToPlay;

            var enemyNumbers = OtherNumbers(root, builder);
            var enemyNumbersAtFirst = ValuesOf(enemyNumbers);

            var post = IsoProjection.Of(graph.Decisions.Node(Doorstep).Position);
            var enemyPost = figure != null ? figure.Ground : post;
            var band = figure != null ? figure.Band : default(EnemyBand);
            var close = PreviewFilm.Rig(
                new Vector3(enemyPost.X, enemyPost.Y, enemyPost.Z), CloseRange, CloseFraming);

            PreviewFilm.Warm(close);

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
                var shownBefore = builder.PlayerBadge == null ? 0 : builder.PlayerBadge.Shown;
                var draining = walker.IsDraining;

                Step(rig, builder, walker, acting);
                Answer(reel, striking, answering, close, leg);

                if (draining)
                {
                    reel.DrainFrames++;
                    reel.DrainSeconds += Frame;
                    reel.EnemyNumbersMoved |= !SameValues(enemyNumbers, enemyNumbersAtFirst);

                    if (builder.PlayerBadge != null)
                    {
                        var shownNow = builder.PlayerBadge.Shown;
                        if (reel.ShownAtContact < 0)
                        {
                            reel.ShownAtContact = shownBefore;
                        }

                        if (shownNow != shownBefore)
                        {
                            reel.ShownSteps++;
                            var step = Math.Abs(shownNow - shownBefore);
                            reel.WidestShownStep = step > reel.WidestShownStep ? step : reel.WidestShownStep;
                            reel.ShownRose |= shownNow > shownBefore;
                        }
                    }
                }

                if (!walker.Walk.IsWaiting)
                {
                    continue;
                }

                reel.HeldFrames++;

                if (figure != null && figure.IsGhost && !figure.HasFallen)
                {
                    var haunting = figure.GhostAlpha;

                    reel.WentGhost = true;
                    reel.GhostHigh = haunting > reel.GhostHigh ? haunting : reel.GhostHigh;
                    reel.GhostLow = haunting < reel.GhostLow ? haunting : reel.GhostLow;
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
                Step(rig, builder, walker, acting);
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
            reel.EnemyNumbersMoved |= !SameValues(enemyNumbers, enemyNumbersAtFirst);

            ThePlayerStandsOnItsNode(builder, walker.Run, leg);

            WorldObjects.Destroy(close.gameObject);
            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return reel;
        }

        static string PastTheEnemy(int startingPower, int enemyValue, string leg)
        {
            var graph = Arena(enemyValue);
            var rig = CameraRig.Raise();

            var builder = new WorldBuilder();
            var root = builder.Build(graph, startingPower);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, startingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);
            var acting = root.GetComponentsInChildren<FigureAnimator>(true);
            var won = enemyValue < startingPower;

            var outcomes = new List<ActionOutcome>();
            walker.Arrived += result => outcomes.Add(result.Outcome);

            if (opening.IsReachable(Prizehold))
            {
                Debug.LogError(
                    "The prize is not behind the enemy in the " + leg
                    + " arena, so the leg proves nothing about tapping past one.");
            }

            if (!new HashSet<int>(TapAim.Aimable(opening)).Contains(Prizehold))
            {
                Debug.LogError(
                    "The prize behind the enemy is not offered to the finger on the " + leg + " leg.");
            }

            var preview = TargetPreview.Of(opening, Prizehold);

            if (!preview.IsLegal || preview.Route.Count != 3)
            {
                Debug.LogError(
                    "A tap on the prize behind the enemy previewed " + preview.Outcome + " over "
                    + preview.Route.Count + " nodes on the " + leg + " leg.");
            }

            walker.WalkTo(Prizehold);

            if (!walker.IsWalking)
            {
                Debug.LogError(
                    "A tap on the prize behind the enemy started no walk on the " + leg + " leg.");
            }

            var fought = false;
            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                fought |= walker.Walk.IsWaiting && walker.Walk.ArrivedNodeId == Doorstep;
                Step(rig, builder, walker, acting);
            }

            if (walker.IsWalking)
            {
                Debug.LogError(
                    "The walk past the enemy was still playing after " + FrameCap + " frames on the "
                    + leg + " leg.");
            }

            if (!fought)
            {
                Debug.LogError(
                    "The walk past the enemy never stopped on it, so nothing interrupted the movement "
                    + "on the " + leg + " leg.");
            }

            var settled = walker.Run;
            var landed = won ? Prizehold : Home;
            var carried = won ? startingPower + enemyValue + Prize : Drain.Floor;

            if (settled.PositionNodeId != landed || settled.Power != carried)
            {
                Debug.LogError(
                    "A walk past an enemy worth " + enemyValue + " at power " + startingPower
                    + " ended on node " + settled.PositionNodeId + " at power " + settled.Power
                    + " where it had to end on node " + landed + " at power " + carried + ".");
            }

            if (settled.IsConsumed(Prizehold) != won)
            {
                Debug.LogError(
                    "The prize behind the enemy was " + (won ? "left standing" : "taken")
                    + " on the " + leg + " leg.");
            }

            if (input.IsLocked)
            {
                Debug.LogError("The " + leg + " walk past the enemy ended without handing input back.");
            }

            ThePlayerStandsOnItsNode(builder, settled, leg + " past the enemy");

            var report = string.Format(
                CultureInfo.InvariantCulture,
                "\n  a tap on the prize behind the enemy, out of passage, laid a {0} node route, "
                + "fought on contact and {1} to node {2} at power {3} after {4}",
                preview.Route.Count,
                won ? "carried on" : "bounced back",
                settled.PositionNodeId,
                settled.Power,
                Outcomes(outcomes));

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return report;
        }

        static string Outcomes(List<ActionOutcome> outcomes)
        {
            var written = new StringBuilder();

            foreach (var outcome in outcomes)
            {
                if (written.Length > 0)
                {
                    written.Append(" then ");
                }

                written.Append(outcome.ToString());
            }

            return written.Length == 0 ? "no arrival at all" : written.ToString();
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

        static void OnlyThePowerMoved(Tally reel, int startingPower)
        {
            if (reel.Settled.PositionNodeId != Home || reel.Settled.ConsumedNodes.Count != 0)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " left the run on node " + reel.Settled.PositionNodeId
                    + " with " + reel.Settled.ConsumedNodes.Count + " consumed, where it began on node "
                    + Home + " with nothing consumed.");
            }

            if (reel.Settled.Power != Drain.Floor)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " held to the end left the run at power "
                    + reel.Settled.Power + " where the drain stops at " + Drain.Floor + ".");
            }

            if (reel.Settled.Power >= startingPower)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " cost the run nothing: it walked in at power " + startingPower
                    + " and walked out at " + reel.Settled.Power + ".");
            }

            if (reel.DrainFrames == 0)
            {
                Debug.LogError("A " + reel.Outcome + " never drained at all.");
            }

            if (reel.DrainSeconds < Drain.Seconds * 0.9f || reel.DrainSeconds > Drain.Seconds * 1.6f)
            {
                Debug.LogError(
                    "A " + reel.Outcome + " held for " + reel.DrainSeconds + "s where a drain to the "
                    + "floor takes " + Drain.Seconds + "s.");
            }

            if (reel.EnemyNumbersMoved)
            {
                Debug.LogError(
                    "The enemy number moved while it was eating the run on the " + reel.Outcome + ".");
            }

            if (reel.ShownRose)
            {
                Debug.LogError("The power badge counted upwards during a " + reel.Outcome + ".");
            }

            if (reel.ShownAtContact != startingPower)
            {
                Debug.LogError(
                    "The badge read " + reel.ShownAtContact + " when a " + reel.Outcome
                    + " began draining, where the run walked in at " + startingPower + ".");
            }

            if (reel.ShownSteps < 1)
            {
                Debug.LogError(
                    "The power badge never moved over a " + reel.Outcome + " that took the run from "
                    + startingPower + " to " + Drain.Floor + ", so it snapped at the end of it.");
            }

            if (reel.WidestShownStep > 1)
            {
                Debug.LogError(
                    "The power badge jumped " + reel.WidestShownStep + " in one frame over a "
                    + reel.Outcome + ", so it skipped a number rather than following the fall.");
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

        static void TheWinCostNothing(Tally win)
        {
            if (win.DrainFrames != 0)
            {
                Debug.LogError("A win drained the player, where only a wall it cannot pass does.");
            }
        }

        static string TheBrushCostsLessThanTheLean()
        {
            var reel = new Tally();
            var untouched = Brushed(Standing, Wall, 0f, "untouched", null);
            var brushed = Brushed(Standing, Wall, 0.25f, "brush", null);
            var leaned = Brushed(Standing, Wall, 1.1f, "lean", null);
            var held = Brushed(Standing, Wall, Drain.Seconds * 1.5f, "hold", reel);

            TheBadgeFollowedTheFall(reel, Standing, held);

            var span = Standing - Drain.Floor;

            if (untouched > Standing || untouched < Standing - span / 10)
            {
                Debug.LogError(
                    "Touching a wall worth " + Wall + " at power " + Standing
                    + " and pulling straight out left " + untouched
                    + ", so the ramp is not there and a probe is a punishment.");
            }

            if (brushed >= untouched)
            {
                Debug.LogError(
                    "A 0.25s brush against a wall worth " + Wall + " left " + brushed
                    + " where a touch-and-go left " + untouched + ", so contact reads as free.");
            }

            if (Standing - brushed > span / 4)
            {
                Debug.LogError(
                    "A 0.25s brush cost " + (Standing - brushed) + " of " + span
                    + ", so a probe is not a move a player can afford to make.");
            }

            if (brushed <= leaned)
            {
                Debug.LogError(
                    "A 0.25s brush left " + brushed + " and a 1.1s lean left " + leaned
                    + ", so a short touch costs no less than a long one.");
            }

            if (leaned <= held)
            {
                Debug.LogError(
                    "A 1.1s lean left " + leaned + " and holding to the end left " + held
                    + ", so pulling out buys nothing.");
            }

            if (held != Drain.Floor)
            {
                Debug.LogError(
                    "Holding against the wall to the end left " + held + " rather than the floor of "
                    + Drain.Floor + ".");
            }

            if (brushed < Drain.Floor || leaned < Drain.Floor)
            {
                Debug.LogError("A drain fell through the floor of " + Drain.Floor + ".");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  against a wall worth {0} at power {1}, pulling out after 0s/0.25s/1.1s/{2}s left "
                + "{3}/{4}/{5}/{6}",
                Wall,
                Standing,
                Drain.Seconds * 1.5f,
                untouched,
                brushed,
                leaned,
                held)
                + string.Format(
                    CultureInfo.InvariantCulture,
                    ", the badge walking {0} down to {1} in {2} steps of at most {3}",
                    reel.ShownAtContact,
                    held,
                    reel.ShownSteps,
                    reel.WidestShownStep);
        }

        static void TheBadgeFollowedTheFall(Tally reel, int startingPower, int landed)
        {
            if (reel.ShownAtContact != startingPower)
            {
                Debug.LogError(
                    "The badge read " + reel.ShownAtContact + " when the drain began, where the run "
                    + "walked in at " + startingPower + ".");
            }

            if (reel.ShownRose)
            {
                Debug.LogError("The power badge counted upwards while it was being drained.");
            }

            if (reel.ShownSteps < (startingPower - landed) / 4)
            {
                Debug.LogError(
                    "The badge moved only " + reel.ShownSteps + " times falling from " + startingPower
                    + " to " + landed + ", so it snapped rather than following the fall.");
            }

            if (reel.WidestShownStep > (startingPower - landed) / 4)
            {
                Debug.LogError(
                    "The badge jumped " + reel.WidestShownStep + " in one frame falling from "
                    + startingPower + " to " + landed + ", so it snapped rather than following the fall.");
            }
        }

        static int Brushed(int startingPower, int wallValue, float seconds, string leg, Tally reel)
        {
            var graph = Arena(wallValue);
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph, startingPower);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, startingPower);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);
            var acting = root.GetComponentsInChildren<FigureAnimator>(true);

            walker.WalkTo(Doorstep);

            for (var frame = 0; frame < FrameCap && walker.IsWalking && !walker.IsDraining; frame++)
            {
                Step(rig, builder, walker, acting);
            }

            if (!walker.IsDraining)
            {
                Debug.LogError(
                    "A walk into a wall worth " + wallValue + " at power " + startingPower
                    + " never started draining on the " + leg + " leg.");
            }

            for (var held = 0f; held < seconds && walker.IsDraining; held += Frame)
            {
                var shownBefore = builder.PlayerBadge == null ? 0 : builder.PlayerBadge.Shown;

                Step(rig, builder, walker, acting);

                if (reel == null || builder.PlayerBadge == null)
                {
                    continue;
                }

                reel.DrainFrames++;
                if (reel.ShownAtContact < 0)
                {
                    reel.ShownAtContact = shownBefore;
                }

                var shownNow = builder.PlayerBadge.Shown;
                if (shownNow == shownBefore)
                {
                    continue;
                }

                reel.ShownSteps++;
                reel.ShownRose |= shownNow > shownBefore;
                var step = Math.Abs(shownNow - shownBefore);
                reel.WidestShownStep = step > reel.WidestShownStep ? step : reel.WidestShownStep;
            }

            walker.WalkTo(Home);

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker, acting);
            }

            var settled = walker.Run;

            if (settled.PositionNodeId != Home || settled.ConsumedNodes.Count != 0)
            {
                Debug.LogError(
                    "The " + leg + " leg ended on node " + settled.PositionNodeId + " with "
                    + settled.ConsumedNodes.Count + " consumed, where a wall is passed by nobody.");
            }

            if (graph.Decisions.Node(Doorstep).Value != wallValue)
            {
                Debug.LogError(
                    "The " + leg + " leg left the wall worth something other than " + wallValue + ".");
            }

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return settled.Power;
        }

        static NumberBadge[] OtherNumbers(GameObject root, WorldBuilder builder)
        {
            var mine = builder.PlayerBadge == null ? null : builder.PlayerBadge.GetComponent<NumberBadge>();
            var found = new List<NumberBadge>();

            foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
            {
                if (badge != mine)
                {
                    found.Add(badge);
                }
            }

            return found.ToArray();
        }

        static int[] ValuesOf(NumberBadge[] badges)
        {
            var values = new int[badges.Length];

            for (var index = 0; index < badges.Length; index++)
            {
                values[index] = badges[index] == null ? 0 : badges[index].Value;
            }

            return values;
        }

        static bool SameValues(NumberBadge[] badges, int[] first)
        {
            var now = ValuesOf(badges);

            if (now.Length != first.Length)
            {
                return false;
            }

            for (var index = 0; index < now.Length; index++)
            {
                if (now[index] != first[index])
                {
                    return false;
                }
            }

            return true;
        }

        static string Bled(Tally reel, string leg)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  the {0} held contact for {1:0.###}s over {2} frames, taking the badge from {3} down "
                + "to {4} in {5} steps of at most {6}, with the enemy own number {7}",
                leg,
                reel.DrainSeconds,
                reel.DrainFrames,
                reel.ShownAtContact,
                reel.Settled.Power,
                reel.ShownSteps,
                reel.WidestShownStep,
                reel.EnemyNumbersMoved ? "MOVED" : "never moving");
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

        static void Step(CameraRig rig, WorldBuilder builder, Walker walker, FigureAnimator[] acting)
        {
            walker.Advance(Frame);
            rig.Advance(Frame);
            builder.Floor.Advance(Frame);
            builder.Pickups.Advance(Frame);

            if (builder.PlayerBadge != null)
            {
                builder.PlayerBadge.Advance(Frame);
            }

            foreach (var driven in acting)
            {
                driven.Advance(Frame);
            }
        }

        static void Answer(
            Tally reel, FigureAnimator striking, FigureAnimator answering, Camera lens, string leg)
        {
            var blow = striking == null ? FigureAct.Idle : striking.Act;
            var reply = answering == null ? FigureAct.Idle : answering.Act;

            if (!IsBlow(blow) && !IsBlow(reply))
            {
                return;
            }

            reel.BlowFrames++;
            reel.PlayerActs.Add(blow);
            reel.EnemyActs.Add(reply);

            if (reel.Shot < Sequence && reel.BlowFrames % EveryNthFrame == 0)
            {
                PreviewFilm.Shoot(
                    lens,
                    SidesPath + leg + "-" + reel.Shot.ToString("00", CultureInfo.InvariantCulture) + ".png");
                reel.Shot++;
            }
        }

        static bool IsBlow(FigureAct act)
        {
            return act == FigureAct.Strike || act == FigureAct.Clash || act == FigureAct.Recoil;
        }

        static void TheWinHeldTheControlsForTheWholeCeremony(Tally win)
        {
            var held = win.HeldFrames * Frame;

            if (Math.Abs(held - VictoryStages.BlockingSeconds) > Frame * 2f)
            {
                Debug.LogError(
                    "A win held movement for " + held.ToString("0.###", CultureInfo.InvariantCulture)
                    + "s over " + win.HeldFrames + " frames, where the clash and the dissolve add up to "
                    + VictoryStages.BlockingSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s.");
            }
        }

        static void TheWinDissolvedAGhost(Tally win)
        {
            if (!win.WentGhost)
            {
                Debug.LogError(
                    "A win took the enemy off the board without ever turning it into a ghost, so the "
                    + "dissolve had nothing to fade.");
                return;
            }

            if (win.GhostHigh <= 0f || win.GhostHigh >= 1f || win.GhostHigh > Ghosting.Alpha + 0.001f)
            {
                Debug.LogError(
                    "A win's ghost stood at " + win.GhostHigh + " where a translucent silhouette reads "
                    + "somewhere above nothing and below the " + Ghosting.Alpha + " the ghost opens on.");
            }

            if (win.GhostLow > 0.05f)
            {
                Debug.LogError(
                    "A win's ghost never faded past " + win.GhostLow
                    + ", so the enemy blinked out rather than dissolving.");
            }
        }

        static void NoGhostHaunts(Tally reel)
        {
            if (reel.WentGhost)
            {
                Debug.LogError("A " + reel.Outcome + " turned the enemy it left standing into a ghost.");
            }
        }

        static void BothSidesRead(Tally reel, string leg, FigureAct[] blows, FigureAct[] replies)
        {
            if (!reel.PlayerActing || !reel.EnemyActing)
            {
                Debug.LogError(
                    "The " + leg + " was fought with the player animated " + reel.PlayerActing
                    + " and the enemy animated " + reel.EnemyActing
                    + ", so one side of it could never have read at all.");
            }

            if (reel.BlowFrames == 0)
            {
                Debug.LogError("The " + leg + " played no blow on either side.");
            }

            if (!Exactly(reel.PlayerActs, blows))
            {
                Debug.LogError(
                    "The " + leg + " had the player play " + Acts(reel.PlayerActs) + " where it owes "
                    + Wanted(blows) + ".");
            }

            if (!Exactly(reel.EnemyActs, replies))
            {
                Debug.LogError(
                    "The " + leg + " had the enemy play " + Acts(reel.EnemyActs) + " where it owes "
                    + Wanted(replies) + ".");
            }

            if (reel.Shot == 0)
            {
                Debug.LogError("The " + leg + " was never photographed, so nobody can read it on both sides.");
            }
        }

        static bool Exactly(HashSet<FigureAct> acts, FigureAct[] wanted)
        {
            if (acts.Count != wanted.Length)
            {
                return false;
            }

            foreach (var act in wanted)
            {
                if (!acts.Contains(act))
                {
                    return false;
                }
            }

            return true;
        }

        static string Wanted(FigureAct[] acts)
        {
            var names = new List<string>();

            foreach (var act in acts)
            {
                names.Add(act.ToString());
            }

            names.Sort(StringComparer.Ordinal);

            return string.Join("/", names.ToArray());
        }

        static string Acts(HashSet<FigureAct> acts)
        {
            var names = new List<string>();

            foreach (var act in acts)
            {
                names.Add(act.ToString());
            }

            names.Sort(StringComparer.Ordinal);

            return names.Count == 0 ? "nothing" : string.Join("/", names.ToArray());
        }

        static string Sides(Tally reel, string leg)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  the {0} read on both sides over {1} blow frames: the player played {2} while the enemy "
                + "played {3}, photographed as {4} frames at {5}{0}-NN.png",
                leg,
                reel.BlowFrames,
                Acts(reel.PlayerActs),
                Acts(reel.EnemyActs),
                reel.Shot,
                SidesPath);
        }

        static string Row(Tally reel, int startingPower, int enemyValue)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0} against {1}: {2}, player thrown {3:0.###} tiles, enemy {4:0.###}, {5} lit frames,"
                + " ending on node {6} at power {7}, floor {8} to {9}{10}{11}, movement held {12:0.###}s"
                + " over {13} frames{14}",
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
                reel.WeaponFlew ? ", weapon dropped" : string.Empty,
                reel.HeldFrames * Frame,
                reel.HeldFrames,
                reel.WentGhost
                    ? ", ghosted from " + reel.GhostHigh.ToString("0.###", CultureInfo.InvariantCulture)
                        + " down to " + reel.GhostLow.ToString("0.###", CultureInfo.InvariantCulture)
                    : string.Empty);
        }
    }
}
