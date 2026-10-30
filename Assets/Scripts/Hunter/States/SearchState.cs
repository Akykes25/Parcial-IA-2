using System.Collections.Generic;
using Parcial2.Core;
using Parcial2.FSM;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Hunter.States
{
    /// <summary>
    /// Initial state: travel to the hunting ground with A*, then roam between random hunting-ground nodes
    /// and periodically drop interest objects while inside.
    /// </summary>
    public sealed class SearchState : State
    {
        private readonly HunterController hunter;
        private readonly List<GraphNode> groundNodes = new();
        private GraphNode roamNode;
        private float nextSpawnTime;

        public SearchState(HunterController hunter)
        {
            this.hunter = hunter;
        }

        public override void Enter()
        {
            roamNode = null;
            nextSpawnTime = Time.time + hunter.InterestSpawnInterval;
        }

        public override void Update()
        {
            ZoneVolume ground = hunter.Context.HuntingGround;
            if (!ground.Contains(hunter.Position))
            {
                roamNode = null;
                hunter.MoveTo(ground.Center, hunter.SearchSpeed);
                return;
            }

            if (Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + hunter.InterestSpawnInterval;
                hunter.Spawner.TrySpawn(ground);
            }

            if (roamNode == null || hunter.HasArrived(roamNode.Position))
                roamNode = PickRoamNode(ground);

            if (roamNode != null)
                hunter.MoveTo(roamNode.Position, hunter.SearchSpeed);
        }

        private GraphNode PickRoamNode(ZoneVolume ground)
        {
            if (groundNodes.Count == 0)
            {
                foreach (GraphNode node in hunter.Context.Graph.Nodes)
                {
                    if (node != null && ground.Contains(node.Position))
                        groundNodes.Add(node);
                }
            }

            if (groundNodes.Count == 0)
                return null;

            GraphNode next = groundNodes[Random.Range(0, groundNodes.Count)];
            if (next == roamNode && groundNodes.Count > 1)
                next = groundNodes[(groundNodes.IndexOf(next) + 1) % groundNodes.Count];

            return next;
        }
    }
}
