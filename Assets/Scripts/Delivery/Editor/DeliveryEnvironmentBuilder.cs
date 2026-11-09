using UnityEditor;
using UnityEngine;

namespace CookAndDeliver.Delivery.Editor
{
    /// <summary>Places sourced Kenney road tiles and Roboden trees. No textures are painted here.</summary>
    internal static class DeliveryEnvironmentBuilder
    {
        private const string Tiles = "Assets/Art/Delivery/Environment/KenneyModernCity/Tiles/";
        private const string Prefabs = "Assets/Prefabs/Delivery/";
        private const float Ppu = 32f;
        private const int Layer = 30;

        public static void Build(DeliveryWorldView world, Material material)
        {
            var segment = new GameObject("Delivery street segment · Kenney 16px");
            // Camera crops the outer nine pixels. Asphalt edges are x=39 and x=231,
            // exactly 192 pixels apart: three 64-pixel lanes centred on 71/135/199.
            Tiled(segment.transform, material, "Asphalt", 714, 135, 192, -10);
            Tiled(segment.transform, material, "Left pavement", 741, 15, 48, -9);
            Tiled(segment.transform, material, "Right pavement", 741, 255, 48, -9);
            Tiled(segment.transform, material, "Left curb", 742, 31, 16, -8);
            Tiled(segment.transform, material, "Right curb", 740, 239, 16, -8);
            foreach (var boundary in new[] { 103, 167 })
            {
                // The original two neighbouring tiles form a two-pixel dashed line.
                Tiled(segment.transform, material, "Lane stripe left " + boundary, 786, boundary - 8, 16, -7);
                Tiled(segment.transform, material, "Lane stripe right " + boundary, 787, boundary + 8, 16, -7);
            }
            var roadPrefab = PrefabUtility.SaveAsPrefabAsset(segment, Prefabs + "DeliveryStreetSegment.prefab");
            Object.DestroyImmediate(segment);
            world.roadTiles = new Transform[4];
            for (var i = 0; i < world.roadTiles.Length; i++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab, world.transform);
                tile.name = "Street segment " + i;
                tile.transform.localPosition = new Vector3(135 / Ppu, (64 + i * 128) / Ppu, 0);
                world.roadTiles[i] = tile.transform;
            }

            var trees = new[] { "StreetTreeA", "StreetTreeB", "StreetTreeC", "StreetTreeD", "StreetTreeE", "StreetTreeF" };
            var treePrefabs = new GameObject[trees.Length];
            for (var i = 0; i < trees.Length; i++)
            {
                var tree = new GameObject("Delivery sidewalk tree " + (i + 1));
                var child = new GameObject(trees[i]) { layer = Layer }; child.transform.SetParent(tree.transform, false);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Delivery/Sourced/Trees/" + trees[i] + ".png");
                if (renderer.sprite == null) throw new System.InvalidOperationException("Missing sourced tree " + trees[i]);
                renderer.sharedMaterial = material; renderer.sortingOrder = -5;
                renderer.transform.localScale = Vector3.one;
                treePrefabs[i] = PrefabUtility.SaveAsPrefabAsset(tree, Prefabs + "DeliverySidewalkTree" + (i + 1) + ".prefab");
                Object.DestroyImmediate(tree);
            }
            world.scenery = new Transform[12];
            // Distinct original sprites, with different neighbours on each pavement.
            var treeOrder = new[] { 0, 3, 2, 5, 4, 1, 3, 0, 5, 2, 1, 4 };
            for (var i = 0; i < world.scenery.Length; i++)
            {
                var side = i % 2;
                var row = i / 2;
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefabs[treeOrder[i]], world.transform);
                tree.name = (side == 0 ? "Left" : "Right") + " sidewalk tree " + row;
                tree.transform.localPosition = new Vector3((side == 0 ? 20 : 250) / Ppu, (40 + row * 80 + side * 40) / Ppu, 0);
                world.scenery[i] = tree.transform;
            }
        }

        private static void Tiled(Transform parent, Material material, string name, int id, float x, float width, int order)
        {
            var renderer = Tile(parent, material, name, id, x - 135, 0, order);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(width / Ppu, 128 / Ppu);
        }

        private static SpriteRenderer Tile(Transform parent, Material material, string name, int id, float x, float y, int order)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + "tile_" + id.ToString("0000") + ".png");
            if (sprite == null) throw new System.InvalidOperationException("Missing Kenney environment tile " + id);
            var obj = new GameObject(name) { layer = Layer };
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x / Ppu, y / Ppu, 0);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
