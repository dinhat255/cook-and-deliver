using UnityEngine;

namespace CookAndDeliver.Delivery
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class DeliverySafeArea : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect previousSafeArea = Rect.zero;
        private Vector2Int previousSize = Vector2Int.zero;
        private ScreenOrientation previousOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (previousSafeArea != Screen.safeArea ||
                previousSize.x != Screen.width ||
                previousSize.y != Screen.height ||
                previousOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null) return;
            }

            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            previousSafeArea = safe;
            previousSize = new Vector2Int(Screen.width, Screen.height);
            previousOrientation = Screen.orientation;

            Vector2 anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            Vector2 anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
