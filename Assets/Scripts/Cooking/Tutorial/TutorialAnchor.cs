using UnityEngine;

namespace CookAndDeliver.Cooking.Tutorial
{
    // Passive marker component exposing a UI RectTransform by semantic ID for tutorial targeting
    [RequireComponent(typeof(RectTransform))]
    public class TutorialAnchor : MonoBehaviour
    {
        public string anchorId;

        private RectTransform _rectTransform;

        private void OnEnable()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }
            TutorialAnchorRegistry.Register(anchorId, _rectTransform);
        }

        private void OnDisable()
        {
            TutorialAnchorRegistry.Unregister(anchorId, _rectTransform);
        }
    }
}
