using System;
using CookAndDeliver.Delivery.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Delivery
{
    /// <summary>
    /// Interactive Day 1 onboarding overlay for Delivery without modifying core simulation rules.
    /// Guides the first obstacle dodge and first highlighted delivery stop.
    /// </summary>
    public sealed class DeliveryTutorialController : MonoBehaviour
    {
        private enum WaitMode
        {
            None = 0,
            HazardSwipe = 1,
            StopSwipe = 2,
            StopConfirm = 3
        }

        [Header("Scene References")]
        [SerializeField] private DeliveryController controller;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private GameObject swipePromptGroup;
        [SerializeField] private Image arrowImage;
        [SerializeField] private Sprite arrowLeftSprite;
        [SerializeField] private Sprite arrowRightSprite;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI continueLabel;
        [SerializeField] private Button skipButton;

        [Header("Trigger Tuning")]
        [SerializeField] private float hazardTriggerMinY = DeliverySettings.PlayerY + 42f;
        [SerializeField] private float hazardTriggerMaxY = 225f;
        [SerializeField] private float stopTriggerMaxDistance = 175f;
        [SerializeField] private float stopTriggerMinDistance = 85f;

        private WaitMode waitMode = WaitMode.None;
        private int expectedDirection;
        private bool step1Done;
        private bool step2Done;
        private bool tutorialFinished;
        private float animClock;
        private Vector2 arrowBasePos;
        private DeliverySimulation boundSim;
        private bool isSubscribed;

        private const string DevSkipPrefKey = "CookAndDeliver.Dev.ShowTutorialSkipControls";

        public static bool IsDevSkipEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(DevSkipPrefKey, false);
#else
            return false;
#endif
        }

        public void RefreshDevSkipVisibility()
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(!tutorialFinished && IsDevSkipEnabled());
            }
        }

        private void Awake()
        {
            if (arrowImage != null)
            {
                arrowBasePos = arrowImage.rectTransform.anchoredPosition;
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(CompleteTutorial);
            }

            RefreshDevSkipVisibility();

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void Start()
        {
            BindEvents();
            RefreshCompletionFlag();
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(CompleteTutorial);
            }

            UnbindEvents();
        }

        private void BindEvents()
        {
            if (isSubscribed || controller == null) return;
            boundSim = controller.Simulation;
            if (boundSim != null)
            {
                boundSim.StateChanged += OnSimulationStateChanged;
                boundSim.OrderResolved += OnOrderResolved;
                boundSim.RunCompleted += OnRunCompleted;
            }
            if (controller.input != null)
            {
                controller.input.Swiped += OnSwiped;
            }
            isSubscribed = true;
        }

        private void UnbindEvents()
        {
            if (!isSubscribed) return;
            if (boundSim != null)
            {
                boundSim.StateChanged -= OnSimulationStateChanged;
                boundSim.OrderResolved -= OnOrderResolved;
                boundSim.RunCompleted -= OnRunCompleted;
            }
            if (controller != null && controller.input != null)
            {
                controller.input.Swiped -= OnSwiped;
            }
            boundSim = null;
            isSubscribed = false;
        }

        private void RefreshCompletionFlag()
        {
            tutorialFinished = IsTutorialAlreadyCompleted();
            RefreshDevSkipVisibility();
            if (tutorialFinished)
            {
                step1Done = true;
                step2Done = true;
                HideOverlay(restorePauseButton: false);
            }
        }

        private bool IsTutorialAlreadyCompleted()
        {
            if (DeliveryController.ExternalIsTutorialCompleted != null)
            {
                return DeliveryController.ExternalIsTutorialCompleted.Invoke();
            }
            return controller != null && controller.Progress != null && controller.Progress.Data.tutorialCompleted;
        }

        private void OnSimulationStateChanged()
        {
            if (boundSim == null) return;

            if (boundSim.State == DeliveryState.Countdown || boundSim.State == DeliveryState.Ready)
            {
                RefreshCompletionFlag();
                if (!tutorialFinished)
                {
                    step1Done = false;
                    step2Done = false;
                    waitMode = WaitMode.None;
                    HideOverlay(restorePauseButton: false);
                }
                return;
            }

            if (boundSim.State != DeliveryState.Running && waitMode != WaitMode.None)
            {
                waitMode = WaitMode.None;
                HideOverlay(restorePauseButton: false);
            }
        }

        private void OnOrderResolved(OrderResult result)
        {
            if (tutorialFinished) return;
            if (boundSim != null && boundSim.CurrentOrderIndex >= 1 && (step1Done || step2Done))
            {
                CompleteTutorial();
            }
        }

        private void OnRunCompleted(DeliveryRunResult result)
        {
            if (!tutorialFinished)
            {
                CompleteTutorial();
            }
        }

        private void Update()
        {
            if (tutorialFinished || controller == null || boundSim == null) return;

            if (waitMode != WaitMode.None)
            {
                AnimatePrompt();
                SuppressDefaultHint();
                return;
            }

            if (boundSim.State != DeliveryState.Running || controller.IsSimulationFrozen) return;

            if (!step1Done && TryTriggerHazardStep()) return;
            if (!step2Done) TryTriggerDeliveryStopStep();
        }

        private void LateUpdate()
        {
            if (waitMode != WaitMode.None)
            {
                SuppressDefaultHint();
            }
        }

        private void SuppressDefaultHint()
        {
            if (controller != null && controller.ui != null && controller.ui.hint != null)
            {
                if (controller.ui.hint.gameObject.activeSelf)
                {
                    controller.ui.hint.gameObject.SetActive(false);
                }
            }
        }

        private bool TryTriggerHazardStep()
        {
            if (boundSim.IsChangingLane) return false;

            DeliveryHazard nearest = null;
            float minY = float.MaxValue;
            var hazards = boundSim.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                if (h.Hit || h.Lane != boundSim.Lane) continue;
                if (h.Y < hazardTriggerMinY || h.Y > hazardTriggerMaxY) continue;
                if (h.Y < minY)
                {
                    minY = h.Y;
                    nearest = h;
                }
            }

            if (nearest == null) return false;
            int safeDir = FindSafeAdjacentDirection(boundSim, nearest);
            if (safeDir == 0) return false;

            step1Done = true;
            expectedDirection = safeDir;
            waitMode = WaitMode.HazardSwipe;
            controller.SetSimulationFrozen(true);

            string dirWord = safeDir < 0 ? "TRÁI" : "PHẢI";
            ShowSwipePrompt(
                "HƯỚNG DẪN LÁI XE (1/2)",
                $"Chướng ngại vật phía trước! Hãy vuốt sang {dirWord} trên mặt đường để né tránh.",
                safeDir);
            return true;
        }

        private void TryTriggerDeliveryStopStep()
        {
            if (boundSim.CurrentOrderIndex != 0 || boundSim.Request == null) return;
            float dist = boundSim.DistanceToNextStop;
            if (dist > stopTriggerMaxDistance || dist < stopTriggerMinDistance) return;

            step2Done = true;
            controller.SetSimulationFrozen(true);

            int targetLane = boundSim.NextStopLane;
            if (boundSim.Lane == targetLane)
            {
                expectedDirection = 0;
                waitMode = WaitMode.StopConfirm;
                ShowConfirmPrompt(
                    "HƯỚNG DẪN GIAO HÀNG (2/2)",
                    "Vạch vàng là điểm giao đơn đầu tiên! Bạn đã ở đúng làn, bấm Tiếp tục để hoàn tất giao hàng.");
            }
            else
            {
                int dir = targetLane < boundSim.Lane ? -1 : 1;
                expectedDirection = dir;
                waitMode = WaitMode.StopSwipe;
                string dirWord = dir < 0 ? "TRÁI" : "PHẢI";
                string extra = Mathf.Abs(targetLane - boundSim.Lane) > 1 ? " (cần 2 lần để vào sát làn vàng)" : "";
                ShowSwipePrompt(
                    "HƯỚNG DẪN GIAO HÀNG (2/2)",
                    $"Sắp tới điểm giao đơn! Làn tô vàng bên {dirWord} là nơi khách đợi, hãy vuốt sang {dirWord}{extra}.",
                    dir);
            }
        }

        private int FindSafeAdjacentDirection(DeliverySimulation sim, DeliveryHazard incoming)
        {
            // Try preferred directions: inward toward lane 1 first when on edges, or left (-1) then right (+1).
            int first = sim.Lane == 2 ? -1 : sim.Lane == 0 ? 1 : -1;
            int second = -first;

            if (IsAdjacentLaneClear(sim, sim.Lane + first, incoming)) return first;
            if (IsAdjacentLaneClear(sim, sim.Lane + second, incoming)) return second;
            return 0;
        }

        private static bool IsAdjacentLaneClear(DeliverySimulation sim, int candidateLane, DeliveryHazard incoming)
        {
            if (candidateLane < 0 || candidateLane > 2) return false;
            var hazards = sim.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                var other = hazards[i];
                if (other.Hit || other.Lane != candidateLane) continue;
                float minSafeY = DeliverySettings.PlayerY - 36f;
                float maxSafeY = incoming.Y + other.HalfOccupancyDistance(16f);
                if (other.Y >= minSafeY && other.Y <= maxSafeY)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnSwiped(int direction)
        {
            if (tutorialFinished) return;
            if (waitMode != WaitMode.HazardSwipe && waitMode != WaitMode.StopSwipe) return;

            if (direction != expectedDirection)
            {
                if (bodyText != null)
                {
                    string expectedWord = expectedDirection < 0 ? "TRÁI" : "PHẢI";
                    bodyText.text = $"Hãy vuốt theo hướng mũi tên sang {expectedWord}!";
                }
                return;
            }

            int moveDir = expectedDirection;
            waitMode = WaitMode.None;
            HideOverlay(restorePauseButton: true);
            controller.SetSimulationFrozen(false);
            controller.RequestLaneChange(moveDir);
        }

        private void OnContinueClicked()
        {
            if (waitMode != WaitMode.StopConfirm) return;
            waitMode = WaitMode.None;
            HideOverlay(restorePauseButton: true);
            controller.SetSimulationFrozen(false);
        }

        private void CompleteTutorial()
        {
            if (tutorialFinished) return;
            tutorialFinished = true;
            step1Done = true;
            step2Done = true;
            waitMode = WaitMode.None;
            RefreshDevSkipVisibility();
            HideOverlay(restorePauseButton: true);
            if (controller != null)
            {
                controller.SetSimulationFrozen(false);
                controller.Progress?.MarkTutorialCompleted();
            }
            DeliveryController.ExternalOnTutorialCompleted?.Invoke();
        }

        private void ShowSwipePrompt(string title, string body, int dir)
        {
            if (controller != null && controller.ui != null && controller.ui.pauseButton != null)
            {
                controller.ui.pauseButton.interactable = false;
            }
            SuppressDefaultHint();

            if (overlayRoot != null) overlayRoot.SetActive(true);
            RefreshDevSkipVisibility();
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (swipePromptGroup != null) swipePromptGroup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            if (arrowImage != null)
            {
                arrowImage.sprite = dir < 0 ? arrowLeftSprite : arrowRightSprite;
                arrowImage.rectTransform.anchoredPosition = arrowBasePos;
            }
            animClock = 0f;
        }

        private void ShowConfirmPrompt(string title, string body)
        {
            if (controller != null && controller.ui != null && controller.ui.pauseButton != null)
            {
                controller.ui.pauseButton.interactable = false;
            }
            SuppressDefaultHint();

            if (overlayRoot != null) overlayRoot.SetActive(true);
            RefreshDevSkipVisibility();
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (swipePromptGroup != null) swipePromptGroup.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (continueLabel != null) continueLabel.text = "Tiếp tục";
        }

        private void HideOverlay(bool restorePauseButton)
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);
            if (restorePauseButton && controller != null && controller.ui != null && controller.ui.pauseButton != null && boundSim != null)
            {
                controller.ui.pauseButton.interactable = boundSim.State == DeliveryState.Running || boundSim.State == DeliveryState.Countdown;
            }
        }

        private void AnimatePrompt()
        {
            animClock += Time.unscaledDeltaTime;
            if (waitMode == WaitMode.HazardSwipe || waitMode == WaitMode.StopSwipe)
            {
                if (arrowImage != null)
                {
                    float cycle = Mathf.PingPong(animClock * 2.2f, 1f);
                    float smooth = cycle * cycle * (3f - 2f * cycle);
                    float offset = expectedDirection * (smooth - 0.3f) * 56f;
                    arrowImage.rectTransform.anchoredPosition = arrowBasePos + new Vector2(offset, 0f);
                }
            }
            if (cardRect != null)
            {
                float scale = 1f + Mathf.Sin(animClock * 4f) * 0.012f;
                cardRect.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
