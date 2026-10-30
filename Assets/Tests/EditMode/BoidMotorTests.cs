using System.Reflection;
using NUnit.Framework;
using Parcial2.Core;
using UnityEngine;

namespace Parcial2.Tests
{
    public sealed class BoidMotorTests
    {
        private GameObject zoneObject;

        [TearDown]
        public void TearDown()
        {
            if (zoneObject != null)
                Object.DestroyImmediate(zoneObject);
        }

        [Test]
        public void ConstrainPosition_ClampsToHuntingZoneAndReflectsVelocity()
        {
            zoneObject = new GameObject("Hunting Ground Test Zone");
            BoxCollider bounds = zoneObject.AddComponent<BoxCollider>();
            bounds.size = new Vector3(10f, 2f, 10f);
            ZoneVolume zone = zoneObject.AddComponent<ZoneVolume>();
            zone.Configure(ZoneType.HuntingGround);

            System.Type motorType = typeof(ZoneVolume).Assembly.GetType("Parcial2.Boids.BoidMotor");
            Assert.That(motorType, Is.Not.Null, "The Boids motor must exist in the runtime assembly.");

            MethodInfo constrain = motorType.GetMethod("ConstrainPosition", BindingFlags.Public | BindingFlags.Static);
            Assert.That(constrain, Is.Not.Null, "The motor must expose its deterministic boundary response.");

            object[] arguments =
            {
                zone,
                new Vector3(6f, 0f, -6f),
                new Vector3(3f, 0f, -4f),
                0.5f
            };

            Vector3 result = (Vector3)constrain.Invoke(null, arguments);
            Vector3 reflectedVelocity = (Vector3)arguments[2];

            Assert.That(zone.Contains(result), Is.True);
            Assert.That(result.x, Is.EqualTo(4.5f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(-4.5f).Within(0.001f));
            Assert.That(reflectedVelocity.x, Is.LessThan(0f));
            Assert.That(reflectedVelocity.z, Is.GreaterThan(0f));
        }
    }
}
