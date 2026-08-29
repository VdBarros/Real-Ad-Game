using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

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

        const float ProbeTolerance = 0.03f;

        const float LeastVoidShare = 0.1f;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();

            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            PreviewFilm.Sunlight();

            rig.Begin(graph);
            PreviewFilm.Shoot(lens, OpeningPath);

            var report = new StringBuilder("camera rig on ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');

            TheRigStandsTheWorldInARoomOfItsOwn(rig, lens, report);

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
            TheFrameIsGradedRatherThanSkyed(rig, lens, "the opening reveal", report);

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
            TheFrameIsGradedRatherThanSkyed(rig, lens, "a drag to the horizon", report);

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
            TheSheetCoversTheFrame(rig, lens, "the beat's deepest punch", report);

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

        static void TheRigStandsTheWorldInARoomOfItsOwn(CameraRig rig, Camera lens, StringBuilder report)
        {
            var backdrop = rig.Backdrop;

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the rig clears to {0} with {1}, grading from {2} below to {3} above",
                lens.clearFlags,
                Read(lens.backgroundColor),
                Backdrop.Below,
                Backdrop.Above);

            if (lens.clearFlags != CameraClearFlags.SolidColor)
            {
                Debug.LogError(
                    "The rig raised a camera on " + lens.clearFlags
                    + ", so the stock skybox draws behind the dungeon.");
            }

            if (Apart(Read(lens.backgroundColor), Backdrop.Clear) > ProbeTolerance)
            {
                Debug.LogError(
                    "The rig clears to " + Read(lens.backgroundColor) + " rather than the backdrop's "
                    + Backdrop.Clear + ".");
            }

            if (backdrop == null)
            {
                Debug.LogError("The rig hung no backdrop, so the void behind the dungeon is a flat clear.");
                return;
            }

            if (backdrop.transform.parent != lens.transform)
            {
                Debug.LogError("The backdrop is not carried by the camera, so a pan will slide off it.");
            }

            if (!backdrop.Face.enabled || !backdrop.gameObject.activeInHierarchy)
            {
                Debug.LogError("The backdrop hangs but does not draw.");
            }

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  the backdrop is {0} triangles of {1} on a {2}-band ramp, shadows {3}, probes {4}",
                backdrop.Sheet.triangles.Length / 3,
                backdrop.Skin.shader.name,
                backdrop.Ramp.height,
                backdrop.Face.shadowCastingMode,
                backdrop.Face.lightProbeUsage);

            if (backdrop.Sheet.vertexCount != 4 || backdrop.Sheet.triangles.Length != 6)
            {
                Debug.LogError(
                    "The backdrop costs " + backdrop.Sheet.vertexCount
                    + " vertices where a gradient costs four.");
            }

            if (backdrop.Skin.shader.name.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) < 0
                && backdrop.Skin.shader.name.IndexOf("Sprites", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Debug.LogError(
                    "The backdrop is shaded by " + backdrop.Skin.shader.name
                    + ", which costs the frame more per pixel than a gradient does.");
            }

            if (backdrop.Face.shadowCastingMode != ShadowCastingMode.Off
                || backdrop.Face.receiveShadows
                || backdrop.Face.lightProbeUsage != LightProbeUsage.Off
                || backdrop.Face.reflectionProbeUsage != ReflectionProbeUsage.Off)
            {
                Debug.LogError("The backdrop asks the renderer for light it cannot use.");
            }

            TheRoomIsLitByItsOwnAmbientAndNotBySky(report);
        }

        static void TheRoomIsLitByItsOwnAmbientAndNotBySky(StringBuilder report)
        {
            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  ambient is {0} from {1} sky through {2} to {3} ground, carrying {4:0.####}"
                + " of the {5:0.####} budget, reflection {6:0.###} off {7}",
                RenderSettings.ambientMode,
                Read(RenderSettings.ambientSkyColor),
                Read(RenderSettings.ambientEquatorColor),
                Read(RenderSettings.ambientGroundColor),
                Backdrop.AmbientLoad,
                Backdrop.AmbientBudget.Luminance,
                RenderSettings.reflectionIntensity,
                RenderSettings.skybox == null ? "no skybox" : RenderSettings.skybox.name);

            if (RenderSettings.skybox != null)
            {
                Debug.LogError(
                    "The scene still carries the skybox " + RenderSettings.skybox.name
                    + ", which lights and reflects off every pack material.");
            }

            if (RenderSettings.ambientMode != AmbientMode.Trilight)
            {
                Debug.LogError(
                    "Ambient runs on " + RenderSettings.ambientMode
                    + " rather than the three-band room the rig sets, so unlit faces read flat.");
            }

            AmbientBandHolds("sky", RenderSettings.ambientSkyColor, Backdrop.AmbientSky);
            AmbientBandHolds("equator", RenderSettings.ambientEquatorColor, Backdrop.AmbientEquator);
            AmbientBandHolds("ground", RenderSettings.ambientGroundColor, Backdrop.AmbientGround);

            if (RenderSettings.reflectionIntensity > Backdrop.ReflectionStrength)
            {
                Debug.LogError(
                    "A reflection probe still washes the pack materials at "
                    + RenderSettings.reflectionIntensity + " strength.");
            }

            if (RenderSettings.customReflectionTexture != null)
            {
                Debug.LogError("The scene reflects off a cubemap the dungeon never stood in.");
            }
        }

        static void AmbientBandHolds(string band, Color live, Tint wanted)
        {
            if (Apart(Read(live), wanted) > ProbeTolerance)
            {
                Debug.LogError(
                    "The " + band + " ambient renders at " + Read(live) + " rather than " + wanted + ".");
            }
        }

        static void TheSheetCoversTheFrame(CameraRig rig, Camera lens, string leg, StringBuilder report)
        {
            var backdrop = rig.Backdrop;
            if (backdrop == null)
            {
                return;
            }

            var aspect = lens.aspect;
            lens.aspect = (float)ScreenFrame.Width / ScreenFrame.Height;

            var lowX = float.MaxValue;
            var highX = float.MinValue;
            var lowY = float.MaxValue;
            var highY = float.MinValue;

            for (var corner = 0; corner < 4; corner++)
            {
                var local = new Vector3(corner % 2 == 0 ? -0.5f : 0.5f, corner < 2 ? -0.5f : 0.5f, 0f);
                var drawn = lens.WorldToViewportPoint(backdrop.transform.TransformPoint(local));

                lowX = Mathf.Min(lowX, drawn.x);
                highX = Mathf.Max(highX, drawn.x);
                lowY = Mathf.Min(lowY, drawn.y);
                highY = Mathf.Max(highY, drawn.y);
            }

            lens.aspect = aspect;

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  at {0} the backdrop spans x {1:0.##} to {2:0.##} and y {3:0.##} to {4:0.##} of the frame",
                leg,
                lowX,
                highX,
                lowY,
                highY);

            if (lowX > 0f || highX < 1f || lowY > 0f || highY < 1f)
            {
                Debug.LogError(
                    "At " + leg + " the backdrop leaves an edge of the frame uncovered, spanning x "
                    + lowX + " to " + highX + " and y " + lowY + " to " + highY + ".");
            }
        }

        static void TheFrameIsGradedRatherThanSkyed(
            CameraRig rig, Camera lens, string leg, StringBuilder report)
        {
            TheSheetCoversTheFrame(rig, lens, leg, report);

            var frame = PreviewFilm.Frame(lens);
            var pixels = frame.GetPixels32();
            UnityEngine.Object.DestroyImmediate(frame);

            var width = ScreenFrame.Width;
            var height = ScreenFrame.Height;
            var voided = 0;
            var voidLuminance = 0.0;
            var drawn = new List<float>();

            for (var row = 0; row < height; row++)
            {
                var wanted = Backdrop.At(row / (float)(height - 1));

                for (var column = 0; column < width; column++)
                {
                    var pixel = Read(pixels[row * width + column]);

                    if (Apart(pixel, wanted) <= ProbeTolerance)
                    {
                        voided++;
                        voidLuminance += pixel.Luminance;
                        continue;
                    }

                    drawn.Add(pixel.Luminance);
                }
            }

            var share = voided / (float)(width * height);
            var background = voided == 0
                ? 0f
                : (float)(voidLuminance / voided);

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n  {0} renders {1:0.#}% of its pixels on the backdrop ramp, mean luminance {2:0.####},"
                + " corners {3}",
                leg,
                share * 100f,
                background,
                Corners(pixels, width, height));

            if (share < LeastVoidShare)
            {
                Debug.LogError(
                    "Only " + (share * 100f) + "% of " + leg
                    + " renders on the backdrop ramp, so something else is drawing the void.");
            }

            if (drawn.Count < width * height / 100)
            {
                report.Append(" (too little drawn to weigh against it)");
                return;
            }

            drawn.Sort();

            var darkest = drawn[drawn.Count / 100];
            var middling = drawn[drawn.Count / 2];
            var brightest = drawn[drawn.Count - 1 - drawn.Count / 100];

            report.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n    what is drawn runs from a darkest hundredth at {0:0.####} for {1:0.###}:1 against the"
                + " backdrop, through a median of {2:0.####}, to a brightest hundredth at {3:0.####} for"
                + " {4:0.###}:1",
                darkest,
                Contrast(darkest, background),
                middling,
                brightest,
                Contrast(brightest, background));

            if (Contrast(darkest, background) < Backdrop.LeastFigureSeparation)
            {
                Debug.LogError(
                    "At " + leg + " the darkest hundredth of what is drawn stands only "
                    + Contrast(darkest, background) + ":1 off the backdrop, so it reads as a hole in it.");
            }

            if (Contrast(brightest, background) < Backdrop.LeastSurfaceSeparation)
            {
                Debug.LogError(
                    "At " + leg + " the lit terraces stand only " + Contrast(brightest, background)
                    + ":1 off the backdrop, so their edges do not cut against it.");
            }
        }

        static string Corners(Color32[] pixels, int width, int height)
        {
            var written = new StringBuilder();

            foreach (var corner in CornerRows(width, height))
            {
                if (written.Length > 0)
                {
                    written.Append(' ');
                }

                written.Append(Read(pixels[corner.Value * width + corner.Key]));
            }

            return written.ToString();
        }

        static IEnumerable<KeyValuePair<int, int>> CornerRows(int width, int height)
        {
            const int Inset = 2;

            yield return new KeyValuePair<int, int>(Inset, Inset);
            yield return new KeyValuePair<int, int>(width - 1 - Inset, Inset);
            yield return new KeyValuePair<int, int>(Inset, height - 1 - Inset);
            yield return new KeyValuePair<int, int>(width - 1 - Inset, height - 1 - Inset);
        }

        static float Contrast(float one, float other)
        {
            var high = one > other ? one : other;
            var low = one > other ? other : one;

            return (high + 0.05f) / (low + 0.05f);
        }

        static float Apart(Tint one, Tint other)
        {
            var red = Mathf.Abs(one.Red - other.Red);
            var green = Mathf.Abs(one.Green - other.Green);
            var blue = Mathf.Abs(one.Blue - other.Blue);

            return Mathf.Max(red, Mathf.Max(green, blue));
        }

        static Tint Read(Color colour)
        {
            return new Tint(colour.r, colour.g, colour.b);
        }

        static Tint Read(Color32 colour)
        {
            return new Tint(colour.r / 255f, colour.g / 255f, colour.b / 255f);
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
