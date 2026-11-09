using UnityEngine;

namespace CookAndDeliver.Delivery
{
    [ExecuteAlways]
    public sealed class DeliverySafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int size;
        private void Update()
        {
            var safe = Screen.safeArea;
            if (previous == safe && size.x == Screen.width && size.y == Screen.height) return;
            previous = safe;
            size = new Vector2Int(Screen.width, Screen.height);
            if (size.x == 0 || size.y == 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
