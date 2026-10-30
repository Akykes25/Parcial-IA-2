using System;
using System.Collections.Generic;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Navigation
{
    /// <summary>
    /// Single movement rule shared by every graph agent: when the route to the destination is clear,
    /// steer straight to it; otherwise run A* and follow the path with steering. The clearance check runs
    /// on an interval and A* only runs again when the destination maps to a different graph node.
    /// </summary>
    public sealed class NavigationAgent
    {
        private const float DestinationMovedSqr = 0.25f;
        private const float ProgressDistanceSqr = 0.04f;
        private const float StuckTimeout = 0.75f;

        private readonly NavigationGraph graph;
        private readonly AStarPathfinder pathfinder = new();
        private readonly List<GraphNode> path = new();
        private readonly List<GraphNode> scratchPath = new();
        private readonly PathFollower follower;
        private readonly float agentRadius;
        private readonly float slowingRadius;
        private readonly int obstacleMask;
        private readonly float clearanceCheckInterval;

        private Vector3 destination;
        private Vector3 lastGoalQueryPosition;
        private GraphNode goalNode;
        private GraphNode unreachableGoal;
        private bool hasDestination;
        private bool directRoute;
        private float nextClearanceCheck;
        private Vector3 progressPosition;
        private float progressTime;

        public NavigationAgent(NavigationGraph graph, float agentRadius, float reachDistance, float slowingRadius,
            int obstacleMask, float clearanceCheckInterval = 0.2f)
        {
            this.graph = graph;
            this.agentRadius = agentRadius;
            this.slowingRadius = slowingRadius;
            this.obstacleMask = obstacleMask;
            this.clearanceCheckInterval = clearanceCheckInterval;
            follower = new PathFollower(reachDistance, slowingRadius);
        }

        /// <summary>Nodes this agent may not use, e.g. wolves skip safe-zone nodes.</summary>
        public Predicate<GraphNode> NodeFilter { get; set; }

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;
        public bool IsFollowingPath => hasDestination && !directRoute && path.Count > 0;
        public IReadOnlyList<GraphNode> Path => path;
        public int PathRequests { get; private set; }

        public event Action<IReadOnlyList<GraphNode>> PathCalculated;

        public void SetDestination(Vector3 target)
        {
            if (!hasDestination || (target - destination).sqrMagnitude > DestinationMovedSqr)
                nextClearanceCheck = float.MinValue;

            destination = target;
            hasDestination = true;
        }

        public void Stop()
        {
            hasDestination = false;
            directRoute = false;
            goalNode = null;
            unreachableGoal = null;
            path.Clear();
            follower.Clear();
        }

        /// <summary>Returns the desired velocity for this frame.</summary>
        public Vector3 Tick(Vector3 position, float maxSpeed, float time)
        {
            if (!hasDestination)
                return Vector3.zero;

            if (time >= nextClearanceCheck)
            {
                nextClearanceCheck = time + clearanceCheckInterval;
                directRoute = HasClearRoute(position, destination);
                if (directRoute)
                    ClearPath();
            }

            if (directRoute)
                return SteeringBehaviours.Arrive(position, destination, maxSpeed, slowingRadius);

            RefreshGoalNode();
            if (goalNode == null)
                return Vector3.zero;

            bool pathMatchesGoal = path.Count > 0 && path[^1] == goalNode;
            if (!pathMatchesGoal && goalNode != unreachableGoal)
                Repath(position, time);
            else if (pathMatchesGoal && IsStuck(position, time))
                Repath(position, time);

            if (path.Count == 0)
                return Vector3.zero;

            // The last node is next to the destination; close the gap with steering.
            return follower.IsComplete
                ? SteeringBehaviours.Arrive(position, destination, maxSpeed, slowingRadius)
                : follower.Evaluate(position, maxSpeed);
        }

        public bool HasArrived(Vector3 position, float tolerance)
        {
            Vector3 offset = destination - position;
            offset.y = 0f;
            return hasDestination && offset.sqrMagnitude <= tolerance * tolerance;
        }

        /// <summary>Length of the A* path between two points, used to rank destinations.</summary>
        public bool TryGetPathCost(Vector3 from, Vector3 to, out float cost)
        {
            return pathfinder.TryFindPath(graph, from, to, scratchPath, out cost, NodeFilter);
        }

        public bool HasClearRoute(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance < 0.01f)
                return true;

            return !Physics.SphereCast(from, agentRadius, offset / distance, out _, distance, obstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void RefreshGoalNode()
        {
            if (goalNode != null && (destination - lastGoalQueryPosition).sqrMagnitude <= DestinationMovedSqr)
                return;

            lastGoalQueryPosition = destination;
            goalNode = graph.GetClosestNode(destination, NodeFilter);
            unreachableGoal = null;
        }

        /// <summary>No progress for a while (e.g. snagged on a corner): plan again from where we are.</summary>
        private bool IsStuck(Vector3 position, float time)
        {
            Vector3 moved = position - progressPosition;
            moved.y = 0f;
            if (moved.sqrMagnitude > ProgressDistanceSqr)
            {
                progressPosition = position;
                progressTime = time;
                return false;
            }

            return time - progressTime >= StuckTimeout;
        }

        private void Repath(Vector3 position, float time)
        {
            PathRequests++;
            progressPosition = position;
            progressTime = time;
            GraphNode startNode = graph.GetClosestNode(position, NodeFilter);
            if (!pathfinder.TryFindPath(startNode, goalNode, path, out _, NodeFilter))
            {
                unreachableGoal = goalNode;
                follower.Clear();
                return;
            }

            follower.SetPath(path);
            PathCalculated?.Invoke(path);
        }

        private void ClearPath()
        {
            unreachableGoal = null;
            path.Clear();
            follower.Clear();
        }
    }
}
