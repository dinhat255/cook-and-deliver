using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Delivery
{
    /// <summary>
    /// Dynamically adjusts CanvasScaler match mode based on device aspect ratio (matches Cooking UI).
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class DeliveryCanvasScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 targetResolution = new Vector2(1080f, 1920f);
        private CanvasScaler scaler;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            UpdateScaler();
        }

        private void OnEnable()
        {
            UpdateScaler();
        }

        private void Update()
        {
            UpdateScaler();
        }

        public void UpdateScaler()
        {
            if (scaler == null)
            {
                scaler = GetComponent<CanvasScaler>();
                if (scaler == null) return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = targetResolution;

            if (Screen.height <= 0 || targetResolution.y <= 0f) return;

            float targetAspect = targetResolution.x / targetResolution.y;
            float currentAspect = (float)Screen.width / Screen.height;
            float desiredMatch = currentAspect < targetAspect ? 0f : 1f;

            if (!Mathf.Approximately(scaler.matchWidthOrHeight, desiredMatch))
            {
                scaler.matchWidthOrHeight = desiredMatch;
            }
        }
    }
}
