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

            var subject = Multiplier(graph);
            rig.CutTo(subject);
            PreviewFilm.Shoot(lens, BeatPath);
            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  beat cuts to {0} at size {1:0.###}, input {2}",
                subject,
                lens.orthographicSize,
                rig.IsBusy ? "off" : "on");

            rig.Release();
            frames = 0;
            while (rig.IsBusy && frames < Ceiling)
            {
                rig.Advance(Frame);
                frames++;
            }

            report.Append(Landing("beat exit", lens, rig.Following, frames, 0f));

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

        static float Peak(float peak, CameraFraming from, CameraFraming to)
        {
            var speed = ScreenFrame.PanPixels(from, to) / Frame;
            return speed > peak ? speed : peak;
        }

        static void EveryTileIsOnScreen(LevelGraph graph, Camera lens, CameraFraming reveal)
        {
            var offScreen = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var drawn = lens.WorldToViewportPoint(new Vector3(point.X, point.Y, point.Z));

                if (drawn.x < 0f || drawn.x > 1f || drawn.y < 0f || drawn.y > 1f)
                {
                    offScreen++;
                }
            }

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
