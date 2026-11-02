using System;
using System.Collections.Generic;
using Parcial2.Steering;
using UnityEngine;

namespace Parcial2.Navigation
{
    public sealed class NavigationAgent
    {
        private const float DestinationMovedSqr = 0.25f;
        private const float ProgressDistanceSqr = 0.04f;
        private const float StuckTimeout = 0.75f;
        private const float DirectRouteCooldown = 2f;
        private const float MaxThreatDetourFactor = 2f;

        private readonly NavigationGraph graph;
        private readonly AStarPathfinder pathfinder = new();
        private readonly List<GraphNode> path = new();
        private readonly List<GraphNode> scratchPath = new();
        private readonly PathFollower follower;
        private readonly float agentRadius;
        private readonly float slowingRadius;
        private readonly int obstacleMask;
        private readonly float clearanceCheckInterval;
        private readonly Func<GraphNode, GraphNode, bool> threatEdgeFilter;

        private Vector3 destination;
        private Vector3 lastGoalQueryPosition;
        private GraphNode goalNode;
        private GraphNode unreachableGoal;
        private bool hasDestination;
        private bool directRoute;
        private float nextClearanceCheck;
        private Vector3 progressPosition;
        private float progressTime;
        private float directRouteBlockedUntil = float.MinValue;
        private bool recoveringFromStuck;

        public NavigationAgent(NavigationGraph graph, float agentRadius, float reachDistance, float slowingRadius,
            int obstacleMask, float clearanceCheckInterval = 0.2f)
        {
            this.graph = graph;
            this.agentRadius = agentRadius;
            this.slowingRadius = slowingRadius;
            this.obstacleMask = obstacleMask;
            this.clearanceCheckInterval = clearanceCheckInterval;
            follower = new PathFollower(reachDistance, slowingRadius, HasClearRoute);
            threatEdgeFilter = (from, to) => Threats.IsSegmentClear(from.Position, to.Position);
        }

        public Predicate<GraphNode> NodeFilter { get; set; }

        public INavigationThreats Threats { get; set; }

        public bool IsAvoidingThreats { get; private set; }

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;
        public bool IsFollowingPath => hasDestination && !directRoute && path.Count > 0;
        public IReadOnlyList<GraphNode> Path => path;
        public int PathRequests { get; private set; }

        public event Action<IReadOnlyList<GraphNode>> PathCalculated;

        public void SetDestination(Vector3 target)
        {
            if (!hasDestination || (target - destination).sqrMagnitude > DestinationMovedSqr)
            {
                nextClearanceCheck = float.MinValue;
                progressTime = float.MaxValue;
            }

            destination = target;
            hasDestination = true;
        }

        public void Stop()
        {
            hasDestination = false;
            directRoute = false;
            directRouteBlockedUntil = float.MinValue;
            recoveringFromStuck = false;
            progressTime = float.MaxValue;
            goalNode = null;
            unreachableGoal = null;
            path.Clear();
            follower.Clear();
        }

        public Vector3 Tick(Vector3 position, float maxSpeed, float time)
        {
            if (!hasDestination)
                return Vector3.zero;

            if (time >= nextClearanceCheck)
            {
                nextClearanceCheck = time + clearanceCheckInterval;
                directRoute = time >= directRouteBlockedUntil && HasClearRoute(position, destination)
                    && (Threats == null || Threats.IsSegmentClear(position, destination));
                if (directRoute)
                    ClearPath();
            }

            if (directRoute)
            {
                if (!IsStuck(position, time))
                    return SteeringBehaviours.Arrive(position, destination, maxSpeed, slowingRadius);

                directRoute = false;
                directRouteBlockedUntil = time + DirectRouteCooldown;
                ClearPath();
                recoveringFromStuck = true;
            }

            RefreshGoalNode();
            if (goalNode == null)
                return Vector3.zero;

            bool pathMatchesGoal = path.Count > 0 && path[^1] == goalNode;
            if (!pathMatchesGoal && goalNode != unreachableGoal)
                Repath(position, time);
            else if (pathMatchesGoal && IsStuck(position, time))
            {
                recoveringFromStuck = true;
                Repath(position, time);
            }

            if (path.Count == 0)
                return Vector3.zero;

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

        public bool TryGetPathCost(Vector3 from, Vector3 to, out float cost, bool avoidThreats = false)
        {
            if (!avoidThreats || Threats == null)
                return pathfinder.TryFindPath(graph, from, to, scratchPath, out cost, NodeFilter);

            GraphNode start = FindStartNode(from);
            GraphNode goal = graph.GetClosestNode(to, NodeFilter);
            return pathfinder.TryFindPath(start, goal, scratchPath, out cost, NodeFilter, threatEdgeFilter);
        }

        private GraphNode FindStartNode(Vector3 position)
        {
            if (Threats == null)
                return graph.GetClosestNode(position, NodeFilter);

            GraphNode best = null;
            float bestSqr = float.MaxValue;
            IReadOnlyList<GraphNode> nodes = graph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                GraphNode node = nodes[i];
                if (node == null || (NodeFilter != null && !NodeFilter(node)))
                    continue;

                float sqr = (node.Position - position).sqrMagnitude;
                if (sqr >= bestSqr || !Threats.IsSegmentClear(position, node.Position)
                    || !HasClearRoute(position, node.Position))
                    continue;

                best = node;
                bestSqr = sqr;
            }

            return best ?? graph.GetClosestNode(position, NodeFilter);
        }

        private void SkipPassedStartNode(Vector3 position)
        {
            if (path.Count < 2)
                return;

            Vector3 next = path[1].Position;
            if (HasClearRoute(position, next) && (Threats == null || Threats.IsSegmentClear(position, next)))
                path.RemoveAt(0);
        }

        public void InvalidatePath()
        {
            nextClearanceCheck = float.MinValue;
            ClearPath();
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

        private bool IsStuck(Vector3 position, float time)
        {
            Vector3 moved = position - progressPosition;
            moved.y = 0f;
            Vector3 toDestination = destination - position;
            toDestination.y = 0f;
            bool slowingDown = toDestination.sqrMagnitude <= slowingRadius * slowingRadius;
            if (moved.sqrMagnitude > ProgressDistanceSqr || slowingDown || time < progressTime)
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
            bool recovering = recoveringFromStuck;
            recoveringFromStuck = false;
            GraphNode startNode = recovering ? graph.GetClosestNode(position, NodeFilter) : FindStartNode(position);
            if (!pathfinder.TryFindPath(startNode, goalNode, scratchPath, out float plainCost, NodeFilter))
            {
                path.Clear();
                unreachableGoal = goalNode;
                follower.Clear();
                return;
            }

            IsAvoidingThreats = Threats != null
                && pathfinder.TryFindPath(startNode, goalNode, path, out float avoidingCost, NodeFilter, threatEdgeFilter)
                && avoidingCost <= plainCost * MaxThreatDetourFactor;
            if (!IsAvoidingThreats)
            {
                path.Clear();
                path.AddRange(scratchPath);
            }

            if (!recovering)
                SkipPassedStartNode(position);
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
