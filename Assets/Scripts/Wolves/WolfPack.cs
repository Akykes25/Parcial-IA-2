using System;
using Parcial2.Hunter;
using UnityEngine;

namespace Parcial2.Wolves
{
    /// <summary>
    /// Scene singleton that relays the pack's signals through instance events: a howl carries the Hunter's
    /// last known position, and the pack announces when that Hunter shelters in a safe zone. Wolves subscribe
    /// to it; the pack keeps no list of wolves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WolfPack : MonoBehaviour
    {
        public static WolfPack Instance { get; private set; }

        /// <summary>The Hunter the last howl was about, until it shelters.</summary>
        public HunterController Target { get; private set; }

        public bool HasAlert { get; private set; }
        public Vector3 LastAlertPosition { get; private set; }

        public event Action<WolfController, Vector3> HunterSpotted;
        public event Action<WolfController, Vector3> HowlRaised;
        public event Action<WolfController, WolfController, Vector3> AlertReceived;
        public event Action HunterSheltered;
        public event Action HunterCaught;

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

            if (Target.IsCaught)
                ReportHunterCaught();
            else if (Target.IsSheltered)
                ReportHunterSheltered();
        }

        /// <summary>Any wolf that sees the Hunter registers it, so its sheltering is reported even without a howl.</summary>
        public void Track(WolfController spotter, HunterController hunter, Vector3 position)
        {
            Target = hunter;
            HunterSpotted?.Invoke(spotter, position);
        }

        /// <summary>Lets observers (HUD, feedback) see which wolf took in which alert.</summary>
        public void ReportAlertReceived(WolfController receiver, WolfController source, Vector3 position) =>
            AlertReceived?.Invoke(receiver, source, position);

        public void RaiseHowl(WolfController source, HunterController hunter, Vector3 lastKnownPosition)
        {
            Target = hunter;
            HasAlert = true;
            LastAlertPosition = lastKnownPosition;
            HowlRaised?.Invoke(source, lastKnownPosition);
        }

        public void ReportHunterSheltered()
        {
            ClearTarget();
            HunterSheltered?.Invoke();
        }

        /// <summary>The Hunter left the scene; without this the pack would wait for it at the respawn point.</summary>
        public void ReportHunterCaught()
        {
            ClearTarget();
            HunterCaught?.Invoke();
        }

        private void ClearTarget()
        {
            Target = null;
            HasAlert = false;
        }
    }
}
