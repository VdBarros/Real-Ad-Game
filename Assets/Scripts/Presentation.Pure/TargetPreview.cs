using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class TargetPreview
    {
        static readonly int[] NoRoute = new int[0];

        public static readonly TargetPreview None = new TargetPreview(TapAim.Nothing, null, 0);

        readonly ActionResult resolved;

        TargetPreview(int nodeId, ActionResult resolved, int fightsOnTheWay)
        {
            NodeId = nodeId;
            this.resolved = resolved;
            FightsOnTheWay = fightsOnTheWay;
            BlockedByNodeId = BlockerOn(resolved);
        }

        public static TargetPreview Of(RunState state, int nodeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (nodeId == TapAim.Nothing)
            {
                return None;
            }

            return Of(NavigationMap.Of(state), nodeId);
        }

        public static TargetPreview Of(NavigationMap navigation, int nodeId)
        {
            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            if (nodeId == TapAim.Nothing)
            {
                return None;
            }

            return new TargetPreview(
                nodeId,
                ActionResolver.Along(navigation.State, navigation.RouteTo(nodeId)),
                navigation.FightsOnTheWayTo(nodeId));
        }

        public int NodeId { get; }

        public int FightsOnTheWay { get; }

        public int BlockedByNodeId { get; }

        public bool IsDangerous
        {
            get { return BlockedByNodeId != TapAim.Nothing; }
        }

        static int BlockerOn(ActionResult walked)
        {
            if (walked == null || walked.Route.Count < 2)
            {
                return TapAim.Nothing;
            }

            var route = walked.Route;
            var halted = walked.State.PositionNodeId;

            if (route[route.Count - 1] == halted)
            {
                return TapAim.Nothing;
            }

            for (var step = 0; step + 1 < route.Count; step++)
            {
                if (route[step] == halted)
                {
                    return route[step + 1];
                }
            }

            return TapAim.Nothing;
        }

        public bool IsAimed
        {
            get { return NodeId != TapAim.Nothing; }
        }

        public bool IsLegal
        {
            get { return Outcome != ActionOutcome.Rejected; }
        }

        public ActionOutcome Outcome
        {
            get { return resolved == null ? ActionOutcome.Rejected : resolved.Outcome; }
        }

        public int Power
        {
            get { return resolved == null ? 0 : resolved.State.Power; }
        }

        public IReadOnlyList<int> Route
        {
            get { return resolved == null ? NoRoute : resolved.Route; }
        }
    }
}
