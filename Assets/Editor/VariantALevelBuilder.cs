using System.Collections.Generic;
using System.IO;
using Parcial2.Boids;
using Parcial2.Core;
using Parcial2.Editor.Levels;
using Parcial2.Hunter;
using Parcial2.Navigation;
using Parcial2.Wolves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parcial2.Editor
{
    public static class VariantALevelBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ScenePath = "Assets/Scenes/VariantA.unity";
        private const string LevelRootName = "Level Variant A";
        private const float WidestBodyRadius = 0.45f;

        [MenuItem("Tools/Build Variant A Level")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Build();
        }

        public static void Build()
        {
            Scene scene = OpenTargetScene();
            var context = FindInScene<SimulationContext>(scene);
            var pack = FindInScene<WolfPack>(scene);
            var hunter = FindInScene<HunterController>(scene);
            if (context == null || pack == null || hunter == null)
            {
                Debug.LogError($"{ScenePath} needs a SimulationContext, a WolfPack and a Hunter.");
                return;
            }

            context.gameObject.name = LevelRootName;
            Transform level = context.transform;

            LevelGeometryBuilder.BuildFloors(VariantALayout.Areas, CleanGroup(level, "Floor"));
            int wallCount = LevelGeometryBuilder.BuildWalls(VariantALayout.Areas, CleanGroup(level, "Walls"));
            List<ZoneVolume> zones = LevelGeometryBuilder.BuildZones(VariantALayout.Areas, CleanGroup(level, "Zones"));

            Transform navigation = CleanGroup(level, "Navigation");
            NavigationGraph graph = navigation.GetComponent<NavigationGraph>();
            if (graph == null)
                graph = navigation.gameObject.AddComponent<NavigationGraph>();
            List<GraphNode> nodes = LevelGraphBuilder.Build(graph, VariantALayout.Nodes, VariantALayout.Links,
                VariantALayout.NodeHeight);

            ConfigureContext(context, graph, zones);
            PlaceAgents(scene, hunter, pack, nodes);

            int problems = LevelGraphBuilder.ReportBlocked(nodes, WidestBodyRadius);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Variant A built: {wallCount} walls, {zones.Count} zones, {nodes.Count} nodes, " +
                $"{VariantALayout.Links.Length} links, {problems} clearance problems.");
        }

        private static Scene OpenTargetScene()
        {
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
                Debug.LogError($"Could not copy {SourceScenePath} to {ScenePath}.");

            Scene active = SceneManager.GetActiveScene();
            return active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigureContext(SimulationContext context, NavigationGraph graph, List<ZoneVolume> zones)
        {
            ZoneVolume start = null, hunting = null, exit = null;
            var safe = new List<ZoneVolume>();
            foreach (ZoneVolume zone in zones)
            {
                switch (zone.Type)
                {
                    case ZoneType.Start: start = zone; break;
                    case ZoneType.HuntingGround: hunting = zone; break;
                    case ZoneType.Exit: exit = zone; break;
                    default: safe.Add(zone); break;
                }
            }

            context.Configure(graph, start, hunting, exit, safe);
            EditorUtility.SetDirty(context);
        }

        private static void PlaceAgents(Scene scene, HunterController hunter, WolfPack pack, List<GraphNode> nodes)
        {
            LevelAgentsPlacer.PlaceOnFloor(hunter.transform, VariantALayout.HunterStart, VariantALayout.AgentHeight);

            var boids = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (BoidAgent boid in root.GetComponentsInChildren<BoidAgent>(true))
                    boids.Add(boid.transform);
            }

            boids.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            LevelAgentsPlacer.PlaceGroup(boids, VariantALayout.BoidSpawns, VariantALayout.AgentHeight);

            LevelAgentsPlacer.SpawnWolves(pack, VariantALayout.Routes, nodes, VariantALayout.AgentHeight);
            LevelAgentsPlacer.FrameCamera(FindInScene<Camera>(scene), VariantALayout.CameraCenter,
                VariantALayout.CameraOrthographicSize);
        }

        private static Transform CleanGroup(Transform parent, string name)
        {
            Transform group = parent.Find(name);
            if (group == null)
            {
                group = new GameObject(name).transform;
                group.SetParent(parent, false);
            }

            for (int i = group.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(group.GetChild(i).gameObject);

            return group;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
