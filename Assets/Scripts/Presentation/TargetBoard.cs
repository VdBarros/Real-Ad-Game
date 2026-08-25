using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class TargetBoard : MonoBehaviour
    {
        readonly List<NodeTarget> targets = new List<NodeTarget>();

        NodeTarget[] byNode;

        public IReadOnlyList<NodeTarget> Targets
        {
            get { return targets; }
        }

        internal void Begin(int nodeCount)
        {
            targets.Clear();
            byNode = new NodeTarget[nodeCount];
        }

        internal void Adopt(NodeTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            RequireABeginning();
            targets.Add(target);
            byNode[target.NodeId] = target;
        }

        public NodeTarget Of(int nodeId)
        {
            RequireABeginning();
            return nodeId >= 0 && nodeId < byNode.Length ? byNode[nodeId] : null;
        }

        public void Show(RunState state, TargetPreview preview)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            RequireABeginning();

            foreach (var target in targets)
            {
                target.Wear(TargetMarks.Of(state, target.NodeId, preview), preview.Power);
            }
        }

        void RequireABeginning()
        {
            if (byNode == null)
            {
                throw new InvalidOperationException(
                    "The board marks the nodes of a level it has not been given. Call Begin.");
            }
        }
    }
}
