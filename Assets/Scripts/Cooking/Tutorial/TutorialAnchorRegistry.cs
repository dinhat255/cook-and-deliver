using System.Collections.Generic;
using UnityEngine;

namespace CookAndDeliver.Cooking.Tutorial
{
    // Global registry mapping semantic anchor IDs to live scene RectTransforms
    public static class TutorialAnchorRegistry
    {
        private static readonly Dictionary<string, RectTransform> Anchors = new Dictionary<string, RectTransform>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Anchors.Clear();
        }

        public static void Register(string anchorId, RectTransform rectTransform)
        {
            if (string.IsNullOrEmpty(anchorId) || rectTransform == null) return;
            Anchors[anchorId] = rectTransform;
        }

        public static void Unregister(string anchorId, RectTransform rectTransform)
        {
            if (string.IsNullOrEmpty(anchorId)) return;
            if (Anchors.TryGetValue(anchorId, out var existing) && existing == rectTransform)
            {
                Anchors.Remove(anchorId);
            }
        }

        public static bool TryGet(string anchorId, out RectTransform rectTransform)
        {
            if (string.IsNullOrEmpty(anchorId))
            {
                rectTransform = null;
                return false;
            }
            return Anchors.TryGetValue(anchorId, out rectTransform) && rectTransform != null;
        }
    }
}
