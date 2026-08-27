using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Flow;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class RecentreCheckCommand
    {
        const long Seed = 20250824L;

        const float Frame = 1f / 60f;

        const int FlightCap = 600;

        const int Ceiling = 600;

        const float DragMetres = 1.4f;

        static int findings;

        public static void Check()
        {
            findings = 0;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var loop = GameLoop.Raise(Seed, MazePreset.Ship, null);
            var report = new StringBuilder("t-38: recentre button on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');

            loop.Advance(Frame);

            if (loop.Button == null)
            {
                Fail("The loop raised no recentre button at all.");
                Report(report);
                return;
            }

            if (loop.Button.IsShowing)
            {
                Fail("The recentre button was showing in " + loop.Phase + ", before play began.");
            }

            var flightFrames = 0;
            var showedInFlight = false;

            while (loop.Phase == GamePhase.Preview && flightFrames < FlightCap)
            {
                loop.Rig.Advance(Frame);
                loop.Advance(Frame);
                flightFrames++;
                showedInFlight |= loop.Button.IsShowing;
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the fly-through ran {0} frames and the button showed in {1} of them",
                flightFrames,
                showedInFlight ? "some" : "none");

            if (showedInFlight)
            {
                Fail("The recentre button showed while the opening flight owned the camera.");
            }

            if (loop.Phase != GamePhase.Play)
            {
                Fail("The loop sat in " + loop.Phase + " rather than entering play.");
                Report(report);
                return;
            }

            if (loop.Button.IsShowing)
            {
                Fail("The recentre button was showing the frame play began, with nothing panned away.");
            }

            TheButtonAndTheReaderAgree(loop, "the first frame of play");

            var resting = LevelFraming.Play(LevelFraming.StartPoint(loop.Level.Graph));
            var settled = 0;

            while (settled < Ceiling && !loop.Rig.Framing.Equals(resting))
            {
                loop.Rig.Advance(Frame);
                loop.Advance(Frame);
                settled++;
            }

            if (!loop.Rig.Framing.Equals(resting))
            {
                Fail("The follow never settled on the player; it sits at " + loop.Rig.Framing
                    + " rather than " + resting + ".");
                Report(report);
                return;
            }

            if (loop.Button.IsShowing)
            {
                Fail("The recentre button was showing with the camera settled on the player.");
            }

            loop.Rig.Look(Along(IsoProjection.CameraUp, DragMetres));
            loop.Advance(Frame);

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  a drag to {0} leaves the rig away: {1}, the button showing: {2}",
                loop.Rig.Framing.Target,
                loop.Rig.IsAway,
                loop.Button.IsShowing);

            if (loop.Rig.Framing.Equals(resting))
            {
                Fail("A drag left the camera on the player, so there was nothing to recentre.");
                Report(report);
                return;
            }

            if (!loop.Button.IsShowing)
            {
                Fail("The camera panned off the player to " + loop.Rig.Framing
                    + " and no recentre button appeared.");
            }

            TheButtonAndTheReaderAgree(loop, "a drag off the player");

            APressOnTheButtonLeavesTheMazeAlone(loop, report);
            TheButtonLeavesOnArrivalAndNotOnTheTap(loop, resting, report);
            ACommittedTapTakesTheButtonAwayUntapped(loop, report);

            loop.Tear();

            if (loop.Button.IsShowing)
            {
                Fail("A torn-down level left the recentre button on screen.");
            }

            loop.Close();
            WorldObjects.Destroy(loop.gameObject);

            Report(report);
        }

        static void APressOnTheButtonLeavesTheMazeAlone(GameLoop loop, StringBuilder report)
        {
            var finger = OnTheCall(loop);
            var framing = loop.Rig.Framing;
            var run = loop.Run;

            loop.Input.Reading(true, false, true, false, finger);

            if (!loop.Input.SwallowsThePress)
            {
                Fail("A press at " + finger + " on the recentre button was not swallowed.");
            }

            if (loop.Input.Preview.IsAimed)
            {
                Fail("A press on the recentre button aimed at node " + loop.Input.Preview.NodeId
                    + " in the maze beneath it.");
            }

            loop.Input.Reading(false, false, true, false, Along(finger, 40f));

            if (!loop.Rig.Framing.Equals(framing))
            {
                Fail("A drag that began on the recentre button panned the maze from " + framing
                    + " to " + loop.Rig.Framing + ".");
            }

            loop.Input.Reading(false, true, false, false, finger);

            if (loop.Input.SwallowsThePress)
            {
                Fail("The tap reader was still swallowing after the press on the button ended.");
            }

            if (!ReferenceEquals(loop.Run, run))
            {
                Fail("A tap on the recentre button committed a move in the maze beneath it.");
            }

            if (!loop.Rig.Framing.Equals(framing))
            {
                Fail("A tap on the recentre button moved the camera before the button was pressed.");
            }

            if (!loop.Button.IsShowing)
            {
                Fail("A swallowed press took the recentre button off the screen on its own.");
            }

            report.Append("\n  a press on the button aims at ")
                .Append(loop.Input.Preview.IsAimed ? "a node" : "nothing")
                .Append(" and leaves the camera at ")
                .Append(loop.Rig.Framing.Target.ToString());
        }

        static void TheButtonLeavesOnArrivalAndNotOnTheTap(
            GameLoop loop, CameraFraming resting, StringBuilder report)
        {
            loop.Button.Call.onClick.Invoke();
            loop.Advance(Frame);

            if (!loop.Button.IsShowing)
            {
                Fail("The recentre button left on the tap itself, with the camera still at "
                    + loop.Rig.Framing + ".");
            }

            var came = 0;
            var leftAt = -1;

            while (came < Ceiling && (loop.Button.IsShowing || !loop.Rig.Framing.Equals(resting)))
            {
                loop.Rig.Advance(Frame);
                loop.Advance(Frame);
                came++;

                if (leftAt < 0 && !loop.Button.IsShowing)
                {
                    leftAt = came;
                }
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  a tap eases home over {0} frames and the button leaves on frame {1}",
                came,
                leftAt);

            if (!loop.Rig.Framing.Equals(resting))
            {
                Fail("A tap on the recentre button left the camera at " + loop.Rig.Framing
                    + " rather than back on the player at " + resting + ".");
            }

            if (loop.Button.IsShowing)
            {
                Fail("The camera came back to " + loop.Rig.Framing
                    + " and the recentre button stayed on screen.");
            }

            if (leftAt <= 1)
            {
                Fail("The recentre button left after " + leftAt
                    + " frame, which is the tap and not the camera's arrival.");
            }

            TheButtonAndTheReaderAgree(loop, "the camera's arrival");
        }

        static void TheButtonAndTheReaderAgree(GameLoop loop, string moment)
        {
            if (loop.Button.IsShowing == loop.Input.CallShowing)
            {
                return;
            }

            Fail("At " + moment + " the button is " + (loop.Button.IsShowing ? "showing" : "gone")
                + " and the tap reader was told the call is "
                + (loop.Input.CallShowing ? "showing" : "gone") + ".");
        }

        static void ACommittedTapTakesTheButtonAwayUntapped(GameLoop loop, StringBuilder report)
        {
            loop.Rig.Look(Along(IsoProjection.CameraRight, DragMetres));
            loop.Advance(Frame);

            if (!loop.Button.IsShowing)
            {
                Fail("A second drag to " + loop.Rig.Framing + " left the recentre button hidden.");
                return;
            }

            var move = ALegalMove(loop);
            if (move < 0)
            {
                Fail("No legal move was on screen to commit, so the automatic refocus went untested.");
                return;
            }

            loop.Input.ReleaseAt(PointOf(loop, move));

            var frames = 0;
            while (frames < Ceiling && loop.Button.IsShowing)
            {
                loop.Rig.Advance(Frame);
                loop.Advance(Frame);
                frames++;
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  a committed tap on node {0} takes the button away untapped after {1} frames",
                move,
                frames);

            if (loop.Button.IsShowing)
            {
                Fail("A committed tap never took the recentre button off the screen; the camera sits at "
                    + loop.Rig.Framing + ".");
            }
        }

        static int ALegalMove(GameLoop loop)
        {
            var candidates = loop.Input.Candidates();

            for (var index = 0; index < candidates.Count; index++)
            {
                if (TargetPreview.Of(loop.Run, candidates[index].NodeId).IsLegal)
                {
                    return candidates[index].NodeId;
                }
            }

            return -1;
        }

        static ScreenPoint PointOf(GameLoop loop, int nodeId)
        {
            var candidates = loop.Input.Candidates();

            for (var index = 0; index < candidates.Count; index++)
            {
                if (candidates[index].NodeId == nodeId)
                {
                    return candidates[index].Point;
                }
            }

            return new ScreenPoint(0f, 0f);
        }

        static ScreenPoint OnTheCall(GameLoop loop)
        {
            var scale = loop.Input.FrameHeight / (float)ScreenFrame.Height;

            return new ScreenPoint(
                loop.Input.FrameWidth * 0.5f,
                (RecentreCall.Lift + RecentreCall.Height * 0.5f) * scale);
        }

        static ScreenPoint Along(ScreenPoint from, float pixels)
        {
            return new ScreenPoint(from.X + pixels, from.Y);
        }

        static WorldPoint Along(WorldPoint direction, float metres)
        {
            return new WorldPoint(direction.X * metres, direction.Y * metres, direction.Z * metres);
        }

        static void Report(StringBuilder report)
        {
            report.Append("\n  findings: ").Append(findings.ToString(CultureInfo.InvariantCulture));
            Debug.Log(report.ToString());
        }

        static void Fail(string finding)
        {
            findings++;
            Debug.LogError(finding);
        }
    }
}
