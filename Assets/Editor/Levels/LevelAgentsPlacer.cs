using System.Collections.Generic;
using Parcial2.Wolves;
using Parcial2.Navigation;
using UnityEditor;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    /// <summary>Moves the scene's agents and camera onto a freshly built level and spawns its wolves.</summary>
    internal static class LevelAgentsPlacer
    {
        private const string WolfPrefabPath = "Assets/Prefabs/Wolf.prefab";

        public static void PlaceOnFloor(Transform target, Vector2 position, float height)
        {
            target.position = new Vector3(position.x, height, position.y);
            EditorUtility.SetDirty(target);
        }

        public static void PlaceGroup(IReadOnlyList<Transform> targets, IReadOnlyList<Vector2> positions, float height)
        {
            if (targets.Count != positions.Count)
                Debug.LogWarning($"Placing {targets.Count} agents on {positions.Count} spawn points.");

            for (int i = 0; i < targets.Count && i < positions.Count; i++)
                PlaceOnFloor(targets[i], positions[i], height);
        }

        /// <summary>Replaces every wolf under the pack with one prefab instance per route, at its first waypoint.</summary>
        public static void SpawnWolves(WolfPack pack, IReadOnlyList<RouteSpec> routes, IReadOnlyList<GraphNode> nodes,
            float height)
        {
            for (int i = pack.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(pack.transform.GetChild(i).gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WolfPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Wolf prefab not found at {WolfPrefabPath}.");
                return;
            }

            foreach (RouteSpec spec in routes)
            {
                var wolf = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pack.transform);
                wolf.name = spec.WolfName;

                var waypoints = new List<GraphNode>(spec.Waypoints.Length);
                foreach (int index in spec.Waypoints)
                    waypoints.Add(nodes[index]);

                Vector3 first = waypoints[0].Position;
                wolf.transform.position = new Vector3(first.x, height, first.z);

                PatrolRoute route = wolf.GetComponent<PatrolRoute>();
                route.Configure(spec.Mode, waypoints);
                PrefabUtility.RecordPrefabInstancePropertyModifications(route);
                PrefabUtility.RecordPrefabInstancePropertyModifications(wolf.transform);
            }
        }

        public static void FrameCamera(Camera camera, Vector2 center, float orthographicSize)
        {
            if (camera == null)
                return;

            Vector3 position = camera.transform.position;
            camera.transform.position = new Vector3(center.x, position.y, center.y);
            camera.orthographicSize = orthographicSize;
            EditorUtility.SetDirty(camera.transform);
            EditorUtility.SetDirty(camera);
        }
    }
}
