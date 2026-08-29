using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class NavigationMap
    {
        const int Unvisited = -1;

        readonly RunState state;
        readonly int[] arrivedFrom;
        readonly int[] fightsOnTheWay;
        readonly List<int> navigableNodes;

        NavigationMap(RunState state, int[] arrivedFrom, int[] fightsOnTheWay, List<int> navigableNodes)
        {
            this.state = state;
            this.arrivedFrom = arrivedFrom;
            this.fightsOnTheWay = fightsOnTheWay;
            this.navigableNodes = navigableNodes;
        }

        public static NavigationMap Of(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var decisions = state.Level.Decisions;
            var count = decisions.Nodes.Count;
            var from = new int[count];
            var fights = new int[count];

            for (var nodeId = 0; nodeId < count; nodeId++)
            {
                from[nodeId] = Unvisited;
            }

            var found = new List<int>();
            var here = new List<int> { state.PositionNodeId };
            var barring = new List<int>();
            from[state.PositionNodeId] = state.PositionNodeId;

            for (var toll = 0; here.Count > 0; toll++)
            {
                barring.Clear();

                for (var head = 0; head < here.Count; head++)
                {
                    var nodeId = here[head];
                    found.Add(nodeId);

                    if (nodeId != state.PositionNodeId && state.BlocksPassage(nodeId))
                    {
                        barring.Add(nodeId);
                        continue;
                    }

                    Open(decisions, from, fights, here, nodeId, toll);
                }

                var beyond = new List<int>();
                foreach (var nodeId in barring)
                {
                    Open(decisions, from, fights, beyond, nodeId, toll + 1);
                }

                here = beyond;
            }

            found.Sort();
            return new NavigationMap(state, from, fights, found);
        }

        static void Open(
            DecisionGraph decisions, int[] from, int[] fights, List<int> waiting, int nodeId, int toll)
        {
            foreach (var neighbour in decisions.NeighboursOf(nodeId))
            {
                if (from[neighbour] != Unvisited)
                {
                    continue;
                }

                from[neighbour] = nodeId;
                fights[neighbour] = toll;
                waiting.Add(neighbour);
            }
        }

        public RunState State
        {
            get { return state; }
        }

        public IReadOnlyList<int> NavigableNodes
        {
            get { return navigableNodes; }
        }

        public bool IsNavigable(int nodeId)
        {
            RequireNode(nodeId);
            return arrivedFrom[nodeId] != Unvisited;
        }

        public int FightsOnTheWayTo(int nodeId)
        {
            RequireNode(nodeId);
            return IsNavigable(nodeId) ? fightsOnTheWay[nodeId] : 0;
        }

        public IReadOnlyList<int> RouteTo(int nodeId)
        {
            if (!IsNavigable(nodeId))
            {
                return null;
            }

            var route = new List<int>();
            for (var step = nodeId; step != state.PositionNodeId; step = arrivedFrom[step])
            {
                route.Add(step);
            }

            route.Add(state.PositionNodeId);
            route.Reverse();
            return route;
        }

        void RequireNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= arrivedFrom.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "No node carries that id.");
            }
        }
    }
}
