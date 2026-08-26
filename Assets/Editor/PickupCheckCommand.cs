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
                .Append(Row(multiplyFirst));

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
            var constant = LevelFraming.Play(graph);
            var leg = new Leg { Name = name, Taps = new Tap[order.Length] };

            for (var slot = 0; slot < order.Length; slot++)
            {
                leg.Taps[slot] = Tapped(rig, builder, walker, constant, order[slot], name);
            }

            leg.Settled = walker.Run;

            ASpentPickupLeavesAPedestal(builder, walker.Run, name);
            ASpentPickupIsNoLongerATarget(walker.Run, name);
            PreviewFilm.Shoot(lens, ShotPath + name + ".png");

            WorldObjects.Destroy(root);
            WorldObjects.Destroy(rig.gameObject);
            builder.Dispose();

            return leg;
        }

        static Tap Tapped(
            CameraRig rig, WorldBuilder builder, Walker walker, CameraFraming constant, int nodeId, string leg)
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
                Watch(rig, builder, constant, tap);
            }

            if (walker.IsWalking)
            {
                Debug.LogError(
                    "Taking node " + nodeId + " in the " + leg + " leg outlasted " + FrameCap + " frames.");
            }

            for (var frame = 0; frame < SettleFrames; frame++)
            {
                Step(rig, builder, walker);
                Watch(rig, builder, constant, tap);
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

        static void Watch(CameraRig rig, WorldBuilder builder, CameraFraming constant, Tap tap)
        {
            if (!rig.Framing.Equals(constant))
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

        static void ASpentPickupLeavesAPedestal(WorldBuilder builder, RunState state, string leg)
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

                if (!pickup.gameObject.activeSelf)
                {
                    Debug.LogError(
                        "Node " + nodeId + " left the board when it was taken, so a spent node no "
                        + "longer reads as a node.");
                }

                var scale = pickup.transform.localScale;

                if (Mathf.Abs(scale.y - Take.PedestalHeight) > Tolerance
                    || Mathf.Abs(scale.x - Take.PedestalEdge) > Tolerance)
                {
                    Debug.LogError(
                        "Node " + nodeId + " stands " + scale + " where a pedestal is "
                        + Take.PedestalEdge + " wide and " + Take.PedestalHeight + " tall.");
                }

                var target = builder.Targets.Of(nodeId);

                if (target != null && target.gameObject.activeSelf)
                {
                    Debug.LogError(
                        "Node " + nodeId + " kept its badge once taken, so an empty pedestal still "
                        + "advertises a number the player cannot have.");
                }
            }
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
