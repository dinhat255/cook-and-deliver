using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    /// <summary>
    /// Dynamically adjusts CanvasScaler match mode based on device aspect ratio.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public class DynamicCanvasScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 targetResolution = new Vector2(1080f, 1920f);
        private CanvasScaler _scaler;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
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
            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
                if (_scaler == null) return;
            }

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = targetResolution;

            float targetAspect = targetResolution.x / targetResolution.y; // 1080 / 1920 = 0.5625
            if (Screen.height <= 0) return;
            float currentAspect = (float)Screen.width / Screen.height;

            // If screen is taller than 9:16 (phones 19.5:9, 20:9), match width = 0
            // If screen is wider/squarer than 9:16 (tablets, iPad 4:3), match height = 1
            float desiredMatch = currentAspect < targetAspect ? 0f : 1f;
            if (!Mathf.Approximately(_scaler.matchWidthOrHeight, desiredMatch))
            {
                _scaler.matchWidthOrHeight = desiredMatch;
            }
        }
    }
}
