using System;
using System.IO;
using System.Linq;
using CookAndDeliver.Delivery.Core;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CookAndDeliver.Delivery.Editor
{
    /// <summary>Rebuilds only Delivery assets. It never saves or closes another open scene.</summary>
    public static class DeliverySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Delivery.unity";
        private const string DataPath = "Assets/Data/Delivery";
        private const string PrefabPath = "Assets/Prefabs/Delivery";
        private const string ArtPath = "Assets/Art/Delivery/Sourced/";
        private const int WorldLayer = 30;
        private static readonly Color Ink = new Color32(36, 29, 26, 255);
        private static TMP_FontAsset font;
        private static Material spriteMaterial;

        [MenuItem("Tools/Delivery/Build standalone scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding Delivery.");
            var openDelivery = SceneManager.GetSceneByPath(ScenePath);
            if (openDelivery.IsValid() && openDelivery.isDirty)
                throw new InvalidOperationException("Delivery has unsaved edits. Save or close that scene before rebuilding.");
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(PrefabPath);
            AssetDatabase.Refresh();
            NormalizeSprites();
            font = PrepareFont();
            spriteMaterial = PrepareMaterial();
            var config = AssetDatabase.LoadAssetAtPath<DeliveryConfigSO>(DataPath + "/DeliveryConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DeliveryConfigSO>();
                AssetDatabase.CreateAsset(config, DataPath + "/DeliveryConfig.asset");
            }
            config.Validate();
            var texture = PrepareRenderTexture();
            var previous = SceneManager.GetActiveScene();
            // Reuse the clean target only; additive creation preserves unsaved Cooking changes.
            var scene = openDelivery.IsValid() ? openDelivery : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
                var controller = new GameObject("Delivery · Standalone").AddComponent<DeliveryController>();
                controller.config = config;
                controller.audioSource = controller.gameObject.AddComponent<AudioSource>();
                controller.audioSource.playOnAwake = false;
                controller.audioSource.volume = .6f;
                controller.tapClip = Audio("Tap"); controller.hitClip = Audio("Hit");
                controller.deliverClip = Audio("Deliver"); controller.finishClip = Audio("Finish");
                controller.world = BuildWorld(texture);
                BuildDisplayCamera(controller.transform);
                controller.ui = DeliveryUIBuilder.Build(texture, font, out var input);
                controller.input = input;
                var events = new GameObject("Delivery EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                ValidateController(controller);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("Delivery standalone scene authored and validated: " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!openDelivery.IsValid() && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void NormalizeSprites()
        {
            foreach (var file in Directory.GetFiles("Assets/Art/Delivery", "*.png", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
                importer.textureType = TextureImporterType.Sprite;
                if (path.Contains("/Sourced/") || path.Contains("/Environment/")) importer.spriteImportMode = SpriteImportMode.Single;
                if (path.Contains("/Environment/") || path.Contains("/Sourced/"))
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                }
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 32;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static TMP_FontAsset PrepareFont()
        {
            var path = DataPath + "/DeliveryFont.asset";
            var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (result != null)
            {
                result.TryAddCharacters("…");
                EditorUtility.SetDirty(result);
                return result;
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null) throw new InvalidOperationException("LiberationSans.ttf source font is required.");
            result = TMP_FontAsset.CreateFontAsset(source, 80, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            result.name = "DeliveryFont";
            AssetDatabase.CreateAsset(result, path);
            result.TryAddCharacters("…");
            result.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠƯàáâãèéêìíòóôõùúăđĩũơưẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼẾỀỂỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỲỴỶỸạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ ←→·—%/+!:.?()", out string missing);
            foreach (var atlas in result.atlasTextures)
            { if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, result); }
            if (!AssetDatabase.Contains(result.material)) AssetDatabase.AddObjectToAsset(result.material, result);
            EditorUtility.SetDirty(result);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Delivery font missing glyphs: " + missing);
            return result;
        }

        private static Material PrepareMaterial()
        {
            var path = DataPath + "/DeliverySprites.mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            result = new Material(shader) { name = "DeliverySprites" };
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static RenderTexture PrepareRenderTexture()
        {
            var path = DataPath + "/DeliveryRoad.renderTexture";
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (texture != null) return texture;
            texture = new RenderTexture(270, 360, 24, RenderTextureFormat.ARGB32)
            { name = "DeliveryRoad", filterMode = FilterMode.Point, antiAliasing = 1, useMipMap = false };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static void BuildDisplayCamera(Transform parent)
        {
            var cameraObject = new GameObject("Delivery Display Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.cullingMask = 0;
            camera.targetDisplay = 0;
            camera.targetTexture = null;
            camera.depth = -100;
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private static DeliveryWorldView BuildWorld(RenderTexture target)
        {
            var root = new GameObject("Pixel road · 270 × 360").AddComponent<DeliveryWorldView>();
            var camObject = new GameObject("Delivery Camera");
            camObject.transform.SetParent(root.transform);
            camObject.transform.localPosition = new Vector3(135f / 32, 180f / 32, -10);
            var camera = camObject.AddComponent<Camera>();
            root.worldCamera = camObject.transform;
            camera.orthographic = true; camera.orthographicSize = 180f / 32;
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink;
            camera.cullingMask = 1 << WorldLayer; camera.targetTexture = target;
            camera.allowHDR = false; camera.allowMSAA = false;
            camObject.AddComponent<AudioListener>();
            root.impact = DeliveryImpactBuilder.Build(root.transform);
            DeliveryEnvironmentBuilder.Build(root, spriteMaterial);
            var player = new GameObject("Rider and bike"); player.transform.SetParent(root.transform, false);
            root.player = player.transform;
            player.transform.localScale = Vector3.one;
            root.bike = Sprite("Bike", player.transform, LoadExisting("Assets/Art/Delivery/Bike Green.png"), 0, 0, 5);
            root.ridingPose = LoadExisting("Assets/Art/Delivery/Rider Green/Rider Green.png");
            root.idlePose = LoadExisting("Assets/Art/Delivery/Rider Green/Rider Green Idle.png");
            root.trashedPose = LoadExisting("Assets/Art/Delivery/Rider Green/Rider Green Trashed.png");
            root.rider = Sprite("Rider", player.transform, root.idlePose, 0, -2, 6);
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath + "/DeliveryPlayer.prefab");
            root.player.localPosition = new Vector3(135f / 32, 72f / 32, 0);
            var hazard = new GameObject("Pooled road hazard") { layer = WorldLayer };
            var visual = hazard.AddComponent<DeliveryHazardVisual>();
            visual.single = Sprite("Single hazard", hazard.transform, LoadSprite("Props/Pothole"), 0, 0, 2);
            visual.cones = new SpriteRenderer[4];
            for (var i = 0; i < visual.cones.Length; i++)
            {
                visual.cones[i] = Sprite("Cone " + (i + 1), hazard.transform, LoadSprite("Props/Cone"), -21 + 14 * i, 0, 2);
                visual.cones[i].transform.localScale = Vector3.one * 2f;
            }
            visual.barriers = new SpriteRenderer[2];
            for (var i = 0; i < visual.barriers.Length; i++)
            {
                visual.barriers[i] = Sprite("Barrier " + (i + 1), hazard.transform, LoadSprite("Props/Barrier"), -14 + 28 * i, 0, 2);
                visual.barriers[i].transform.localScale = Vector3.one * 2.6f;
                visual.barriers[i].gameObject.SetActive(false);
            }
            visual.single.gameObject.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(hazard, PrefabPath + "/DeliveryHazard.prefab");
            Object.DestroyImmediate(hazard);
            root.hazardPrefab = prefab.GetComponent<DeliveryHazardVisual>();
            root.hazardRoot = new GameObject("Hazard pool").transform; root.hazardRoot.SetParent(root.transform, false);
            root.hazardSprites = new[] { LoadSprite("Props/Cone"), LoadSprite("Props/Pothole"), LoadSprite("Props/Barrier"), LoadSprite("Vehicles/TrafficGreen"), LoadSprite("Vehicles/TrafficRed") };
            root.deliveryMarker = Sprite("Delivery crossing", root.transform, LoadExisting("Assets/Art/Delivery/Environment/KenneyModernCity/Tiles/tile_0827.png"), 135, 300, -2);
            root.deliveryMarker.drawMode = SpriteDrawMode.Tiled; root.deliveryMarker.size = new Vector2(192f / 32, 16f / 32);
            root.customer = Sprite("Waiting customer", root.transform, LoadSprite("Urban/Customer"), 25, 310, 2);
            root.laneHighlight = Sprite("Delivery lane highlight", root.transform, LoadExisting("Assets/Art/UI/white.png"), 71, 180, -3);
            var highlightSize = root.laneHighlight.sprite.bounds.size;
            root.laneHighlight.transform.localScale = new Vector3(64f / 32 / highlightSize.x, 360f / 32 / highlightSize.y, 1);
            root.laneHighlight.color = new Color(1, .7f, .2f, .15f);
            root.deliveryMarker.gameObject.SetActive(false); root.customer.gameObject.SetActive(false); root.laneHighlight.gameObject.SetActive(false);
            return root;
        }

        [MenuItem("Tools/Delivery/Validate loaded Delivery")]
        public static void Validate()
        {
            var controller = Object.FindFirstObjectByType<DeliveryController>(FindObjectsInactive.Include);
            if (controller == null) throw new InvalidOperationException("Open the Delivery scene before validating.");
            ValidateController(controller);
            Debug.Log("Delivery references and configuration are valid.");
        }
        private static void ValidateController(DeliveryController controller)
        {
            controller.config.Validate();
            if (controller.world == null || controller.ui == null || controller.input == null || controller.audioSource == null)
                throw new InvalidOperationException("Delivery controller has missing references.");
            var sprites = controller.world.hazardSprites;
            if (sprites == null || sprites.Length != Enum.GetValues(typeof(HazardKind)).Length)
                throw new InvalidOperationException("Delivery needs one sprite per hazard kind.");
            foreach (var kind in new[] { HazardKind.TrafficGreen, HazardKind.TrafficRed })
            {
                var sprite = sprites[(int)kind];
                if (sprite == null || sprite.rect.height <= sprite.rect.width)
                    throw new InvalidOperationException(kind + " must use a vertical, top-down car sprite facing up.");
            }
            foreach (var root in controller.gameObject.scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing script in Delivery.");
                if (!component.GetType().Namespace?.StartsWith("CookAndDeliver.Delivery") ?? true) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        throw new InvalidOperationException(component.name + " is missing " + property.propertyPath);
            }
        }

        [MenuItem("Tools/Delivery/Build Development Android")]
        public static void BuildAndroid()
        {
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Build Delivery scene first.");
            Directory.CreateDirectory("Builds/Delivery");
            var previousOrientation = PlayerSettings.defaultInterfaceOrientation;
            try
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, target = BuildTarget.Android,
                    locationPathName = "Builds/Delivery/Delivery-Development.apk", options = BuildOptions.Development
                });
                if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Delivery Android build failed: " + result.summary.result);
                Debug.Log("Delivery Android build complete: " + result.summary.outputPath);
            }
            finally { PlayerSettings.defaultInterfaceOrientation = previousOrientation; }
        }

        private static AudioClip Audio(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Delivery/" + name + ".ogg");
        private static Sprite LoadSprite(string name) => LoadExisting(ArtPath + name + ".png");
        private static Sprite LoadExisting(string path)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("Missing Delivery sprite " + path);
            return sprite;
        }
        private static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, float x, float y, int order)
        {
            var gameObject = new GameObject(name); gameObject.layer = WorldLayer; gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = new Vector3(x / 32, y / 32, 0);
            var renderer = gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order; renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }
    }
}
