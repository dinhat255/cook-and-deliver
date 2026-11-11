using System.Collections.Generic;
using System.IO;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.Presenters;
using CookAndDeliver.Cooking.UI;
using CookAndDeliver.Delivery;
using UnityEngine;

namespace CookAndDeliver.Cooking.Tutorial
{
    // Decoupled Day 1 Cooking tutorial director driving scripted intro steps via event channels and neutral presenter seams
    public class CookingTutorialDirector : MonoBehaviour
    {
        [Header("Tutorial Configuration")]
        public TutorialSequenceSO sequence;

        [Header("Scene Presenters & Views")]
        public CookingMasterPresenter masterPresenter;
        public OrderPresenter orderPresenter;
        public HeatStationPresenter heatStationPresenter;
        public WorkstationPresenter workstationPresenter;
        public ShipOverlayView shipOverlay;
        public CookingTutorialOverlayView overlayView;

        [Header("Event Channels")]
        public OrderEventChannelSO orderEventChannel;
        public StationEventChannelSO stationEventChannel;
        public WorkstationEventChannelSO workstationEventChannel;

        private const string DevSkipPrefKey = "CookAndDeliver.Dev.ShowTutorialSkipControls";

        private readonly struct SlotTargetCandidate
        {
            public readonly RectTransform Rect;
            public readonly int Priority;
            public readonly int Order;

            public SlotTargetCandidate(RectTransform rect, int priority, int order)
            {
                Rect = rect;
                Priority = priority;
                Order = order;
            }
        }

        private readonly List<RectTransform> _rawTargets = new List<RectTransform>();
        private readonly List<RectTransform> _resolvedTargets = new List<RectTransform>();
        private readonly List<SlotTargetCandidate> _slotCandidates = new List<SlotTargetCandidate>();
        private int _currentStepIndex = -1;
        private bool _isGuidedActive;
        private bool _isSoftHintMode;
        private float _softHintRemaining;
        private string _activeSoftHintType;
        private bool _panHintShown;
        private bool _burntHintShown;
        private bool _shipHintShown;

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
            if (overlayView == null) return;
            bool devSkipEnabled = IsDevSkipEnabled();
            overlayView.SetSkipButtonVisible(_isGuidedActive && devSkipEnabled);
            overlayView.SetSkipToDay2Visible(CookingSession.CurrentDay <= 1 && devSkipEnabled);
        }

        private void Awake()
        {
            if (masterPresenter != null)
            {
                masterPresenter.OnSessionReady += HandleSessionReady;
                masterPresenter.OnTrashModeChanged += HandleTrashModeChanged;
            }

            if (overlayView != null)
            {
                overlayView.OnSkipClicked += HandleSkipClicked;
                overlayView.OnSkipToDay2Clicked += SkipTutorialToDay2;
            }

            if (shipOverlay != null)
            {
                shipOverlay.OnVisibilityChanged += HandleShipOverlayVisibilityChanged;
            }
        }

        private void OnEnable()
        {
            if (orderEventChannel != null) orderEventChannel.OnOrderSpawned += HandleOrderSpawned;
            if (stationEventChannel != null)
            {
                stationEventChannel.OnSlotStateChanged += HandleSlotStateChanged;
                stationEventChannel.OnSlotBurnt += HandleSlotBurnt;
            }
            if (workstationEventChannel != null)
            {
                workstationEventChannel.OnIngredientAdded += HandleIngredientAdded;
                workstationEventChannel.OnDishCompleted += HandleDishCompleted;
                workstationEventChannel.OnDishServed += HandleDishServed;
                workstationEventChannel.OnBoardSelected += HandleBoardSelected;
            }
        }

        private void OnDisable()
        {
            if (orderEventChannel != null) orderEventChannel.OnOrderSpawned -= HandleOrderSpawned;
            if (stationEventChannel != null)
            {
                stationEventChannel.OnSlotStateChanged -= HandleSlotStateChanged;
                stationEventChannel.OnSlotBurnt -= HandleSlotBurnt;
            }
            if (workstationEventChannel != null)
            {
                workstationEventChannel.OnIngredientAdded -= HandleIngredientAdded;
                workstationEventChannel.OnDishCompleted -= HandleDishCompleted;
                workstationEventChannel.OnDishServed -= HandleDishServed;
                workstationEventChannel.OnBoardSelected -= HandleBoardSelected;
            }
        }

        private void OnDestroy()
        {
            if (masterPresenter != null)
            {
                masterPresenter.OnSessionReady -= HandleSessionReady;
                masterPresenter.OnTrashModeChanged -= HandleTrashModeChanged;
            }

            if (overlayView != null)
            {
                overlayView.OnSkipClicked -= HandleSkipClicked;
                overlayView.OnSkipToDay2Clicked -= SkipTutorialToDay2;
            }

            if (shipOverlay != null)
            {
                shipOverlay.OnVisibilityChanged -= HandleShipOverlayVisibilityChanged;
            }
        }

        private void Update()
        {
            if (_isSoftHintMode && _softHintRemaining > 0f)
            {
                _softHintRemaining -= Time.deltaTime;
                if (_softHintRemaining <= 0f)
                {
                    DismissSoftHint();
                }
            }
        }

        private void HandleSessionReady()
        {
            if (sequence == null || sequence.steps == null || sequence.steps.Count == 0)
            {
                overlayView?.SetSkipToDay2Visible(false);
                gameObject.SetActive(false);
                return;
            }

            bool isDay1 = CookingSession.CurrentDay <= 1;
            bool devSkipEnabled = IsDevSkipEnabled();
            overlayView?.SetSkipToDay2Visible(isDay1 && devSkipEnabled);

            if (GameProgressStore.IsCookingTutorialCompleted)
            {
                if (isDay1)
                {
                    _isSoftHintMode = true;
                    overlayView?.SetSkipButtonVisible(false);
                }
                else
                {
                    overlayView?.HideAll();
                    overlayView?.SetSkipToDay2Visible(false);
                    gameObject.SetActive(false);
                }
                return;
            }

            BeginGuidedTutorial();
        }

        private void BeginGuidedTutorial()
        {
            _isGuidedActive = true;
            _isSoftHintMode = false;

            if (orderPresenter != null)
            {
                orderPresenter.ClearAllOrders();
                orderPresenter.SetAutoSpawnEnabled(false);
                orderPresenter.SetPatienceDecayEnabled(false);
            }

            if (heatStationPresenter != null)
            {
                heatStationPresenter.SetBurnEnabled(false);
            }

            EnterStep(0);
        }

        private void EnterStep(int index)
        {
            if (sequence == null || index < 0 || index >= sequence.steps.Count)
            {
                CompleteGuidedTutorial(spawnMissingStarterOrders: true);
                return;
            }

            _currentStepIndex = index;
            TutorialStepData step = sequence.steps[index];

            if (step.spawnOrderBeforeStep && step.orderToSpawn != null && orderPresenter != null)
            {
                orderPresenter.SpawnSpecificOrder(step.orderToSpawn, step.orderIsShip, step.orderPreferredLane);
                if (workstationPresenter != null)
                {
                    workstationPresenter.AssignOrdersToBoards(orderPresenter.ActiveOrders);
                }
            }

            ResolveStepTargets(step, out bool hasPointerTarget);

            if (overlayView != null)
            {
                string badge = $"HƯỚNG DẪN {index + 1}/{sequence.steps.Count}";
                overlayView.ShowStep(
                    badge,
                    step.instructionText,
                    _resolvedTargets,
                    step.blockOutsideInputs,
                    step.showSpotlight,
                    step.showHandPointer && hasPointerTarget,
                    showSkipButton: IsDevSkipEnabled(),
                    step.tooltipPlacement);
            }

            // Re-evaluate immediately in case current scene state already satisfies the rule
            EvaluateCurrentStep();
        }

        private void CompleteGuidedTutorial(bool spawnMissingStarterOrders)
        {
            _isGuidedActive = false;
            _currentStepIndex = -1;
            _isSoftHintMode = true;

            GameProgressStore.MarkCookingTutorialCompleted();
            overlayView?.HideAll();
            overlayView?.SetSkipToDay2Visible(CookingSession.CurrentDay <= 1 && IsDevSkipEnabled());

            if (heatStationPresenter != null)
            {
                heatStationPresenter.SetBurnEnabled(true);
            }

            if (orderPresenter != null)
            {
                orderPresenter.SetPatienceDecayEnabled(true);
                orderPresenter.SetAutoSpawnEnabled(true);

                if (spawnMissingStarterOrders)
                {
                    if (sequence != null && sequence.postTutorialStarterRecipe != null && orderPresenter.ActiveOrders.Count > 0 && orderPresenter.ActiveOrders[0] == null)
                    {
                        orderPresenter.SpawnSpecificOrder(sequence.postTutorialStarterRecipe, false, 0);
                    }
                    else if (!orderPresenter.HasAnyOrders())
                    {
                        orderPresenter.InitializeOrders();
                    }
                }
            }
        }

        [ContextMenu("Skip Tutorial To Day 2")]
        public void SkipTutorialToDay2()
        {
            _isGuidedActive = false;
            _isSoftHintMode = false;
            _currentStepIndex = -1;
            _activeSoftHintType = null;
            _softHintRemaining = 0f;

            GameProgressStore.MarkCookingTutorialCompleted(false);
            GameProgressStore.MarkDeliveryTutorialCompleted(false);
            new DeliveryProgressStore(Path.Combine(Application.persistentDataPath, "Delivery")).MarkTutorialCompleted();

            overlayView?.HideAll();
            overlayView?.SetSkipToDay2Visible(false);

            if (masterPresenter != null)
            {
                masterPresenter.ResetForSkippedDay(2);
            }
            else
            {
                CookingSession.CurrentDay = 2;
                CookingSession.DayDineInCompleted = 0;
                CookingSession.DayDeliveryCompleted = 0;
                CookingSession.ClearDeliveryOrders();
                heatStationPresenter?.SetBurnEnabled(true);
                orderPresenter?.SetPatienceDecayEnabled(true);
                orderPresenter?.SetAutoSpawnEnabled(true);
                GameProgressStore.SyncFromSession(true);
            }

            gameObject.SetActive(false);
        }

        private void HandleSkipClicked()
        {
            if (!_isGuidedActive) return;
            CompleteGuidedTutorial(spawnMissingStarterOrders: true);
        }

        private void HandleSlotStateChanged(string slotId, IngredientSO ingredient, HeatSlotState state)
        {
            if (_isGuidedActive)
            {
                if (!EvaluateCurrentStep())
                {
                    RefreshCurrentStepTargets();
                }
                return;
            }

            if (_isSoftHintMode && _activeSoftHintType == "pan" && ingredient != null && ingredient.id == "rice_cake")
            {
                DismissSoftHint();
            }
        }

        private void HandleIngredientAdded(bool isDeliveryBoard, IngredientSO ingredient, int currentCount, int requiredCount)
        {
            if (_isGuidedActive)
            {
                if (!EvaluateCurrentStep())
                {
                    RefreshCurrentStepTargets();
                }
            }
        }

        private void HandleBoardSelected(int boardIndex)
        {
            if (_isGuidedActive)
            {
                EvaluateCurrentStep();
            }
        }

        private void HandleDishCompleted(bool isDeliveryBoard, RecipeSO recipe, Vector3 dishPos)
        {
            if (!_isGuidedActive) return;
            TutorialStepData step = GetCurrentStep();
            if (step == null || step.completionRule != TutorialCompletionRule.DishCompleted) return;

            bool matchesBoard = isDeliveryBoard == (step.targetBoardIndex == 1);
            if (matchesBoard)
            {
                EnterStep(_currentStepIndex + 1);
            }
        }

        private void HandleDishServed(bool isDeliveryBoard, RecipeSO recipe)
        {
            if (!_isGuidedActive) return;
            TutorialStepData step = GetCurrentStep();
            if (step == null || step.completionRule != TutorialCompletionRule.DishServed) return;

            bool matchesBoard = isDeliveryBoard == (step.targetBoardIndex == 1);
            if (matchesBoard)
            {
                EnterStep(_currentStepIndex + 1);
            }
        }

        private void HandleOrderSpawned(OrderModel order)
        {
            if (_isGuidedActive || !_isSoftHintMode || _panHintShown || sequence == null || order == null) return;
            if (sequence.panHintRecipe != null && order.Recipe == sequence.panHintRecipe)
            {
                _panHintShown = true;
                ShowSoftHint("pan", sequence.panHintText, sequence.panHintAnchorIds, 6f);
            }
        }

        private void HandleSlotBurnt(string slotId, Vector3 worldPos, int penalty)
        {
            if (_isGuidedActive || !_isSoftHintMode || _burntHintShown || sequence == null) return;
            _burntHintShown = true;
            ShowSoftHint("burnt", sequence.burntHintText, sequence.burntHintAnchorIds, 6f);
        }

        private void HandleTrashModeChanged(bool active)
        {
            if (_isSoftHintMode && _activeSoftHintType == "burnt" && active)
            {
                DismissSoftHint();
            }
        }

        private void HandleShipOverlayVisibilityChanged(bool visible)
        {
            if (_isGuidedActive || sequence == null) return;

            if (!visible && _activeSoftHintType == "ship")
            {
                DismissSoftHint();
                return;
            }

            if (visible && !_shipHintShown && CookingSession.CurrentDay <= 1)
            {
                _shipHintShown = true;
                ShowSoftHint("ship", sequence.shipOverlayHintText, sequence.shipOverlayHintAnchorIds, 6f);
            }
        }

        private void ShowSoftHint(string hintType, string text, string[] anchorIds, float duration)
        {
            if (overlayView == null || string.IsNullOrEmpty(text)) return;

            _activeSoftHintType = hintType;
            _softHintRemaining = duration;
            ResolveAnchors(anchorIds);

            overlayView.ShowStep(
                "MẸO NHANH",
                text,
                _resolvedTargets,
                blockOutsideInputs: false,
                showSpotlight: false,
                showHandPointer: _resolvedTargets.Count > 0,
                showSkipButton: false,
                TooltipPlacement.Auto);
        }

        private void DismissSoftHint()
        {
            _activeSoftHintType = null;
            _softHintRemaining = 0f;
            if (!_isGuidedActive)
            {
                overlayView?.HideAll();
            }
        }

        private TutorialStepData GetCurrentStep()
        {
            if (sequence == null || _currentStepIndex < 0 || _currentStepIndex >= sequence.steps.Count) return null;
            return sequence.steps[_currentStepIndex];
        }

        private bool EvaluateCurrentStep()
        {
            TutorialStepData step = GetCurrentStep();
            if (step == null) return false;

            bool satisfied = step.completionRule switch
            {
                TutorialCompletionRule.StationIngredientPlaced => CheckStationPlacedGoals(step.ingredientGoals),
                TutorialCompletionRule.BoardIngredientCount => CheckBoardIngredientGoals(step.targetBoardIndex, step.ingredientGoals),
                TutorialCompletionRule.BoardSelected => CheckBoardSelected(step.targetBoardIndex),
                _ => false
            };

            if (satisfied)
            {
                EnterStep(_currentStepIndex + 1);
                return true;
            }

            return false;
        }

        private void RefreshCurrentStepTargets()
        {
            if (!_isGuidedActive || overlayView == null) return;
            TutorialStepData step = GetCurrentStep();
            if (step == null) return;

            ResolveStepTargets(step, out bool hasPointerTarget);
            overlayView.RefreshTargets(
                _resolvedTargets,
                step.blockOutsideInputs,
                step.showSpotlight,
                step.showHandPointer && hasPointerTarget);
        }

        private bool CheckStationPlacedGoals(List<TutorialIngredientGoal> goals)
        {
            if (heatStationPresenter == null || goals == null || goals.Count == 0) return false;

            for (int i = 0; i < goals.Count; i++)
            {
                TutorialIngredientGoal goal = goals[i];
                if (goal == null || string.IsNullOrEmpty(goal.ingredientId)) continue;

                int count = CountStationIngredient(goal.ingredientId);
                if (count < goal.requiredAmount) return false;
            }
            return true;
        }

        private int CountStationIngredient(string ingredientId)
        {
            int count = 0;
            if (heatStationPresenter.grillViews != null)
            {
                for (int i = 0; i < heatStationPresenter.grillViews.Length; i++)
                {
                    if (MatchesStationSlot(heatStationPresenter.grillViews[i], ingredientId)) count++;
                }
            }
            if (MatchesStationSlot(heatStationPresenter.ovenView, ingredientId)) count++;
            if (MatchesStationSlot(heatStationPresenter.panView, ingredientId)) count++;
            return count;
        }

        private static bool MatchesStationSlot(HeatSlotView slot, string ingredientId)
        {
            return slot != null
                && slot.CurrentState != HeatSlotState.Idle
                && slot.CurrentIngredient != null
                && slot.CurrentIngredient.id == ingredientId;
        }

        private bool CheckBoardIngredientGoals(int boardIndex, List<TutorialIngredientGoal> goals)
        {
            if (workstationPresenter == null || workstationPresenter.boards == null) return false;
            if (boardIndex < 0 || boardIndex >= workstationPresenter.boards.Length) return false;

            BoardView board = workstationPresenter.boards[boardIndex];
            if (board == null || goals == null || goals.Count == 0) return false;

            for (int i = 0; i < goals.Count; i++)
            {
                TutorialIngredientGoal goal = goals[i];
                if (goal == null || string.IsNullOrEmpty(goal.ingredientId)) continue;

                board.HaveCounts.TryGetValue(goal.ingredientId, out int have);
                if (have < goal.requiredAmount) return false;
            }
            return true;
        }

        private bool CheckBoardSelected(int boardIndex)
        {
            if (workstationPresenter == null || workstationPresenter.boards == null) return false;
            if (boardIndex < 0 || boardIndex >= workstationPresenter.boards.Length) return false;
            return workstationPresenter.ActiveBoard == workstationPresenter.boards[boardIndex];
        }

        private void ResolveAnchors(string[] anchorIds)
        {
            ResolveAnchorsInto(anchorIds, _resolvedTargets);
        }

        private static void ResolveAnchorsInto(string[] anchorIds, List<RectTransform> output)
        {
            output.Clear();
            if (anchorIds == null) return;

            for (int i = 0; i < anchorIds.Length; i++)
            {
                string anchorId = anchorIds[i];
                if (TutorialAnchorRegistry.TryGet(anchorId, out RectTransform rect))
                {
                    output.Add(rect);
                }
                else if (!string.IsNullOrEmpty(anchorId))
                {
                    Debug.LogWarning($"[CookingTutorialDirector] Missing TutorialAnchor '{anchorId}' in active scene.");
                }
            }
        }

        private void ResolveStepTargets(TutorialStepData step, out bool hasPointerTarget)
        {
            ResolveAnchorsInto(step.targetAnchorIds, _rawTargets);
            _resolvedTargets.Clear();
            hasPointerTarget = _rawTargets.Count > 0;

            if (step.completionRule != TutorialCompletionRule.BoardIngredientCount
                || heatStationPresenter == null
                || _rawTargets.Count == 0)
            {
                _resolvedTargets.AddRange(_rawTargets);
                return;
            }

            bool hasHeatSlotAnchor = false;
            for (int i = 0; i < _rawTargets.Count; i++)
            {
                if (FindHeatSlotByRect(_rawTargets[i]) != null)
                {
                    hasHeatSlotAnchor = true;
                    break;
                }
            }

            if (!hasHeatSlotAnchor)
            {
                _resolvedTargets.AddRange(_rawTargets);
                return;
            }

            BoardView board = GetBoardByIndex(step.targetBoardIndex);
            _slotCandidates.Clear();

            for (int i = 0; i < _rawTargets.Count; i++)
            {
                RectTransform rect = _rawTargets[i];
                HeatSlotView slot = FindHeatSlotByRect(rect);
                if (slot == null)
                {
                    _slotCandidates.Add(new SlotTargetCandidate(rect, 0, i));
                    continue;
                }

                if (slot.CurrentState == HeatSlotState.Idle || slot.CurrentIngredient == null)
                {
                    continue;
                }

                if (!IsGoalIngredientStillNeeded(board, step.ingredientGoals, slot.CurrentIngredient.id))
                {
                    continue;
                }

                int priority = slot.CurrentState switch
                {
                    HeatSlotState.Perfect => 0,
                    HeatSlotState.Warning => 0,
                    HeatSlotState.Cooking => 1,
                    _ => 2
                };

                _slotCandidates.Add(new SlotTargetCandidate(rect, priority, i));
            }

            if (_slotCandidates.Count > 0)
            {
                _slotCandidates.Sort((a, b) =>
                {
                    int byPriority = a.Priority.CompareTo(b.Priority);
                    return byPriority != 0 ? byPriority : a.Order.CompareTo(b.Order);
                });

                for (int i = 0; i < _slotCandidates.Count; i++)
                {
                    _resolvedTargets.Add(_slotCandidates[i].Rect);
                }
                hasPointerTarget = true;
            }
            else
            {
                // Keep spotlight bounds active while the final ingredient flies to the board
                _resolvedTargets.AddRange(_rawTargets);
                hasPointerTarget = false;
            }
        }

        private BoardView GetBoardByIndex(int boardIndex)
        {
            if (workstationPresenter == null || workstationPresenter.boards == null) return null;
            if (boardIndex < 0 || boardIndex >= workstationPresenter.boards.Length) return null;
            return workstationPresenter.boards[boardIndex];
        }

        private HeatSlotView FindHeatSlotByRect(RectTransform rect)
        {
            if (rect == null || heatStationPresenter == null) return null;

            if (MatchesSlotTransform(heatStationPresenter.ovenView, rect))
            {
                return heatStationPresenter.ovenView;
            }

            if (MatchesSlotTransform(heatStationPresenter.panView, rect))
            {
                return heatStationPresenter.panView;
            }

            if (heatStationPresenter.grillViews != null)
            {
                for (int i = 0; i < heatStationPresenter.grillViews.Length; i++)
                {
                    HeatSlotView grill = heatStationPresenter.grillViews[i];
                    if (MatchesSlotTransform(grill, rect))
                    {
                        return grill;
                    }
                }
            }

            return null;
        }

        private static bool MatchesSlotTransform(HeatSlotView slot, RectTransform anchorRect)
        {
            return slot != null && (anchorRect == (RectTransform)slot.transform || anchorRect.IsChildOf(slot.transform));
        }

        private static bool IsGoalIngredientStillNeeded(BoardView board, List<TutorialIngredientGoal> goals, string ingredientId)
        {
            if (string.IsNullOrEmpty(ingredientId)) return false;
            if (goals == null || goals.Count == 0) return true;

            for (int i = 0; i < goals.Count; i++)
            {
                TutorialIngredientGoal goal = goals[i];
                if (goal == null || goal.ingredientId != ingredientId) continue;

                if (board == null) return true;
                board.HaveCounts.TryGetValue(ingredientId, out int have);
                board.PendingCounts.TryGetValue(ingredientId, out int pending);
                return have + pending < goal.requiredAmount;
            }

            return false;
        }
    }
}
