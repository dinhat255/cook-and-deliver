using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CookAndDeliver.Delivery.Editor
{
    internal static class DeliveryImpactBuilder
    {
        public static DeliveryImpactFeedback Build(Transform parent)
        {
            const string path = "Assets/Data/Delivery/DeliveryImpact.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new System.InvalidOperationException("URP particle shader is required.");
                material = new Material(shader) { name = "DeliveryImpact" };
                AssetDatabase.CreateAsset(material, path);
            }
            // Tiny square pixel fragments using Cooking's existing white texture, not new art.
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/white.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);

            var root = new GameObject("Delivery impact · sparks and dust") { layer = 30 };
            var effect = root.AddComponent<DeliveryImpactFeedback>();
            effect.sparks = Particles(root.transform, "Pixel sparks", material, 12, false);
            effect.dust = Particles(root.transform, "Road dust", material, 8, true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Delivery/DeliveryImpact.prefab");
            Object.DestroyImmediate(root);
            return ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).GetComponent<DeliveryImpactFeedback>();
        }

        private static ParticleSystem Particles(Transform parent, string name, Material material, int order, bool dust)
        {
            var obj = new GameObject(name) { layer = 30 };
            obj.transform.SetParent(parent, false);
            var system = obj.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = .5f;
            main.startLifetime = .3f;
            main.startSpeed = 0;
            main.startSize = (dust ? 9f : 6f) / 32;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var size = system.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, dust ? .5f : .1f));
            var color = system.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = order;
            renderer.maxParticleSize = .2f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }
    }
}
