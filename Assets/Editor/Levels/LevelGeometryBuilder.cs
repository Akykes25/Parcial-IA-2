using System.Collections.Generic;
using Parcial2.Core;
using UnityEditor;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    internal static class LevelGeometryBuilder
    {
        private const string MaterialFolder = "Assets/Materials/Level/";
        private const string DefaultMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat";
        private const float WallThickness = 0.2f;
        private const float WallHeight = 3f;
        private const float FloorThickness = 0.3f;
        private const float ZoneFloorLift = 0.01f;
        private const float ZoneHeight = 2f;

        public static void BuildFloors(IReadOnlyList<AreaSpec> areas, Transform parent)
        {
            Material defaultMaterial = LoadMaterial(DefaultMaterialPath);
            foreach (AreaSpec area in areas)
            {
                bool isZone = area.Zone.HasValue;
                Material material = isZone ? LoadMaterial(MaterialFolder + ZoneMaterialName(area.Zone.Value)) : defaultMaterial;
                float top = isZone ? ZoneFloorLift : 0f;
                CreateBox(area.Name, parent, area.Bounds, top - FloorThickness, FloorThickness, 0, material);
            }
        }

        public static int BuildWalls(IReadOnlyList<AreaSpec> areas, Transform parent)
        {
            var walkable = new List<Rect>(areas.Count);
            foreach (AreaSpec area in areas)
                walkable.Add(area.Bounds);

            Material material = LoadMaterial(DefaultMaterialPath);
            List<Rect> walls = WallOutline.Build(walkable, WallThickness);
            for (int i = 0; i < walls.Count; i++)
                CreateBox($"Wall {i + 1:00}", parent, walls[i], 0f, WallHeight, GameLayers.Obstacle, material);

            return walls.Count;
        }

        public static List<ZoneVolume> BuildZones(IReadOnlyList<AreaSpec> areas, Transform parent)
        {
            var zones = new List<ZoneVolume>();
            foreach (AreaSpec area in areas)
            {
                if (!area.Zone.HasValue)
                    continue;

                var go = new GameObject($"{area.Name} Zone", typeof(BoxCollider));
                go.transform.SetParent(parent, false);
                Place(go.transform, area.Bounds, 0f, ZoneHeight);
                ZoneVolume zone = go.AddComponent<ZoneVolume>();
                zone.Configure(area.Zone.Value);
                zones.Add(zone);
            }

            return zones;
        }

        private static void CreateBox(string name, Transform parent, Rect area, float bottom, float height, int layer,
            Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = layer;
            box.transform.SetParent(parent, false);
            Place(box.transform, area, bottom, height);
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void Place(Transform target, Rect area, float bottom, float height)
        {
            target.position = new Vector3(area.center.x, bottom + height * 0.5f, area.center.y);
            target.localScale = new Vector3(area.width, height, area.height);
        }

        private static string ZoneMaterialName(ZoneType type) => type switch
        {
            ZoneType.Start => "mat_Start.mat",
            ZoneType.HuntingGround => "mat_Hunting.mat",
            ZoneType.Safe => "mat_Safe.mat",
            _ => "mat_Exit.mat"
        };

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                Debug.LogWarning($"Level material not found: {path}");
            return material;
        }
    }
}
