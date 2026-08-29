using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class CameraRigCheckCommand
    {
        const long Seed = 20250824L;

        const float Frame = 1f / 60f;

        const int Ceiling = 400;

        const float ReadableReveal = 0.3f;

        const string OpeningPath = "dev/scratch/t-11-camera-opening.png";

        const string MidflightPath = "dev/scratch/t-11-camera-midflight.png";

        const string RevealPath = "dev/scratch/t-11-camera-reveal.png";

        const string LandedPath = "dev/scratch/t-11-camera-landed.png";

        const string FollowedPath = "dev/scratch/t-11-camera-followed.png";

        const string DraggedPath = "dev/scratch/t-11-camera-dragged.png";

        const string BeatPath = "dev/scratch/t-11-camera-beat.png";

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            PreviewFilm.Sun();

            rig.Begin(graph);
            PreviewFilm.Shoot(lens, OpeningPath);

            var report = new StringBuilder("camera rig on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');

            var reveal = LevelFraming.Whole(graph);
            var peak = 0f;
            var previous = rig.Framing;
            var frames = 0;
            var midflight = false;
            var revealed = false;
            var heldFrames = 0;

            while (rig.IsBusy && frames < Ceiling)
            {
                rig.Advance(Frame);
                frames++;

                peak = Peak(peak, previous, rig.Framing);
                previous = rig.Framing;

                if (rig.Framing.Equals(reveal))
                {
                    heldFrames++;
                    if (!revealed)
                    {
                        revealed = true;
                        PreviewFilm.Shoot(lens, RevealPath);
                    }
                }

                if (!midflight && frames * Frame >= CameraFlight.Seconds * 0.5f)
                {
                    PreviewFilm.Shoot(lens, MidflightPath);
                    midflight = true;
                }
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  reveal held for {0:0.###} s of the {1} s hold on {2}",
                heldFrames * Frame,
                CameraFlight.HoldSeconds,
                reveal);

            if (heldFrames * Frame < ReadableReveal)
            {
                Debug.LogError(
                    "The opening held the whole level for " + (heldFrames * Frame)
                    + "s, under the " + ReadableReveal + "s it takes to read a level off one frame.");
            }

            if (!rig.Framing.Equals(reveal))
            {
                Debug.LogError(
                    "The opening let go of input at " + rig.Framing + " rather than on the whole level "
                    + reveal + ".");
            }

            EveryTileIsOnScreen(graph, lens, reveal);

            var player = LevelFraming.Play(LevelFraming.StartPoint(graph));
            var settled = 0;
            while (!rig.Framing.Equals(player) && settled < Ceiling)
            {
                rig.Advance(Frame);
                settled++;

                peak = Peak(peak, previous, rig.Framing);
                previous = rig.Framing;
            }

            PreviewFilm.Shoot(lens, LandedPath);
            report.Append(Landing("settle onto the player", lens, player, settled, peak));

            if (settled <= 1)
            {
                Debug.LogError("The camera cut from the reveal to the player rather than easing onto them.");
            }

            var walked = FarFrom(graph);
            var chased = 0;
            var followPeak = 0f;
            previous = rig.Framing;
            rig.Follow(walked);

            while (!rig.Framing.Target.Equals(walked) && chased < Ceiling)
            {
                rig.Advance(Frame);
                chased++;

                followPeak = Peak(followPeak, previous, rig.Framing);
                previous = rig.Framing;
            }

            PreviewFilm.Shoot(lens, FollowedPath);
            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  follow reached the player at {0} in {1} frames, peak pan {2:0} px/s of {3} allowed",
                walked,
                chased,
                followPeak,
                ScreenFrame.PanCeiling);

            if (!rig.Framing.Target.Equals(walked))
            {
                Debug.LogError(
                    "The camera never reached a player standing at " + walked
                    + ", stopping at " + rig.Framing + ".");
            }

            if (followPeak > ScreenFrame.PanCeiling)
            {
                Debug.LogError(
                    "The follow panned at " + followPeak + " px/s, over the "
                    + ScreenFrame.PanCeiling + " px/s ceiling.");
            }

            var held = rig.Framing;
            rig.Look(Horizon(IsoProjection.CameraUp));
            PreviewFilm.Shoot(lens, DraggedPath);

            var showing = OnScreen(graph, lens);
            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  a drag to the horizon looks at {0}, {1} of {2} tiles still on screen",
                rig.Framing.Target,
                showing,
                graph.Tiles.Tiles.Count);

            if (rig.Framing.Equals(held))
            {
                Debug.LogError("A drag left the camera on the player rather than panning away from them.");
            }

            if (showing == 0)
            {
                Debug.LogError(
                    "A drag to the horizon left no tile of the level on screen, at " + rig.Framing + ".");
            }

            var dragged = rig.Framing;
            for (var frame = 0; frame < 60; frame++)
            {
                rig.Advance(Frame);
            }

            if (!rig.Framing.Equals(dragged))
            {
                Debug.LogError(
                    "The camera crept from " + dragged + " to " + rig.Framing
                    + " while the finger was still holding the drag.");
            }

            rig.LookBack();
            var came = 0;
            var returnPeak = 0f;
            previous = rig.Framing;

            while (!rig.Framing.Equals(held) && came < Ceiling)
            {
                rig.Advance(Frame);
                came++;

                returnPeak = Peak(returnPeak, previous, rig.Framing);
                previous = rig.Framing;
            }

            report.Append(Landing("return from a drag", lens, held, came, returnPeak));

            if (came <= 1)
            {
                Debug.LogError("Letting go of a drag cut back to the player rather than easing back.");
            }

            if (!rig.Framing.Equals(held))
            {
                Debug.LogError(
                    "Letting go of a drag left the camera at " + rig.Framing
                    + " rather than back on the player at " + held + ".");
            }

            if (returnPeak > ScreenFrame.PanCeiling)
            {
                Debug.LogError(
                    "The return from a drag panned at " + returnPeak + " px/s, over the "
                    + ScreenFrame.PanCeiling + " px/s ceiling.");
            }

            ThePlayFramingStandsTheFigureAtItsShareOfTheScreen(lens, report);

            var subject = Multiplier(graph);
            var resting = lens.orthographicSize;
            rig.CutTo(subject);

            if (!Mathf.Approximately(lens.orthographicSize, resting))
            {
                Debug.LogError(
                    "The beat opened with a cut from size " + resting + " to " + lens.orthographicSize
                    + " rather than easing in.");
            }

            var deepest = resting;
            var biggestStep = 0f;
            var previousSize = resting;
            frames = 0;

            while (rig.IsBusy && frames < Ceiling)
            {
                rig.Advance(Frame);
                frames++;

                deepest = Mathf.Min(deepest, lens.orthographicSize);
                biggestStep = Mathf.Max(biggestStep, Mathf.Abs(lens.orthographicSize - previousSize));
                previousSize = lens.orthographicSize;
            }

            PreviewFilm.Shoot(lens, BeatPath);

            var punch = resting / deepest;
            var span = resting - resting / ZoomBeat.Punch;

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  beat punches in on {0} over {1} frames from size {2:0.###} to {3:0.###}, a {4:0.###}x"
                + " punch, biggest single-frame step {5:0.####} m of {6:0.####} m",
                subject,
                frames,
                resting,
                deepest,
                punch,
                biggestStep,
                span);

            if (punch < 1.10f || punch > 1.25f)
            {
                Debug.LogError("The beat punched " + punch + "x, outside the 1.10x to 1.25x band.");
            }

            if (biggestStep >= span * 0.5f)
            {
                Debug.LogError(
                    "One frame of the punch moved the lens " + biggestStep + " m of the " + span
                    + " m it spans, which reads as a cut.");
            }

            if (frames <= 1)
            {
                Debug.LogError("The punch reached its peak in one frame, which is a cut and not an ease.");
            }

            rig.Release();

            var freeAt = lens.orthographicSize;
            var easedBack = 0;

            while (!rig.Framing.Equals(rig.Following) && easedBack < Ceiling)
            {
                rig.Advance(Frame);
                easedBack++;

                if (rig.IsBusy)
                {
                    Debug.LogError(
                        "The camera took input back at frame " + easedBack + " of its return from the beat.");
                }
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the release frees input at size {0:0.###} and the lens eases home over {1} more frames",
                freeAt,
                easedBack);

            if (rig.IsBusy)
            {
                Debug.LogError("The release left the beat still holding input.");
            }

            if (easedBack <= 1)
            {
                Debug.LogError("The beat cut back to the player rather than easing back over its return.");
            }

            report.Append(Landing("beat exit", lens, rig.Following, easedBack, 0f));

            if (!rig.Framing.Target.Equals(walked))
            {
                Debug.LogError(
                    "A beat handed the camera back at " + rig.Framing
                    + " rather than to the player it was following at " + walked + ".");
            }

            rig.Skip();
            report.Append("\n  a tap leaves the rig ").Append(rig.IsBusy ? "busy" : "free");

            Debug.Log(report.ToString());

            WorldObjects.Destroy(root);
            builder.Dispose();
        }

        static void ThePlayFramingStandsTheFigureAtItsShareOfTheScreen(Camera lens, StringBuilder report)
        {
            var standing = Vector3.zero;
            var head = standing + Vector3.up * LevelFraming.FigureHeight;
            var drawn = Mathf.Abs(
                lens.WorldToViewportPoint(head).y - lens.WorldToViewportPoint(standing).y);
            var share = LevelFraming.ShareOfScreen(LevelFraming.FigureHeight, lens.orthographicSize);

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the play lens sits at {0:0.###} against the framing's {1:0.###}, standing a {2:0.###} m"
                + " figure across {3:0.#}% of the frame's world height and {4:0.#}% of its pixels",
                lens.orthographicSize,
                LevelFraming.PlaySize,
                LevelFraming.FigureHeight,
                share * 100f,
                drawn * 100f);

            if (Mathf.Abs(share - LevelFraming.FigureHeightFraction) > 0.002f)
            {
                Debug.LogError(
                    "The live lens stands the figure across " + (share * 100f)
                    + "% of the frame where the framing asks for "
                    + (LevelFraming.FigureHeightFraction * 100f) + "%.");
            }

            if (drawn <= 0.04f || drawn >= 0.09f)
            {
                Debug.LogError(
                    "The figure draws across " + (drawn * 100f)
                    + "% of the rendered frame, nowhere near the 7% the ad reads at.");
            }
        }

        static float Peak(float peak, CameraFraming from, CameraFraming to)
        {
            var speed = ScreenFrame.PanPixels(from, to) / Frame;
            return speed > peak ? speed : peak;
        }

        static WorldPoint Horizon(WorldPoint direction)
        {
            const float Pull = 1000f;

            return new WorldPoint(direction.X * Pull, direction.Y * Pull, direction.Z * Pull);
        }

        static int OnScreen(LevelGraph graph, Camera lens)
        {
            var showing = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var drawn = lens.WorldToViewportPoint(new Vector3(point.X, point.Y, point.Z));

                if (drawn.x >= 0f && drawn.x <= 1f && drawn.y >= 0f && drawn.y <= 1f)
                {
                    showing++;
                }
            }

            return showing;
        }

        static void EveryTileIsOnScreen(LevelGraph graph, Camera lens, CameraFraming reveal)
        {
            var offScreen = graph.Tiles.Tiles.Count - OnScreen(graph, lens);

            if (offScreen > 0)
            {
                Debug.LogError(
                    offScreen + " of " + graph.Tiles.Tiles.Count
                    + " tiles sit off screen at the frame the opening reveals the level on, " + reveal + ".");
            }
        }

        static WorldPoint FarFrom(LevelGraph graph)
        {
            var start = LevelFraming.StartPoint(graph);
            var furthest = start;
            var apart = 0f;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var span = ScreenFrame.PanPixels(LevelFraming.Play(start), LevelFraming.Play(point));

                if (span > apart)
                {
                    apart = span;
                    furthest = point;
                }
            }

            return furthest;
        }

        static string Landing(string leg, Camera lens, CameraFraming constant, int frames, float peak)
        {
            var expected = new Vector3(constant.Position.X, constant.Position.Y, constant.Position.Z);
            var drift = (lens.transform.position - expected).magnitude;
            var sizeError = Mathf.Abs(lens.orthographicSize - constant.OrthographicSize);

            var row = string.Format(
                CultureInfo.InvariantCulture,
                "\n  {0} settled in {1} frames, position error {2:0.#######}, size error {3:0.#######}",
                leg,
                frames,
                drift,
                sizeError);

            if (peak <= 0f)
            {
                return row;
            }

            return row + string.Format(
                CultureInfo.InvariantCulture,
                ", peak pan {0:0} px/s of {1} allowed",
                peak,
                ScreenFrame.PanCeiling);
        }

        static TilePosition Multiplier(LevelGraph graph)
        {
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Multiplier)
                {
                    return node.Position;
                }
            }

            throw new System.InvalidOperationException(
                "Every ship level ships two multipliers for the beat to cut to, and this one has none.");
        }
    }
}
