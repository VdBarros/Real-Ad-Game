using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Game.Domain;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class PickupCheckCommand
    {
        const long Seed = 20250824L;

        const string Preset = "ship";

        const string ShotPath = "dev/scratch/t-16-";

        const string TakePath = "dev/scratch/t-44-";

        const float CloseUpRange = 12f;

        const float CloseUpSize = 0.9f;

        const float CloseUpLift = 0.3f;

        const int ReloadFrames = 30;

        const int Additive = 0;

        const int Multiplier = 2;

        const int AdditiveValue = 5;

        const int MultiplierValue = 3;

        const int EnemyValue = 4;

        const string GatePath = "dev/scratch/t-138-";

        const float GalleryCentre = 3f;

        const float GalleryLift = 0.5f;

        const float GalleryRange = 16f;

        const float GallerySize = 5f;

        const float ColourReach = 0.45f;

        const int Power = 2;

        const float Frame = 1f / 60f;

        const int FrameCap = 4000;

        const int SettleFrames = 180;

        const float Tolerance = 0.001f;

        sealed class Tap
        {
            public int NodeId;
            public int Before;
            public int Expected;
            public int After;
            public int CutFrames;
            public int TickedFrames;
        }

        sealed class Leg
        {
            public string Name;
            public Tap[] Taps;
            public RunState Settled;
        }

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreviewFilm.Sun();

            var addFirst = Took("add-then-multiply", Additive, Multiplier);
            var multiplyFirst = Took("multiply-then-add", Multiplier, Additive);

            var report = new StringBuilder("pickups taken from power ")
                .Append(Power.ToString(CultureInfo.InvariantCulture))
                .Append(", the same two, in both orders:")
                .Append(Row(addFirst))
                .Append(Row(multiplyFirst))
                .Append(TheLidSwingsUpBeforeTheChestThins())
                .Append(AResumedLevelShowsNothingOfAChestItAlreadyTook())
                .Append(AMultiplierIsAnArchOnTheGroundAndNotABadge())
                .Append(TheFourNumericMeaningsWearFourDifferentForms());

            TheOrderShowsInTheResult(addFirst, multiplyFirst);

            Debug.Log(report.ToString());
        }

        static LevelGraph Arena()
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            for (var x = 0; x < 5; x++)
            {
                builder.AddTile(At(x), regionId: 0);
            }

            builder.AddNode(At(0), NodeType.Additive, AdditiveValue);
            builder.AddNode(At(2), NodeType.Start);
            builder.AddNode(At(4), NodeType.Multiplier, MultiplierValue);

            builder.Connect(At(2), At(0), new[] { At(1) });
            builder.Connect(At(2), At(4), new[] { At(3) });

            return builder.Build();
        }

        static TilePosition At(int x)
        {
            return new TilePosition(elevation: 0, x: x, y: 0);
        }

        static Leg Took(string name, params int[] order)
        {
            var graph = Arena();
            var rig = CameraRig.Raise();
            var lens = rig.GetComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var opening = RunState.Begin(graph, Power);
            var input = TapInput.Raise(rig, builder.Targets, opening);
            var walker = Walker.Raise(rig, builder, input, opening);
            var leg = new Leg { Name = name, Taps = new Tap[order.Length] };

            for (var slot = 0; slot < order.Length; slot++)
            {
                leg.Taps[slot] = Tapped(rig, builder, walker, order[slot], name);
            }

            leg.Settled = walker.Run;

            NothingOfASpentPickupIsLeftOnTheTile(builder, root, walker.Run, name);
            ASpentPickupIsNoLongerATarget(walker.Run, name);
            PreviewFilm.Shoot(lens, ShotPath + name + ".png");

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return leg;
        }

        static Tap Tapped(
            CameraRig rig, WorldBuilder builder, Walker walker, int nodeId, string leg)
        {
            var tap = new Tap
            {
                NodeId = nodeId,
                Before = walker.Run.Power,
                Expected = ActionResolver.Resolve(walker.Run, nodeId).State.Power
            };

            if (!TapAim.Aimable(walker.Run).Contains(nodeId))
            {
                Debug.LogError(
                    "The " + leg + " leg cannot aim at node " + nodeId + ", which it has yet to take.");
            }

            walker.WalkTo(nodeId);

            if (!walker.IsWalking)
            {
                Debug.LogError("A tap on node " + nodeId + " in the " + leg + " leg started no walk.");
            }

            for (var frame = 0; frame < FrameCap && walker.IsWalking; frame++)
            {
                Step(rig, builder, walker);
                Watch(rig, builder, tap);
            }

            if (walker.IsWalking)
            {
                Debug.LogError(
                    "Taking node " + nodeId + " in the " + leg + " leg outlasted " + FrameCap + " frames.");
            }

            for (var frame = 0; frame < SettleFrames; frame++)
            {
                Step(rig, builder, walker);
                Watch(rig, builder, tap);
            }

            tap.After = walker.Run.Power;

            if (tap.After != tap.Expected)
            {
                Debug.LogError(
                    "Taking node " + nodeId + " took power " + tap.Before + " to " + tap.After
                    + " rather than the " + tap.Expected + " the resolver promised.");
            }

            if (tap.TickedFrames == 0)
            {
                Debug.LogError(
                    "Power went from " + tap.Before + " to " + tap.After
                    + " with the badge never showing a number in between, so nothing counted up.");
            }

            TheBeatOnlyMarksAMultiplier(walker.Run, nodeId, tap, leg);

            return tap;
        }

        static void Watch(CameraRig rig, WorldBuilder builder, Tap tap)
        {
            if (!rig.Framing.Equals(rig.Following))
            {
                tap.CutFrames++;
            }

            if (builder.PlayerBadge == null)
            {
                return;
            }

            var shown = builder.PlayerBadge.Shown;
            if (shown > tap.Before && shown < tap.Expected)
            {
                tap.TickedFrames++;
            }
        }

        static void TheBeatOnlyMarksAMultiplier(RunState state, int nodeId, Tap tap, string leg)
        {
            var multiplier = state.Level.Decisions.Node(nodeId).Type == NodeType.Multiplier;

            if (multiplier && tap.CutFrames == 0)
            {
                Debug.LogError(
                    "The " + leg + " leg took a multiplier with the camera never cutting away, so the "
                    + "order-of-operations moment passed unmarked.");
            }

            if (!multiplier && tap.CutFrames > 0)
            {
                Debug.LogError(
                    "The " + leg + " leg held a beat for " + (tap.CutFrames * Frame)
                    + "s over an additive. An additive is additive; only a multiplier earns a close-up.");
            }
        }

        static void NothingOfASpentPickupIsLeftOnTheTile(
            WorldBuilder builder, GameObject root, RunState state, string leg)
        {
            foreach (var nodeId in new[] { Additive, Multiplier })
            {
                var pickup = builder.Pickups.Of(nodeId);

                if (pickup == null)
                {
                    Debug.LogError("The arena raised no pickup for node " + nodeId + ".");
                    continue;
                }

                if (!state.IsConsumed(nodeId))
                {
                    Debug.LogError("The " + leg + " leg left node " + nodeId + " untaken.");
                    continue;
                }

                if (!pickup.IsSpent || !pickup.Reel.IsSettled)
                {
                    Debug.LogError(
                        "Node " + nodeId + " reads as " + pickup.Reel + " after the " + leg
                        + " leg settled.");
                }

                if (pickup.Draws || pickup.gameObject.activeSelf)
                {
                    Debug.LogError(
                        "Node " + nodeId + " still puts " + Stands(pickup).size
                        + " of reward on the tile after the " + leg
                        + " leg took it, so the chest was opened and then left lying there.");
                }

                if (pickup.Reel.Opacity > 0f)
                {
                    Debug.LogError(
                        "Node " + nodeId + " reads " + pickup.Reel.Opacity
                        + " opaque after the " + leg + " leg took it.");
                }

                ATakenTileIsStillATile(root, state, nodeId, leg);

                var target = builder.Targets.Of(nodeId);

                if (target != null && target.gameObject.activeSelf)
                {
                    Debug.LogError(
                        "Node " + nodeId + " kept the thing the player tapped once taken, so an empty "
                        + "tile still advertises a reward that cannot be had.");
                }
            }
        }

        static void ATakenTileIsStillATile(
            GameObject root, RunState state, int nodeId, string leg)
        {
            var name = PartNames.Tile(state.Level.Decisions.Node(nodeId).Position);

            foreach (var part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part.name != name)
                {
                    continue;
                }

                var skin = part.GetComponentInChildren<Renderer>(true);

                if (skin == null || !skin.enabled || !skin.gameObject.activeInHierarchy)
                {
                    Debug.LogError(
                        "The tile under node " + nodeId + " stopped drawing when the " + leg
                        + " leg took the reward standing on it, so the chest took the floor with it.");
                }

                return;
            }

            Debug.LogError(
                "The " + leg + " leg left no tile named " + name + " under node " + nodeId + ".");
        }

        static string TheLidSwingsUpBeforeTheChestThins()
        {
            var graph = Arena();
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var chest = builder.Pickups.Of(Additive);

            if (chest == null)
            {
                Debug.LogError("The arena raised no pickup for node " + Additive + ".");
                Clear(root, rig, builder);
                return "\n  the chest never rose";
            }

            if (chest.Lid == null)
            {
                Debug.LogError(
                    "The chest on node " + Additive + " carries no " + PickupProp.LidNode
                    + " node, so a taken reward can only vanish rather than open.");
            }

            var eye = PreviewFilm.Rig(
                chest.transform.position + Vector3.up * CloseUpLift, CloseUpRange, CloseUpSize);

            chest.Wear(Take.None);
            var shut = LidHeight(chest);
            PreviewFilm.Shoot(eye, TakePath + "chest-1-shut.png");

            chest.Wear(Take.Begun().Advanced(Take.Seconds * Take.LidShare));
            var open = LidHeight(chest);
            var opened = chest.Reel;
            PreviewFilm.Shoot(eye, TakePath + "chest-2-open.png");

            if (open <= shut + Tolerance)
            {
                Debug.LogError(
                    "The lid sat at " + shut + " shut and " + open
                    + " open, so it does not swing up and the take reads as a chest sinking.");
            }

            if (opened.Opacity < 1f || !chest.Draws)
            {
                Debug.LogError(
                    "The chest was already " + opened.Opacity
                    + " opaque the moment its lid finished opening, so the lid never reads.");
            }

            chest.Wear(Take.Begun().Advanced(Take.Seconds * (Take.LidShare + 1f) * 0.5f));
            var thinning = chest.Reel;
            PreviewFilm.Shoot(eye, TakePath + "chest-3-thinning.png");

            if (thinning.Opacity <= 0f || thinning.Opacity >= 1f || !chest.Draws)
            {
                Debug.LogError(
                    "Halfway through the fade the chest reads " + thinning.Opacity
                    + " opaque and drawing " + chest.Draws + ", so it snaps out rather than fading.");
            }

            chest.Wear(Take.Spent);
            PreviewFilm.Shoot(eye, TakePath + "chest-4-gone.png");

            if (chest.Draws || chest.gameObject.activeSelf)
            {
                Debug.LogError(
                    "The chest still puts " + Stands(chest).size
                    + " on the tile once the take is spent.");
            }

            WorldObjects.Destroy(eye.gameObject);
            Clear(root, rig, builder);

            return new StringBuilder("\n  the lid rises from ")
                .Append(shut.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" to ")
                .Append(open.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" over the first ")
                .Append((Take.Seconds * Take.LidShare).ToString("F3", CultureInfo.InvariantCulture))
                .Append("s, then the chest thins to ")
                .Append(thinning.Opacity.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" and is gone")
                .ToString();
        }

        static string AResumedLevelShowsNothingOfAChestItAlreadyTook()
        {
            var graph = Arena();
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var resumed = ActionResolver.Resolve(RunState.Begin(graph, Power), Additive).State;
            var input = TapInput.Raise(rig, builder.Targets, resumed);
            var walker = Walker.Raise(rig, builder, input, resumed);
            var chest = builder.Pickups.Of(Additive);

            if (!resumed.IsConsumed(Additive))
            {
                Debug.LogError(
                    "The resume fixture never consumed node " + Additive
                    + ", so it proves nothing about reloading onto a spent node.");
            }

            if (chest == null)
            {
                Debug.LogError("The resumed arena raised no pickup for node " + Additive + ".");
                Clear(root, rig, builder);
                return "\n  the resumed chest never rose";
            }

            if (!chest.Reel.IsSpent || !chest.Reel.IsSettled || !chest.Reel.IsGone)
            {
                Debug.LogError(
                    "A resumed level reads node " + Additive + " as " + chest.Reel
                    + " on its first frame rather than gone.");
            }

            if (chest.Draws || chest.gameObject.activeSelf)
            {
                Debug.LogError(
                    "A resumed level puts " + Stands(chest).size + " of chest back on node "
                    + Additive + ", so a reward already taken is standing there again.");
            }

            for (var frame = 0; frame < ReloadFrames; frame++)
            {
                builder.Pickups.Advance(Frame);
            }

            if (chest.Draws || !builder.Pickups.IsSettled)
            {
                Debug.LogError(
                    "A resumed level plays the take again on node " + Additive
                    + ", so the chest fades out afresh on every reload.");
            }

            var eye = PreviewFilm.Rig(
                chest.transform.position + Vector3.up * CloseUpLift, CloseUpRange, CloseUpSize);
            PreviewFilm.Shoot(eye, TakePath + "chest-5-resumed.png");
            WorldObjects.Destroy(eye.gameObject);

            var power = walker.Run.Power;
            Clear(root, rig, builder);

            return "\n  a level resumed on power " + power.ToString(CultureInfo.InvariantCulture)
                + " shows nothing of node " + Additive + " and never replays its take";
        }

        static string AMultiplierIsAnArchOnTheGroundAndNotABadge()
        {
            var graph = Arena();
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph);

            rig.Begin(graph);
            rig.Skip();

            var gateName = PartNames.Node(Multiplier);
            var gate = Find(root, gateName);
            var arch = gate == null ? null : gate.GetComponent<GateProp>();

            if (gate == null || arch == null)
            {
                Debug.LogError(
                    "FAIL: node " + Multiplier + " raised no " + gateName
                    + " carrying a gate arch, so a multiplier is still whatever it used to be.");
                Clear(root, rig, builder);
                return "\n  the gate never rose";
            }

            foreach (var badge in gate.GetComponentsInChildren<NumberBadge>(true))
            {
                Debug.LogError(
                    "FAIL: the multiplier gate carries a badge named " + badge.name
                    + " showing " + badge.Value + ", so it is still a number to be read.");
            }

            foreach (var text in gate.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                Debug.LogError(
                    "FAIL: the multiplier gate carries the text " + text.text
                    + ", so its factor is still written out rather than built.");
            }

            foreach (var sprite in gate.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Debug.LogError(
                    "FAIL: the multiplier gate carries the sprite " + sprite.name
                    + ", so a badge plate is still hanging on it.");
            }

            foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
            {
                if (badge.name == PartNames.Badge(Multiplier))
                {
                    Debug.LogError(
                        "FAIL: the level raised " + badge.name
                        + " somewhere else in the hierarchy, so the gate badge only moved house.");
                }
            }

            if (arch.Pips != MultiplierValue)
            {
                Debug.LogError(
                    "FAIL: an x" + MultiplierValue + " gate counts " + arch.Pips
                    + " pips along its lintel, so its factor cannot be read off the object.");
            }

            if (arch.Factor != MultiplierValue)
            {
                Debug.LogError(
                    "FAIL: the gate glows for a factor of " + arch.Factor + " rather than "
                    + MultiplierValue + ".");
            }

            var target = builder.Targets.Of(Multiplier);

            if (target == null)
            {
                Debug.LogError("FAIL: the multiplier gate is no longer a tap target at all.");
            }
            else
            {
                if (target.Gate == null)
                {
                    Debug.LogError("FAIL: the multiplier target marks something other than the arch.");
                }

                if (target.Badge != null)
                {
                    Debug.LogError("FAIL: the multiplier target still wears its mark on a badge.");
                }
            }

            var box = Stands(gate.GetComponent<PickupProp>());
            var floor = IsoProjection.Of(graph.Decisions.Node(Multiplier).Position).Y;
            var span = box.size.x > box.size.z ? box.size.x : box.size.z;
            var stands = box.max.y - floor;

            if (stands < LevelBlueprintBuilder.FigureScale * 2f)
            {
                Debug.LogError(
                    "FAIL: the arch stands only " + stands.ToString("F3", CultureInfo.InvariantCulture)
                    + " over its tile, which the player is taller than, so nothing is walked through.");
            }

            if (box.min.y > floor + Tolerance)
            {
                Debug.LogError(
                    "FAIL: the arch floats " + (box.min.y - floor).ToString("F3", CultureInfo.InvariantCulture)
                    + " over its tile rather than standing on it.");
            }

            var walkway = Walkway(gate);

            if (walkway < LevelBlueprintBuilder.FigureScale)
            {
                Debug.LogError(
                    "FAIL: the posts leave a gap of " + walkway.ToString("F3", CultureInfo.InvariantCulture)
                    + ", which the player cannot pass through.");
            }

            var eye = PreviewFilm.Rig(gate.position, CloseUpRange, CloseUpSize);
            PreviewFilm.Shoot(eye, GatePath + "arch.png");
            WorldObjects.Destroy(eye.gameObject);
            Clear(root, rig, builder);

            return new StringBuilder("\n  the x")
                .Append(MultiplierValue.ToString(CultureInfo.InvariantCulture))
                .Append(" gate is an arch ")
                .Append(span.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" across and ")
                .Append(stands.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" tall, walked through a ")
                .Append(walkway.ToString("F3", CultureInfo.InvariantCulture))
                .Append(" gap, wearing ")
                .Append(arch.Pips.ToString(CultureInfo.InvariantCulture))
                .Append(" pips and no badge, sprite or glyph of any kind")
                .ToString();
        }

        static string TheFourNumericMeaningsWearFourDifferentForms()
        {
            var graph = Gallery();
            var rig = CameraRig.Raise();
            var builder = new WorldBuilder();
            var root = builder.Build(graph, Power);

            rig.Begin(graph);
            rig.Skip();

            var shapes = new Dictionary<BadgeStyle, BadgeShape>();
            var colours = new Dictionary<BadgeStyle, Color>();

            foreach (var badge in root.GetComponentsInChildren<NumberBadge>(true))
            {
                if (badge.GetComponent<SpriteRenderer>() == null)
                {
                    Debug.LogError("FAIL: badge " + badge.name + " draws no plate behind its number.");
                }

                shapes[badge.Style] = BadgeStyles.ShapeOf(badge.Style);
                colours[badge.Style] = BadgePalette.Of(badge.Style);
            }

            foreach (var pair in shapes)
            {
                foreach (var other in shapes)
                {
                    if (Family(pair.Key) == Family(other.Key))
                    {
                        continue;
                    }

                    if (pair.Value == other.Value && Near(colours[pair.Key], colours[other.Key]))
                    {
                        Debug.LogError(
                            "FAIL: a " + pair.Key + " badge and a " + other.Key + " badge are both a "
                            + pair.Value + " in the same colour, so only their numbers tell them apart.");
                    }
                }
            }

            if (!shapes.ContainsKey(BadgeStyle.Player)
                || !shapes.ContainsKey(BadgeStyle.Additive)
                || !shapes.ContainsKey(BadgeStyle.Enemy))
            {
                Debug.LogError(
                    "FAIL: the gallery raised only " + shapes.Count
                    + " of the three badge meanings, so it proves nothing about telling them apart.");
            }

            var gates = 0;
            foreach (var arch in root.GetComponentsInChildren<GateProp>(true))
            {
                gates++;

                if (arch.GetComponentInChildren<NumberBadge>(true) != null)
                {
                    Debug.LogError("FAIL: a gallery gate wears a badge.");
                }
            }

            if (gates == 0)
            {
                Debug.LogError("FAIL: the gallery raised no gate at all.");
            }

            var eye = PreviewFilm.Rig(
                new Vector3(GalleryCentre, GalleryLift, 0f), GalleryRange, GallerySize);
            PreviewFilm.Shoot(eye, GatePath + "four-meanings.png");
            WorldObjects.Destroy(eye.gameObject);
            Clear(root, rig, builder);

            var line = new StringBuilder("\n  four meanings, four forms: ")
                .Append(gates.ToString(CultureInfo.InvariantCulture))
                .Append(" multiplier gate(s) wearing no badge, plus");

            foreach (var pair in shapes)
            {
                line.Append(' ')
                    .Append(pair.Key.ToString())
                    .Append(" as a ")
                    .Append(pair.Value.ToString())
                    .Append(" in ")
                    .Append(Hex(colours[pair.Key]))
                    .Append(';');
            }

            return line.ToString();
        }

        static LevelGraph Gallery()
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            for (var x = 0; x < 7; x++)
            {
                builder.AddTile(At(x), regionId: 0);
            }

            builder.AddNode(At(0), NodeType.Additive, AdditiveValue);
            builder.AddNode(At(2), NodeType.Start);
            builder.AddNode(At(4), NodeType.Multiplier, MultiplierValue);
            builder.AddNode(At(6), NodeType.Enemy, EnemyValue);

            builder.Connect(At(2), At(0), new[] { At(1) });
            builder.Connect(At(2), At(4), new[] { At(3) });
            builder.Connect(At(4), At(6), new[] { At(5) });

            return builder.Build();
        }

        static string Family(BadgeStyle style)
        {
            return style == BadgeStyle.Boss ? BadgeStyle.Enemy.ToString() : style.ToString();
        }

        static bool Near(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) + Mathf.Abs(left.g - right.g) + Mathf.Abs(left.b - right.b)
                < ColourReach;
        }

        static string Hex(Color colour)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(colour);
        }

        static float Walkway(Transform gate)
        {
            var left = gate.Find(PartNames.GateLeftPost);
            var right = gate.Find(PartNames.GateRightPost);

            if (left == null || right == null)
            {
                return 0f;
            }

            return Vector3.Distance(left.position, right.position)
                - (left.lossyScale.x + right.lossyScale.x) * 0.5f;
        }

        static Transform Find(GameObject root, string name)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == name)
                {
                    return part;
                }
            }

            return null;
        }

        static float LidHeight(PickupProp pickup)
        {
            if (pickup.Lid == null)
            {
                return pickup.transform.position.y;
            }

            var skin = pickup.Lid.GetComponentInChildren<Renderer>(true);

            return skin == null ? pickup.Lid.position.y : skin.bounds.center.y;
        }

        static void Clear(GameObject root, CameraRig rig, WorldBuilder builder)
        {
            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();
        }

        static Bounds Stands(PickupProp pickup)
        {
            var skins = pickup.GetComponentsInChildren<Renderer>(true);
            var stands = skins.Length == 0
                ? new Bounds(pickup.transform.position, Vector3.zero)
                : skins[0].bounds;

            foreach (var skin in skins)
            {
                stands.Encapsulate(skin.bounds);
            }

            return stands;
        }

        static void ASpentPickupIsNoLongerATarget(RunState state, string leg)
        {
            var aimable = TapAim.Aimable(state);

            foreach (var nodeId in new[] { Additive, Multiplier })
            {
                if (!state.IsConsumed(nodeId))
                {
                    continue;
                }

                if (aimable.Contains(nodeId))
                {
                    Debug.LogError(
                        "The " + leg + " leg leaves spent node " + nodeId
                        + " aimable, so an empty pedestal is still a tap target.");
                }

                var again = ActionResolver.Resolve(state, nodeId);

                if (again.State.Power != state.Power)
                {
                    Debug.LogError(
                        "A second tap on spent node " + nodeId + " moved power from " + state.Power
                        + " to " + again.State.Power + ".");
                }
            }
        }

        static void TheOrderShowsInTheResult(Leg addFirst, Leg multiplyFirst)
        {
            var addedThenMultiplied = (Power + AdditiveValue) * MultiplierValue;
            var multipliedThenAdded = Power * MultiplierValue + AdditiveValue;

            if (addFirst.Settled.Power != addedThenMultiplied)
            {
                Debug.LogError(
                    "Adding then multiplying ended on " + addFirst.Settled.Power + " rather than "
                    + addedThenMultiplied + ".");
            }

            if (multiplyFirst.Settled.Power != multipliedThenAdded)
            {
                Debug.LogError(
                    "Multiplying then adding ended on " + multiplyFirst.Settled.Power + " rather than "
                    + multipliedThenAdded + ".");
            }

            if (addFirst.Settled.Power == multiplyFirst.Settled.Power)
            {
                Debug.LogError(
                    "Both orders ended on " + addFirst.Settled.Power
                    + ", so the order the player took them in is invisible in the result.");
            }

            if (addFirst.Settled.ConsumedNodes.Count != multiplyFirst.Settled.ConsumedNodes.Count)
            {
                Debug.LogError(
                    "The two orders spent a different number of pickups, so the gap between them is "
                    + "more than the order alone.");
            }
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

        static string Row(Leg leg)
        {
            var row = new StringBuilder("\n  ").Append(leg.Name).Append(':');

            foreach (var tap in leg.Taps)
            {
                row.AppendFormat(
                    CultureInfo.InvariantCulture,
                    " node {0} took {1} to {2} over {3} counted frames and {4} cut frames;",
                    tap.NodeId,
                    tap.Before,
                    tap.After,
                    tap.TickedFrames,
                    tap.CutFrames);
            }

            return row.Append(" ending on power ")
                .Append(leg.Settled.Power.ToString(CultureInfo.InvariantCulture))
                .ToString();
        }
    }
}
