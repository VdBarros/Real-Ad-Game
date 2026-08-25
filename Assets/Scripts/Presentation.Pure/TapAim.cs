using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TapAim
    {
        public const int Nothing = -1;

        public const float AnchorLift = LevelBlueprintBuilder.FigureScale;

        public static IReadOnlyList<int> Aimable(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var aimable = new List<int>();
            if (state.IsLevelComplete)
            {
                return aimable;
            }

            foreach (var nodeId in state.ReachableNodes)
            {
                WorldPart prop;
                if (nodeId == state.PositionNodeId
                    || !LevelBlueprintBuilder.TryProp(state.Level.Decisions.Node(nodeId), out prop))
                {
                    continue;
                }

                aimable.Add(nodeId);
            }

            return aimable;
        }

        public static WorldPoint AnchorOf(DecisionNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            WorldPart prop;
            if (!LevelBlueprintBuilder.TryProp(node, out prop))
            {
                throw new ArgumentException(
                    "Node " + node + " has nothing raised over it, so there is nothing to aim at.", nameof(node));
            }

            var tile = IsoProjection.Of(node.Position);
            return new WorldPoint(tile.X, tile.Y + AnchorLift, tile.Z);
        }

        public static IReadOnlyList<TapCandidate> Candidates(
            RunState state, CameraFraming framing, int screenWidth, int screenHeight)
        {
            var candidates = new List<TapCandidate>();

            foreach (var nodeId in Aimable(state))
            {
                var anchor = AnchorOf(state.Level.Decisions.Node(nodeId));
                candidates.Add(new TapCandidate(
                    nodeId,
                    ScreenProjection.Of(framing, anchor, screenWidth, screenHeight),
                    framing.DepthOf(anchor)));
            }

            return candidates;
        }

        public static int Of(IReadOnlyList<TapCandidate> candidates, ScreenPoint finger, float reachPixels)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (reachPixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reachPixels), reachPixels, "A finger always covers some of the screen.");
            }

            var aimed = Nothing;
            var nearest = 0f;
            var shallowest = 0f;

            foreach (var candidate in candidates)
            {
                var distance = ScreenPoint.Distance(finger, candidate.Point);
                if (distance > reachPixels)
                {
                    continue;
                }

                if (aimed != Nothing && !Beats(distance, candidate, nearest, shallowest, aimed))
                {
                    continue;
                }

                aimed = candidate.NodeId;
                nearest = distance;
                shallowest = candidate.Depth;
            }

            return aimed;
        }

        static bool Beats(float distance, TapCandidate candidate, float nearest, float shallowest, int aimed)
        {
            if (distance != nearest)
            {
                return distance < nearest;
            }

            if (candidate.Depth != shallowest)
            {
                return candidate.Depth < shallowest;
            }

            return candidate.NodeId < aimed;
        }
    }
}
