using System;
using Parcial2.Hunter;
using UnityEngine;

namespace Parcial2.Wolves
{
    [DisallowMultipleComponent]
    public sealed class WolfPack : MonoBehaviour
    {
        public static WolfPack Instance { get; private set; }

        public HunterController Target { get; private set; }

        public bool HasAlert { get; private set; }
        public Vector3 LastAlertPosition { get; private set; }

        public event Action<WolfController, Vector3> HunterSpotted;
        public event Action<WolfController, Vector3> HowlRaised;
        public event Action<WolfController, WolfController, Vector3> AlertReceived;
        public event Action HunterSheltered;
        public event Action HunterCaught;
        public event Action HunterEscaped;

        private int targetExitsReached;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Target == null)
                return;

            if (Target.ExitsReached != targetExitsReached)
                ReportHunterEscaped();
            else if (Target.IsCaught)
                ReportHunterCaught();
            else if (Target.IsSheltered)
                ReportHunterSheltered();
        }

        public void Track(WolfController spotter, HunterController hunter, Vector3 position)
        {
            SetTarget(hunter);
            HunterSpotted?.Invoke(spotter, position);
        }

        public void ReportAlertReceived(WolfController receiver, WolfController source, Vector3 position) =>
            AlertReceived?.Invoke(receiver, source, position);

        public void RaiseHowl(WolfController source, HunterController hunter, Vector3 lastKnownPosition)
        {
            SetTarget(hunter);
            HasAlert = true;
            LastAlertPosition = lastKnownPosition;
            HowlRaised?.Invoke(source, lastKnownPosition);
        }

        public void ReportHunterSheltered()
        {
            ClearTarget();
            HunterSheltered?.Invoke();
        }

        public void ReportHunterCaught()
        {
            ClearTarget();
            HunterCaught?.Invoke();
        }

        public void ReportHunterEscaped()
        {
            ClearTarget();
            HunterEscaped?.Invoke();
        }

        private void SetTarget(HunterController hunter)
        {
            if (hunter != null && hunter != Target)
                targetExitsReached = hunter.ExitsReached;
            Target = hunter;
        }

        private void ClearTarget()
        {
            Target = null;
            HasAlert = false;
        }
    }
}
