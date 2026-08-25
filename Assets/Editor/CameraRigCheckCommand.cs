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

        const string OpeningPath = "dev/scratch/t-11-camera-opening.png";

        const string MidflightPath = "dev/scratch/t-11-camera-midflight.png";

        const string LandedPath = "dev/scratch/t-11-camera-landed.png";

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

            var peak = 0f;
            var previous = rig.Framing;
            var frames = 0;
            var midflight = false;

            while (rig.IsBusy && frames < Ceiling)
            {
                rig.Advance(Frame);
                frames++;

                var speed = ScreenFrame.PanPixels(previous, rig.Framing) / Frame;
                if (speed > peak)
                {
                    peak = speed;
                }

                previous = rig.Framing;

                if (!midflight && frames * Frame >= CameraFlight.Seconds * 0.5f)
                {
                    PreviewFilm.Shoot(lens, MidflightPath);
                    midflight = true;
                }
            }

            PreviewFilm.Shoot(lens, LandedPath);

            var constant = LevelFraming.Play(graph);
            report.Append(Landing("flight", lens, constant, frames, peak));

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

            report.Append(Landing("beat exit", lens, constant, frames, 0f));

            rig.Skip();
            report.Append("\n  a tap leaves the rig ").Append(rig.IsBusy ? "busy" : "free");

            Debug.Log(report.ToString());

            WorldObjects.Destroy(root);
            builder.Dispose();
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
