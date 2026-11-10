using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.Tutorial
{
    // View rendering the tutorial spotlight dimmer, non-overlapping tooltip banner, hand pointer, and skip button
    public class CookingTutorialOverlayView : MonoBehaviour
    {
        [Header("Core Layers")]
        public TutorialSpotlightMask spotlightMask;
        public RectTransform overlayRect;

        [Header("Tooltip Card (Sub-Canvas Recommended For Popups)")]
        public RectTransform tooltipCard;
        public TextMeshProUGUI stepBadgeText;
        public TextMeshProUGUI promptText;

        [Header("Hand Pointer")]
        public RectTransform handPointer;

        [Header("Controls")]
        public Button skipButton;
        public TextMeshProUGUI skipButtonText;
        public Button skipToDay2Button;
        public TextMeshProUGUI skipToDay2ButtonText;

        public event Action OnSkipClicked;
        public event Action OnSkipToDay2Clicked;

        private Tween _handTween;
        private Tween _cardTween;
        private RectTransform _activeHandTarget;
        private readonly Vector3[] _targetCorners = new Vector3[4];

        private void Awake()
        {
            if (overlayRect == null)
            {
                overlayRect = (RectTransform)transform;
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(() => OnSkipClicked?.Invoke());
            }

            if (skipToDay2Button != null)
            {
                skipToDay2Button.onClick.AddListener(() => OnSkipToDay2Clicked?.Invoke());
            }

            HideAll();
            SetSkipToDay2Visible(false);
        }

        private void OnDestroy()
        {
            _handTween?.Kill();
            _cardTween?.Kill();
        }

        public void ShowStep(
            string badge,
            string prompt,
            IReadOnlyList<RectTransform> targets,
            bool blockOutsideInputs,
            bool showSpotlight,
            bool showHandPointer,
            bool showSkipButton,
            TooltipPlacement placement)
        {
            gameObject.SetActive(true);

            if (spotlightMask != null)
            {
                spotlightMask.ShowTargets(targets, blockOutsideInputs, showSpotlight);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(showSkipButton);
            }

            if (stepBadgeText != null)
            {
                bool hasBadge = !string.IsNullOrEmpty(badge);
                stepBadgeText.gameObject.SetActive(hasBadge);
                if (hasBadge) stepBadgeText.text = badge;
            }

            if (promptText != null)
            {
                promptText.text = prompt;
            }

            _activeHandTarget = null;
            PositionTooltipCard(targets, placement);
            UpdateHandPointer(targets, showHandPointer);

            if (tooltipCard != null)
            {
                tooltipCard.gameObject.SetActive(true);
                _cardTween?.Kill();
                tooltipCard.localScale = Vector3.one * 0.94f;
                _cardTween = tooltipCard.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        public void RefreshTargets(IReadOnlyList<RectTransform> targets, bool blockOutsideInputs, bool showSpotlight, bool showHandPointer)
        {
            if (spotlightMask != null)
            {
                spotlightMask.ShowTargets(targets, blockOutsideInputs, showSpotlight);
            }

            UpdateHandPointer(targets, showHandPointer);
        }

        public void SetSkipButtonVisible(bool visible)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(visible);
            }
        }

        public void SetSkipToDay2Visible(bool visible)
        {
            if (skipToDay2Button != null)
            {
                skipToDay2Button.gameObject.SetActive(visible);
            }
        }

        public void HideAll()
        {
            _activeHandTarget = null;
            _handTween?.Kill();
            _cardTween?.Kill();

            if (spotlightMask != null) spotlightMask.Hide();
            if (tooltipCard != null) tooltipCard.gameObject.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);
        }

        private void PositionTooltipCard(IReadOnlyList<RectTransform> targets, TooltipPlacement placement)
        {
            if (tooltipCard == null || overlayRect == null) return;

            TooltipPlacement resolved = placement;
            if (resolved == TooltipPlacement.Auto)
            {
                if (TryGetAverageLocalY(targets, out float avgY))
                {
                    Rect bounds = overlayRect.rect;
                    float normY = Mathf.InverseLerp(bounds.yMin, bounds.yMax, avgY);
                    // Targets in the bottom tray sit below y = 0.22 -> float card in middle station band;
                    // targets in stations, boards, tickets, or ship overlay -> float card cleanly over bottom dimmed tray.
                    resolved = normY < 0.22f ? TooltipPlacement.Middle : TooltipPlacement.Bottom;
                }
                else
                {
                    resolved = TooltipPlacement.Middle;
                }
            }

            (float targetAnchorY, float halfHeight) = resolved switch
            {
                TooltipPlacement.Top => (0.78f, 0.058f),
                TooltipPlacement.Middle => (0.56f, 0.058f),
                _ => (0.105f, 0.060f)
            };

            tooltipCard.anchorMin = new Vector2(0.05f, targetAnchorY - halfHeight);
            tooltipCard.anchorMax = new Vector2(0.95f, targetAnchorY + halfHeight);
            tooltipCard.offsetMin = Vector2.zero;
            tooltipCard.offsetMax = Vector2.zero;
        }

        private void UpdateHandPointer(IReadOnlyList<RectTransform> targets, bool show)
        {
            if (handPointer == null) return;

            if (!show || targets == null || targets.Count == 0 || overlayRect == null)
            {
                _activeHandTarget = null;
                _handTween?.Kill();
                handPointer.gameObject.SetActive(false);
                return;
            }

            RectTransform primary = null;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
                {
                    primary = targets[i];
                    break;
                }
            }

            if (primary == null)
            {
                _activeHandTarget = null;
                _handTween?.Kill();
                handPointer.gameObject.SetActive(false);
                return;
            }

            if (primary == _activeHandTarget && handPointer.gameObject.activeSelf)
            {
                return;
            }

            _activeHandTarget = primary;
            _handTween?.Kill();
            handPointer.gameObject.SetActive(true);
            primary.GetWorldCorners(_targetCorners);
            Vector3 worldCenter = (_targetCorners[0] + _targetCorners[2]) * 0.5f;
            Vector3 localPos = overlayRect.InverseTransformPoint(worldCenter);

            Rect bounds = overlayRect.rect;
            float clampedX = Mathf.Clamp(localPos.x, bounds.xMin + 44f, bounds.xMax - 44f);
            float clampedY = Mathf.Clamp(localPos.y - 36f, bounds.yMin + 42f, bounds.yMax - 56f);
            handPointer.anchoredPosition = new Vector2(clampedX, clampedY);

            _handTween = handPointer
                .DOAnchorPos(handPointer.anchoredPosition + new Vector2(0f, 16f), 0.45f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private bool TryGetAverageLocalY(IReadOnlyList<RectTransform> targets, out float averageY)
        {
            averageY = 0f;
            if (targets == null || targets.Count == 0 || overlayRect == null) return false;

            int count = 0;
            float sum = 0f;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
                {
                    targets[i].GetWorldCorners(_targetCorners);
                    Vector3 worldCenter = (_targetCorners[0] + _targetCorners[2]) * 0.5f;
                    Vector3 localPos = overlayRect.InverseTransformPoint(worldCenter);
                    sum += localPos.y;
                    count++;
                }
            }

            if (count == 0) return false;
            averageY = sum / count;
            return true;
        }
    }
}
