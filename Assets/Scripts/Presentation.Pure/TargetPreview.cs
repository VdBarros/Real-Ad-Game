using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class TargetPreview
    {
        static readonly int[] NoRoute = new int[0];

        public static readonly TargetPreview None = new TargetPreview(TapAim.Nothing, null);

        readonly ActionResult resolved;

        TargetPreview(int nodeId, ActionResult resolved)
        {
            NodeId = nodeId;
            this.resolved = resolved;
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
                nodeId, ActionResolver.Along(navigation.State, navigation.RouteTo(nodeId)));
        }

        public int NodeId { get; }

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
