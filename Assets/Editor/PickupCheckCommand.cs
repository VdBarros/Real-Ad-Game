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
                .Append(AResumedLevelShowsNothingOfAChestItAlreadyTook());

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
                        "Node " + nodeId + " kept its badge once taken, so an empty tile still "
                        + "advertises a number the player cannot have.");
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
