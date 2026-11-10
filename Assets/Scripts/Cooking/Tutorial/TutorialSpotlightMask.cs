using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.Tutorial
{
    // Blocks raycasts outside target RectTransforms and positions a 4-curtain spotlight dimmer around them
    public class TutorialSpotlightMask : MonoBehaviour, ICanvasRaycastFilter
    {
        [Header("Curtain Dimmer Panels")]
        public RectTransform topCurtain;
        public RectTransform bottomCurtain;
        public RectTransform leftCurtain;
        public RectTransform rightCurtain;

        [Header("Optional Highlight Frame")]
        public RectTransform highlightFrame;

        [Header("Styling")]
        public float spotlightPadding = 12f;

        private readonly List<RectTransform> _allowedTargets = new List<RectTransform>();
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private RectTransform _maskRect;
        private bool _blockOutsideInputs;
        private bool _showSpotlight;
        private bool _isShown;

        private void Awake()
        {
            _maskRect = (RectTransform)transform;
            var filterImage = GetComponent<Image>();
            if (filterImage != null)
            {
                filterImage.raycastTarget = true;
            }

            if (!_isShown)
            {
                Hide();
            }
        }

        public void ShowTargets(IReadOnlyList<RectTransform> targets, bool blockOutsideInputs, bool showSpotlight)
        {
            if (_maskRect == null) _maskRect = (RectTransform)transform;

            _allowedTargets.Clear();
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null)
                    {
                        _allowedTargets.Add(targets[i]);
                    }
                }
            }

            _blockOutsideInputs = blockOutsideInputs;
            _showSpotlight = showSpotlight;
            _isShown = true;

            gameObject.SetActive(true);
            RefreshLayout();
        }

        public void Hide()
        {
            _isShown = false;
            _allowedTargets.Clear();
            _blockOutsideInputs = false;
            _showSpotlight = false;
            SetCurtainsActive(false);
            if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_showSpotlight && _allowedTargets.Count > 0)
            {
                RefreshLayout();
            }
        }

        public void RefreshLayout()
        {
            if (_maskRect == null) _maskRect = (RectTransform)transform;

            if (!_showSpotlight)
            {
                SetCurtainsActive(false);
                if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
                return;
            }

            if (!TryComputeTargetLocalBounds(out float minX, out float minY, out float maxX, out float maxY))
            {
                // Full screen dim when no valid target rect exists
                SetFullScreenCurtain();
                if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
                return;
            }

            Rect container = _maskRect.rect;
            if (container.width <= 1f || container.height <= 1f) return;

            minX = Mathf.Clamp(minX - spotlightPadding, container.xMin, container.xMax);
            maxX = Mathf.Clamp(maxX + spotlightPadding, container.xMin, container.xMax);
            minY = Mathf.Clamp(minY - spotlightPadding, container.yMin, container.yMax);
            maxY = Mathf.Clamp(maxY + spotlightPadding, container.yMin, container.yMax);

            float uMin = Mathf.InverseLerp(container.xMin, container.xMax, minX);
            float uMax = Mathf.InverseLerp(container.xMin, container.xMax, maxX);
            float vMin = Mathf.InverseLerp(container.yMin, container.yMax, minY);
            float vMax = Mathf.InverseLerp(container.yMin, container.yMax, maxY);

            SetCurtainsActive(true);
            SetAnchorBox(topCurtain, 0f, vMax, 1f, 1f);
            SetAnchorBox(bottomCurtain, 0f, 0f, 1f, vMin);
            SetAnchorBox(leftCurtain, 0f, vMin, uMin, vMax);
            SetAnchorBox(rightCurtain, uMax, vMin, 1f, vMax);

            if (highlightFrame != null)
            {
                highlightFrame.gameObject.SetActive(true);
                SetAnchorBox(highlightFrame, uMin, vMin, uMax, vMax);
            }
        }

        public bool TryComputeTargetLocalBounds(out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;
            bool hasAny = false;

            if (_maskRect == null) _maskRect = (RectTransform)transform;

            for (int i = 0; i < _allowedTargets.Count; i++)
            {
                RectTransform target = _allowedTargets[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;

                target.GetWorldCorners(_worldCorners);
                for (int c = 0; c < 4; c++)
                {
                    Vector3 localPoint = _maskRect.InverseTransformPoint(_worldCorners[c]);
                    if (localPoint.x < minX) minX = localPoint.x;
                    if (localPoint.y < minY) minY = localPoint.y;
                    if (localPoint.x > maxX) maxX = localPoint.x;
                    if (localPoint.y > maxY) maxY = localPoint.y;
                    hasAny = true;
                }
            }

            return hasAny;
        }

        private void SetFullScreenCurtain()
        {
            SetCurtainsActive(true);
            SetAnchorBox(topCurtain, 0f, 0f, 1f, 1f);
            SetAnchorBox(bottomCurtain, 0f, 0f, 0f, 0f);
            SetAnchorBox(leftCurtain, 0f, 0f, 0f, 0f);
            SetAnchorBox(rightCurtain, 0f, 0f, 0f, 0f);
        }

        private void SetCurtainsActive(bool active)
        {
            if (topCurtain != null) topCurtain.gameObject.SetActive(active);
            if (bottomCurtain != null) bottomCurtain.gameObject.SetActive(active);
            if (leftCurtain != null) leftCurtain.gameObject.SetActive(active);
            if (rightCurtain != null) rightCurtain.gameObject.SetActive(active);
        }

        private static void SetAnchorBox(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_blockOutsideInputs) return false;

            for (int i = 0; i < _allowedTargets.Count; i++)
            {
                RectTransform target = _allowedTargets[i];
                if (target != null && target.gameObject.activeInHierarchy)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
