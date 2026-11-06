using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IVfxService managing UI tween animations and pooled particles
    public class VfxService : MonoBehaviour, IVfxService
    {
        [Header("Target Layer")]
        public RectTransform flyLayer;

        void Awake()
        {
            CookingServiceLocator.Register<IVfxService>(this);
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IVfxService>();
        }

        public void FlyArc(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld, float duration, Action onArrive)
        {
            var pool = CookingServiceLocator.Get<IPoolService>();
            if (flyLayer == null || pool == null)
            {
                onArrive?.Invoke();
                return;
            }

            var go = pool.SpawnFlyArc(flyLayer);
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.color = tint;
                image.raycastTarget = false;
            }

            var rt = (RectTransform)go.transform;
            Vector2 a = flyLayer.InverseTransformPoint(fromWorld);
            Vector2 b = flyLayer.InverseTransformPoint(toWorld);

            DOVirtual.Float(0f, 1f, duration, t =>
            {
                if (image == null) return;
                var p = Vector2.Lerp(a, b, t);
                float arc = Mathf.Sin(t * Mathf.PI);
                p.y += arc * 140f;
                rt.anchoredPosition = p;
                rt.localScale = Vector3.one * (1f + 0.25f * arc);
            }).SetEase(Ease.Linear).SetLink(go).OnComplete(() =>
            {
                pool.ReturnFlyArc(go);
                onArrive?.Invoke();
            });
        }

        public void FlyBounce(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld, float duration)
        {
            var mid = Vector3.Lerp(fromWorld, toWorld, 0.55f);
            FlyArc(sprite, tint, fromWorld, mid, duration * 0.8f, () =>
            {
                FlyArc(sprite, tint, mid, fromWorld, duration * 0.8f, null);
            });
        }

        public void ShowToast(string message, Vector3 worldPos, Color color)
        {
            var pool = CookingServiceLocator.Get<IPoolService>();
            if (flyLayer == null || pool == null) return;

            var go = pool.SpawnToast(flyLayer);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
                tmp.color = color;
            }

            var rt = (RectTransform)go.transform;
            Vector2 p = flyLayer.InverseTransformPoint(worldPos);
            float limit = flyLayer.rect.width * 0.5f - rt.sizeDelta.x * 0.5f;
            p.x = Mathf.Clamp(p.x, -limit, limit);
            rt.anchoredPosition = p;

            DOVirtual.Float(0f, 1f, 0.9f, v =>
            {
                if (tmp == null) return;
                rt.anchoredPosition = p + Vector2.up * (110f * v);
                var c = tmp.color;
                c.a = 1f - v * v;
                tmp.color = c;
            }).SetLink(go).OnComplete(() =>
            {
                pool.ReturnToast(go);
            });
        }

        public void EmitSparkle(Vector3 worldPos, Color color, int count, float distance)
        {
            var pool = CookingServiceLocator.Get<IPoolService>();
            if (flyLayer == null || pool == null) return;

            Vector2 p = flyLayer.InverseTransformPoint(worldPos);
            for (int i = 0; i < count; i++)
            {
                var go = pool.SpawnSpark(flyLayer);
                var im = go.GetComponent<Image>();
                if (im != null)
                {
                    im.color = color;
                    im.raycastTarget = false;
                }

                float ang = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

                DOVirtual.Float(0f, 1f, 0.45f, v =>
                {
                    if (im == null) return;
                    ((RectTransform)im.transform).anchoredPosition = p + dir * (distance * v);
                    var c = color;
                    c.a = color.a * (1f - v);
                    im.color = c;
                }).SetLink(go).OnComplete(() =>
                {
                    pool.ReturnSpark(go);
                });
            }
        }

        public void EmitPuff(Vector3 worldPos, Color color)
        {
            var pool = CookingServiceLocator.Get<IPoolService>();
            if (flyLayer == null || pool == null) return;

            var go = pool.SpawnPuff(flyLayer);
            var im = go.GetComponent<Image>();
            if (im != null)
            {
                im.color = color;
                im.raycastTarget = false;
            }

            Vector2 p = flyLayer.InverseTransformPoint(worldPos);
            DOVirtual.Float(0f, 1f, 0.9f, v =>
            {
                if (im == null) return;
                ((RectTransform)im.transform).anchoredPosition = p + Vector2.up * (100f * v);
                im.transform.localScale = Vector3.one * (1f + 1.4f * v);
                var c = color;
                c.a = color.a * (1f - v);
                im.color = c;
            }).SetLink(go).OnComplete(() =>
            {
                pool.ReturnPuff(go);
            });
        }
    }
}
