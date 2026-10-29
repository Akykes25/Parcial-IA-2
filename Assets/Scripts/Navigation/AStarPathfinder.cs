using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parcial2.Navigation
{
    /// <summary>
    /// A* over a <see cref="NavigationGraph"/>. One instance owns its work buffers and can be reused
    /// across searches. Wolves and the Hunter share this implementation.
    /// </summary>
    public class AStarPathfinder
    {
        private struct OpenEntry
        {
            public GraphNode Node;
            public float F;
        }

        private readonly List<OpenEntry> open = new();
        private readonly Dictionary<GraphNode, float> gScore = new();
        private readonly Dictionary<GraphNode, GraphNode> cameFrom = new();

        public bool TryFindPath(NavigationGraph graph, Vector3 from, Vector3 to, List<GraphNode> result,
            out float cost, Predicate<GraphNode> filter = null)
        {
            GraphNode start = graph.GetClosestNode(from, filter);
            GraphNode goal = graph.GetClosestNode(to, filter);

            if (start == null || goal == null)
            {
                result.Clear();
                cost = 0f;
                return false;
            }

            return TryFindPath(start, goal, result, out cost, filter);
        }

        public bool TryFindPath(GraphNode start, GraphNode goal, List<GraphNode> result, out float cost,
            Predicate<GraphNode> filter = null)
        {
            result.Clear();
            cost = 0f;

            if (start == null || goal == null)
                return false;

            open.Clear();
            gScore.Clear();
            cameFrom.Clear();

            gScore[start] = 0f;
            Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                OpenEntry current = Pop();
                float currentG = gScore[current.Node];

                // A cheaper route to this node was found after this entry was queued.
                if (current.F > currentG + Heuristic(current.Node, goal) + 0.0001f)
                    continue;

                if (current.Node == goal)
                {
                    cost = currentG;
                    BuildPath(goal, result);
                    return true;
                }

                Expand(current.Node, currentG, goal, filter);
            }

            return false;
        }

        private void Expand(GraphNode node, float nodeG, GraphNode goal, Predicate<GraphNode> filter)
        {
            foreach (GraphNode neighbor in node.Neighbors)
            {
                if (neighbor == null || (filter != null && !filter(neighbor)))
                    continue;

                float tentative = nodeG + Vector3.Distance(node.Position, neighbor.Position);
                if (gScore.TryGetValue(neighbor, out float known) && tentative >= known)
                    continue;

                gScore[neighbor] = tentative;
                cameFrom[neighbor] = node;
                Push(neighbor, tentative + Heuristic(neighbor, goal));
            }
        }

        private void BuildPath(GraphNode goal, List<GraphNode> result)
        {
            GraphNode step = goal;
            result.Add(step);
            while (cameFrom.TryGetValue(step, out GraphNode previous))
            {
                result.Add(previous);
                step = previous;
            }

            result.Reverse();
        }

        private static float Heuristic(GraphNode a, GraphNode b) => Vector3.Distance(a.Position, b.Position);

        private void Push(GraphNode node, float f)
        {
            open.Add(new OpenEntry { Node = node, F = f });
            int child = open.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (open[parent].F <= open[child].F)
                    break;

                (open[parent], open[child]) = (open[child], open[parent]);
                child = parent;
            }
        }

        private OpenEntry Pop()
        {
            OpenEntry top = open[0];
            int last = open.Count - 1;
            open[0] = open[last];
            open.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;

                if (left < open.Count && open[left].F < open[smallest].F)
                    smallest = left;
                if (right < open.Count && open[right].F < open[smallest].F)
                    smallest = right;
                if (smallest == parent)
                    break;

                (open[parent], open[smallest]) = (open[smallest], open[parent]);
                parent = smallest;
            }

            return top;
        }
    }
}
