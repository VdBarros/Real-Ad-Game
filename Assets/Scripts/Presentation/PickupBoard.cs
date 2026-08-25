using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PickupBoard : MonoBehaviour
    {
        readonly List<int> collapsing = new List<int>();

        PickupProp[] byNode;

        public bool IsSettled
        {
            get { return collapsing.Count == 0; }
        }

        internal void Begin(int nodeCount, IReadOnlyList<PickupProp> pickups, RunState opening)
        {
            if (pickups == null)
            {
                throw new ArgumentNullException(nameof(pickups));
            }

            if (opening == null)
            {
                throw new ArgumentNullException(nameof(opening));
            }

            collapsing.Clear();
            byNode = new PickupProp[nodeCount];

            foreach (var pickup in pickups)
            {
                byNode[pickup.NodeId] = pickup;
            }

            Settle(opening);
        }

        public void Settle(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            RequireABeginning();
            collapsing.Clear();

            for (var nodeId = 0; nodeId < byNode.Length; nodeId++)
            {
                if (byNode[nodeId] != null && state.IsConsumed(nodeId))
                {
                    byNode[nodeId].Wear(Take.Spent);
                }
            }

            enabled = false;
        }

        public PickupProp Of(int nodeId)
        {
            RequireABeginning();
            return nodeId >= 0 && nodeId < byNode.Length ? byNode[nodeId] : null;
        }

        public void Show(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            RequireABeginning();

            for (var nodeId = 0; nodeId < byNode.Length; nodeId++)
            {
                var pickup = byNode[nodeId];
                if (pickup == null || pickup.IsSpent || !state.IsConsumed(nodeId))
                {
                    continue;
                }

                pickup.Wear(Take.Begun());
                collapsing.Add(nodeId);
            }

            enabled = !IsSettled;
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A pickup only ever collapses forwards.");
            }

            for (var index = collapsing.Count - 1; index >= 0; index--)
            {
                var pickup = byNode[collapsing[index]];
                if (pickup == null)
                {
                    collapsing.RemoveAt(index);
                    continue;
                }

                pickup.Wear(pickup.Reel.Advanced(deltaSeconds));

                if (pickup.Reel.IsSettled)
                {
                    collapsing.RemoveAt(index);
                }
            }

            enabled = !IsSettled;
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void RequireABeginning()
        {
            if (byNode == null)
            {
                throw new InvalidOperationException(
                    "The board spends the pickups of a level it has not been given. Call Begin.");
            }
        }
    }
}
