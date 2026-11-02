using System.Collections.Generic;
using Parcial2.FSM;
using Parcial2.Navigation;
using UnityEngine;

namespace Parcial2.Wolves.States
{
    public sealed class InvestigateState : State
    {
        private readonly WolfController wolf;
        private readonly List<GraphNode> searchNodes = new();
        private int knowledgeVersion;
        private bool searching;
        private float searchEndTime;
        private float giveUpTime;
        private GraphNode searchNode;

        public InvestigateState(WolfController wolf)
        {
            this.wolf = wolf;
        }

        public override void Enter() => Restart();

        public override void Update()
        {
            if (knowledgeVersion != wolf.KnowledgeVersion)
                Restart();

            if (Time.time >= giveUpTime)
            {
                wolf.ForgetHunter();
                return;
            }

            if (!searching)
            {
                wolf.MoveTo(wolf.LastKnownPosition, wolf.InvestigateSpeed);
                if (wolf.HasArrived(wolf.LastKnownPosition))
                    BeginSearch();
                return;
            }

            if (Time.time >= searchEndTime)
            {
                wolf.ForgetHunter();
                return;
            }

            if (searchNode == null || wolf.HasArrived(searchNode.Position))
                searchNode = PickSearchNode();

            if (searchNode != null)
                wolf.MoveTo(searchNode.Position, wolf.InvestigateSpeed);
            else
                wolf.LookAround();
        }

        public override void Exit() => wolf.Halt();

        private void Restart()
        {
            knowledgeVersion = wolf.KnowledgeVersion;
            searching = false;
            searchNode = null;
            giveUpTime = Time.time + wolf.MaxInvestigateTime;
        }

        private void BeginSearch()
        {
            searching = true;
            searchEndTime = Time.time + wolf.SearchDuration;
            CollectSearchNodes();
        }

        private void CollectSearchNodes()
        {
            searchNodes.Clear();
            float radiusSqr = wolf.SearchRadius * wolf.SearchRadius;
            foreach (GraphNode node in wolf.Context.Graph.Nodes)
            {
                if (node != null && !node.IsSafeZone
                    && (node.Position - wolf.LastKnownPosition).sqrMagnitude <= radiusSqr)
                    searchNodes.Add(node);
            }
        }

        private GraphNode PickSearchNode()
        {
            if (searchNodes.Count == 0)
                return null;

            GraphNode next = searchNodes[Random.Range(0, searchNodes.Count)];
            if (next == searchNode && searchNodes.Count > 1)
                next = searchNodes[(searchNodes.IndexOf(next) + 1) % searchNodes.Count];

            return next;
        }
    }
}
