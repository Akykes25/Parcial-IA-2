using NUnit.Framework;
using Parcial2.Wolves;
using UnityEngine;

namespace Parcial2.Tests
{
    public sealed class WolfPackTests
    {
        private GameObject host;
        private WolfPack pack;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("WolfPack");
            pack = host.AddComponent<WolfPack>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        [Test]
        public void RaiseHowl_BroadcastsTheLastKnownPosition()
        {
            Vector3 received = Vector3.zero;
            int calls = 0;
            pack.HowlRaised += (_, position) =>
            {
                calls++;
                received = position;
            };

            pack.RaiseHowl(null, null, new Vector3(3f, 0f, -2f));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(received, Is.EqualTo(new Vector3(3f, 0f, -2f)));
            Assert.That(pack.HasAlert, Is.True);
            Assert.That(pack.LastAlertPosition, Is.EqualTo(new Vector3(3f, 0f, -2f)));
        }

        [Test]
        public void ReportHunterSheltered_ClearsTheAlertAndNotifiesSubscribers()
        {
            int calls = 0;
            pack.HunterSheltered += () => calls++;
            pack.RaiseHowl(null, null, Vector3.one);

            pack.ReportHunterSheltered();

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(pack.HasAlert, Is.False);
            Assert.That(pack.Target, Is.Null);
        }

        [Test]
        public void ReportHunterCaught_ClearsTheAlertAndNotifiesSubscribers()
        {
            int caught = 0;
            int sheltered = 0;
            pack.HunterCaught += () => caught++;
            pack.HunterSheltered += () => sheltered++;
            pack.RaiseHowl(null, null, Vector3.one);

            pack.ReportHunterCaught();

            Assert.That(caught, Is.EqualTo(1));
            Assert.That(sheltered, Is.Zero);
            Assert.That(pack.HasAlert, Is.False);
            Assert.That(pack.Target, Is.Null);
        }

        [Test]
        public void ReportHunterEscaped_ClearsTheAlertAndNotifiesOnlyEscapeListeners()
        {
            int escaped = 0;
            int caught = 0;
            pack.HunterEscaped += () => escaped++;
            pack.HunterCaught += () => caught++;
            pack.RaiseHowl(null, null, Vector3.one);

            pack.ReportHunterEscaped();

            Assert.That(escaped, Is.EqualTo(1));
            Assert.That(caught, Is.Zero);
            Assert.That(pack.HasAlert, Is.False);
            Assert.That(pack.Target, Is.Null);
        }

        [Test]
        public void Unsubscribed_ListenersNoLongerReceiveHowls()
        {
            int calls = 0;
            void Listener(WolfController _, Vector3 __) => calls++;
            pack.HowlRaised += Listener;
            pack.HowlRaised -= Listener;

            pack.RaiseHowl(null, null, Vector3.zero);

            Assert.That(calls, Is.Zero);
        }
    }
}
