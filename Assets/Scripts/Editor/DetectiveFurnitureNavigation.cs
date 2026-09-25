using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;

namespace Detective.EditorTools
{
    public static class DetectiveFurnitureNavigation
    {
        // Mark the whole footprint, including floor beneath tabletops and chairs.
        public static int Apply(Transform root)
        {
            Physics.SyncTransforms();
            var holder = root.Find("FurnitureNavigation");
            if (holder == null) { holder = new GameObject("FurnitureNavigation").transform; holder.SetParent(root, false); }
            foreach (Transform old in holder.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            // Geometry baking already erodes by agent radius; add only a small footprint margin.
            float clearance = .12f;
            int count = 0;
            string[] prefixes = { "Table_", "Chair_", "Stool", "Counter", "Desk", "Bed", "Sofa", "TeaTable", "DiningTable", "DiningChair", "Nightstand", "Wardrobe", "DrawerCabinet", "TvCabinet", "DisplayShelf", "Shelf_", "Bench_", "Crate", "Container_", "OilDrum", "AcUnit_", "WaterTank" };
            foreach (Transform item in root)
            {
                if (!prefixes.Any(p => item.name.StartsWith(p, StringComparison.Ordinal))) continue;
                if (item.name.StartsWith("BedroomWall") || item.name == "DeskLamp" || item.name == "SofaBlanket") continue;
                var renderer = item.GetComponent<Renderer>();
                if (renderer == null) continue;
                Mark(item.name, renderer.bounds, holder, clearance); count++;
            }
            var details = root.Find("LayoutDetails");
            if (details != null)
                foreach (Transform item in details)
                {
                    if (item.name.StartsWith("Walkway_")) continue;
                    var renderers = item.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                    Mark("Details_" + item.name, bounds, holder, clearance); count++;
                }
            return count;
        }
        private static void Mark(string name, Bounds bounds, Transform holder, float clearance)
        {
            var child = holder.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(holder, false); }
            child.position = new Vector3(bounds.center.x, (Mathf.Max(bounds.max.y, 2f) - .3f) * .5f, bounds.center.z);
            var volume = child.GetComponent<NavMeshModifierVolume>();
            if (volume == null) volume = child.gameObject.AddComponent<NavMeshModifierVolume>();
            volume.area = 1;
            volume.center = Vector3.zero;
            volume.size = new Vector3(bounds.size.x + clearance * 2f, Mathf.Max(bounds.max.y, 2f) + .3f, bounds.size.z + clearance * 2f);
            EditorUtility.SetDirty(volume);
        }
    }
}
