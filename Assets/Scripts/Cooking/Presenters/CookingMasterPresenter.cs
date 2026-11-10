using System;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Core.Services;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.UI;
using CookAndDeliver.Delivery;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.Presenters
{
    // Master Presenter orchestrating sub-presenters, event channels, object pooling, and user interactions
    public class CookingMasterPresenter : MonoBehaviour
    {
        [Header("Configuration & Database")]
        public CookingConfigSO config;
        public RecipeDatabaseSO recipeDatabase;

        [Header("Sub-Presenters")]
        public OrderPresenter orderPresenter;
        public HeatStationPresenter heatStationPresenter;
        public WorkstationPresenter workstationPresenter;
        public HudPresenter hudPresenter;

        [Header("Interactive Views")]
        public TrayButtonView[] trayButtons;
        public Button potButton;
        public RectTransform potRect;
        public ShipOverlayView shipOverlay;
        public CulinaryCodexPopupView codexPopup;
        public DaySummaryPopupView daySummaryPopup;

        [Header("VFX & UI Pool Layer")]
        public RectTransform flyLayer;

        [Header("Event Channels")]
        public OrderEventChannelSO orderEventChannel;
        public StationEventChannelSO stationEventChannel;
        public WorkstationEventChannelSO workstationEventChannel;
        public EconomyEventChannelSO economyEventChannel;

        bool isGameFrozen;
        bool isSessionReady;
        bool isPotHinting;
        bool isTrashModeActive;
        bool isShiftEndingPending;
        TrayButtonView cachedTrashButtonView;

        public event Action OnSessionReady;
        public event Action<bool> OnTrashModeChanged;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            SetupHeatSlotClicks();
            SetupTrayButtons();
            SetupBoardClicks();
            SetupPotButton();
            SetupOverlay();
            SetupPresenterEvents();
            SetupDeliveryBridge();
        }

        void OnEnable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderExpired += HandleOrderExpired;
            }

            if (workstationEventChannel != null)
            {
                workstationEventChannel.OnDishCompleted += HandleDishCompleted;
            }

            if (economyEventChannel != null)
            {
                economyEventChannel.OnCookingDeepFrozen += HandleCookingDeepFrozen;
            }

            if (stationEventChannel != null)
            {
                stationEventChannel.OnSlotCookPerfect += HandleSlotCookPerfect;
            }
        }

        void OnDisable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderExpired -= HandleOrderExpired;
            }

            if (workstationEventChannel != null)
            {
                workstationEventChannel.OnDishCompleted -= HandleDishCompleted;
            }

            if (economyEventChannel != null)
            {
                economyEventChannel.OnCookingDeepFrozen -= HandleCookingDeepFrozen;
            }

            if (stationEventChannel != null)
            {
                stationEventChannel.OnSlotCookPerfect -= HandleSlotCookPerfect;
            }
        }

        async UniTaskVoid Start()
        {
            await GameProgressStore.EnsureLoadedAsync();
            SetupDeliveryBridge();

            // Sync initial HUD
            economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, 0);
            economyEventChannel?.RaiseComboUpdated(CookingSession.Combo);
            int goal = config != null ? config.shipGoal : 3;
            int initialShipDisplay = (CookingSession.HasDeliveryResult || CookingSession.DayDeliveryCompleted >= goal)
                ? Mathf.Min(Mathf.Max(CookingSession.CookedDeliveryOrders.Count, CookingSession.DayDeliveryCompleted), goal)
                : CookingSession.CookedDeliveryOrders.Count;
            economyEventChannel?.RaiseShipCountUpdated(initialShipDisplay, goal);

            if (workstationPresenter != null && workstationPresenter.boards != null && workstationPresenter.boards.Length > 0)
            {
                workstationPresenter.SetActiveBoard(workstationPresenter.boards[0]);
            }

            OnSessionReady?.Invoke();

            if (orderPresenter != null && !orderPresenter.HasAnyOrders())
            {
                orderPresenter.InitializeOrders();
            }

            if (workstationPresenter != null && orderPresenter != null)
            {
                workstationPresenter.AssignOrdersToBoards(orderPresenter.ActiveOrders);
            }

            UpdateMutualHighlights();
            isSessionReady = true;

            if (CookingSession.HasDeliveryResult)
            {
                HandleDeliveryReturn();
            }
        }

        void Update()
        {
            if (isGameFrozen || !isSessionReady) return;

            float dt = Time.deltaTime;

            if (orderPresenter != null)
            {
                orderPresenter.Tick(dt);
            }

            if (heatStationPresenter != null)
            {
                heatStationPresenter.Tick(dt);
            }

            if (workstationPresenter != null && orderPresenter != null)
            {
                workstationPresenter.AssignOrdersToBoards(orderPresenter.ActiveOrders);
                UpdateMutualHighlights();
                UpdatePotHint();
            }
        }

        // ------------------------------------------------------------- Setup

        void SetupHeatSlotClicks()
        {
            if (heatStationPresenter == null) return;

            if (heatStationPresenter.grillViews != null)
            {
                foreach (var slot in heatStationPresenter.grillViews)
                {
                    BindHeatSlot(slot);
                }
            }

            if (heatStationPresenter.ovenView != null)
            {
                BindHeatSlot(heatStationPresenter.ovenView);
            }

            if (heatStationPresenter.panView != null)
            {
                BindHeatSlot(heatStationPresenter.panView);
            }
        }

        void BindHeatSlot(HeatSlotView slot)
        {
            if (slot == null) return;
            var captured = slot;
            captured.OnEmitPuff = (pos, col) => EmitPuff(pos, col);
            captured.OnCookPerfect = (s) => stationEventChannel?.RaiseSlotCookPerfect(s.name, s.IconWorldPos);

            if (captured.clickButton != null)
            {
                captured.clickButton.onClick.AddListener(() => OnHeatSlotClicked(captured));
            }
        }

        void SetupTrayButtons()
        {
            if (trayButtons == null) return;
            foreach (var btn in trayButtons)
            {
                if (btn == null) continue;
                var captured = btn;
                if (captured.clickButton != null)
                {
                    captured.clickButton.onClick.AddListener(() =>
                    {
                        if (captured.isTrashButton) OnTrashClicked(captured);
                        else OnTrayIngredientClicked(captured);
                    });
                }
            }
        }

        void SetupBoardClicks()
        {
            if (workstationPresenter == null || workstationPresenter.boards == null) return;
            foreach (var b in workstationPresenter.boards)
            {
                if (b == null || b.selectBoardButton == null) continue;
                var captured = b;
                captured.selectBoardButton.onClick.AddListener(() => OnBoardClicked(captured));
            }
        }

        void OnBoardClicked(BoardView board)
        {
            if (isGameFrozen || board == null) return;

            if (isTrashModeActive)
            {
                if (board.HaveCounts.Count == 0 && !board.IsComplete && board.PendingCounts.Count == 0)
                {
                    ShowToast("Thớt trống", board.RectTransform.position, new Color(0.8f, 0.9f, 1f));
                    return;
                }

                if (workstationPresenter != null)
                {
                    workstationPresenter.ClearBoard(board);
                }
                SetTrashMode(false);
            }
        }

        void SetupPotButton()
        {
            if (potButton != null)
            {
                potButton.onClick.AddListener(OnPotClicked);
            }
        }

        void SetupOverlay()
        {
            if (shipOverlay == null) return;
            if (shipOverlay.deliverButton != null)
            {
                shipOverlay.deliverButton.onClick.AddListener(OnGoToDelivery);
            }
            if (shipOverlay.continueButton != null)
            {
                shipOverlay.continueButton.onClick.AddListener(OnContinueCooking);
            }
            shipOverlay.Show(false);
        }

        void SetupPresenterEvents()
        {
            if (workstationPresenter != null)
            {
                workstationPresenter.OnToastRequested += ShowToast;
                workstationPresenter.OnSparkleRequested += EmitSparkle;
                workstationPresenter.OnDishServed += HandleDishServed;
                workstationPresenter.OnActiveBoardChanged += (b) => UpdateMutualHighlights();
            }

            if (orderPresenter != null && orderPresenter.ticketViews != null)
            {
                for (int i = 0; i < orderPresenter.ticketViews.Length; i++)
                {
                    int index = i;
                    var ticket = orderPresenter.ticketViews[i];
                    if (ticket != null && ticket.clickButton != null)
                    {
                        ticket.clickButton.onClick.AddListener(() => OnTicketClicked(index));
                    }
                }
            }

            if (codexPopup != null)
            {
                codexPopup.OnOpenStateChanged += HandleCodexStateChanged;
            }

            if (daySummaryPopup != null)
            {
                daySummaryPopup.OnNextDayClicked += OnNextDayTransition;
                daySummaryPopup.OnCodexClicked += () => codexPopup?.Open();
                daySummaryPopup.OnMainMenuClicked += OnReturnToMainMenu;
            }
        }

        void OnDestroy()
        {
            SetTrashMode(false);
            if (codexPopup != null)
            {
                codexPopup.OnOpenStateChanged -= HandleCodexStateChanged;
            }

            if (daySummaryPopup != null)
            {
                daySummaryPopup.OnNextDayClicked -= OnNextDayTransition;
                daySummaryPopup.OnMainMenuClicked -= OnReturnToMainMenu;
            }
        }

        void HandleCodexStateChanged(bool isOpen)
        {
            if (orderPresenter != null) orderPresenter.SetPaused(isOpen);
            if (heatStationPresenter != null) heatStationPresenter.SetPaused(isOpen);
            if (workstationPresenter != null) workstationPresenter.SetPaused(isOpen);
        }

        // ------------------------------------------------------------- Event Handlers

        void HandleOrderExpired(OrderModel order)
        {
            CookingSession.Combo = 0;
            economyEventChannel?.RaiseComboUpdated(0);
            Vector3 pos = transform.position;
            if (orderPresenter != null && orderPresenter.ticketViews != null && orderPresenter.ticketViews.Length > 0)
            {
                pos = orderPresenter.ticketViews[0].transform.position;
            }
            ShowToast("Khách bỏ đi!", pos, new Color(1f, 0.4f, 0.4f));
        }

        void HandleDishCompleted(bool isDeliveryBoard, RecipeSO recipe, Vector3 dishPos)
        {
            EmitSparkle(dishPos, Color.white, 12, 110f);
            ShowToast("ĐỦ MÓN!", dishPos, new Color(0.5f, 1f, 0.5f));
        }

        void HandleSlotCookPerfect(string slotId, Vector3 worldPos)
        {
            EmitSparkle(worldPos, new Color(1f, 0.9f, 0.3f), 8, 70f);
        }

        void HandleCookingDeepFrozen()
        {
            isGameFrozen = true;
            SetTrashMode(false);
            if (orderPresenter != null) orderPresenter.SetPaused(true);
            if (heatStationPresenter != null) heatStationPresenter.SetPaused(true);
            if (workstationPresenter != null) workstationPresenter.SetPaused(true);

            DOVirtual.DelayedCall(0.6f, () =>
            {
                if (shipOverlay != null)
                {
                    if (shipOverlay.subtitleText != null)
                    {
                        shipOverlay.subtitleText.text = $"ĐÃ NẤU XONG {CookingSession.CookedDeliveryOrders.Count} HỘP SHIP CHO NGÀY {CookingSession.CurrentDay}!";
                    }
                    shipOverlay.Show(true);
                }
            }).SetLink(gameObject);
        }

        void HandleDishServed(Sprite dishSprite, Vector3 startPos)
        {
            Vector3 targetCoinsPos = hudPresenter != null && hudPresenter.hudView != null && hudPresenter.hudView.coinsText != null
                ? hudPresenter.hudView.coinsText.transform.position
                : transform.position;

            float flyDur = config != null ? config.serveFlyDuration : 0.4f;

            FlyArc(dishSprite, Color.white, startPos, targetCoinsPos, flyDur, () =>
            {
                if (hudPresenter != null && hudPresenter.hudView != null && hudPresenter.hudView.coinsText != null)
                {
                    hudPresenter.hudView.coinsText.transform.DOKill();
                    hudPresenter.hudView.coinsText.transform.localScale = Vector3.one;
                    hudPresenter.hudView.coinsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.25f, 6, 0.6f);
                }

                if (CookingSession.IsDayTargetMet && !isShiftEndingPending && (daySummaryPopup == null || !daySummaryPopup.IsOpen))
                {
                    BeginDayEndSequence();
                    DOVirtual.DelayedCall(0.6f, () => ShowDaySummary()).SetLink(gameObject);
                }
            });
        }

        // ------------------------------------------------------------- Tray & Station Actions

        void OnTrayIngredientClicked(TrayButtonView buttonView)
        {
            if (isGameFrozen || buttonView == null || buttonView.boundIngredient == null) return;

            if (isTrashModeActive)
            {
                SetTrashMode(false);
            }

            var ing = buttonView.boundIngredient;
            var cell = (RectTransform)buttonView.transform;
            cell.DOKill();
            cell.localScale = Vector3.one;
            cell.DOPunchScale(Vector3.one * 0.12f, 0.15f, 5, 0.5f);

            var route = ShouldRouteDirectly(ing) ? CookingRoute.Direct : ing.route;
            switch (route)
            {
                case CookingRoute.Direct:
                    HandleDirectIngredient(ing, buttonView);
                    break;
                case CookingRoute.Grill:
                    HandleGrillIngredient(ing, buttonView);
                    break;
                case CookingRoute.Oven:
                    HandleOvenIngredient(ing, buttonView);
                    break;
                case CookingRoute.Pan:
                    HandlePanIngredient(ing, buttonView);
                    break;
            }
        }

        bool ShouldRouteDirectly(IngredientSO ingredient)
        {
            if (ingredient == null) return false;
            if (ingredient.route == CookingRoute.Direct) return true;
            if (workstationPresenter == null) return false;

            var targetBoard = workstationPresenter.GetTargetBoard(ingredient);
            if (targetBoard != null && targetBoard.CurrentOrder != null && targetBoard.CurrentOrder.Recipe != null)
            {
                return !targetBoard.CurrentOrder.Recipe.RequiresCooking(ingredient);
            }

            return false;
        }

        void HandleDirectIngredient(IngredientSO ingredient, TrayButtonView buttonView)
        {
            if (workstationPresenter == null) return;
            var targetBoard = workstationPresenter.GetTargetBoard(ingredient);
            Color rawTint = ingredient.GetDisplayColor(false);

            if (targetBoard == null)
            {
                var fallback = workstationPresenter.ActiveBoard != null
                    ? workstationPresenter.ActiveBoard
                    : (workstationPresenter.boards != null && workstationPresenter.boards.Length > 0 ? workstationPresenter.boards[0] : null);

                if (fallback != null)
                {
                    FlyBounce(ingredient.rawSprite, rawTint, buttonView.iconImage.transform.position, fallback.CenterWorldPos);
                    fallback.RectTransform.DOKill();
                    fallback.RectTransform.DOShakePosition(0.25f, 12f, 30);
                    ShowToast("Không cần món này", fallback.RectTransform.position, new Color(1f, 0.5f, 0.4f));
                }
                return;
            }

            SendIngredientToBoard(targetBoard, ingredient, ingredient.rawSprite, rawTint, buttonView.iconImage.transform.position);
        }

        void HandleGrillIngredient(IngredientSO ingredient, TrayButtonView buttonView)
        {
            if (heatStationPresenter == null) return;
            var freeSlot = heatStationPresenter.GetFreeGrillSlot();

            if (freeSlot == null)
            {
                if (heatStationPresenter.grillRect != null)
                {
                    heatStationPresenter.grillRect.DOKill();
                    heatStationPresenter.grillRect.DOShakePosition(0.3f, 14f, 30);
                    ShowToast("ĐẦY VỈ!", heatStationPresenter.grillRect.position, new Color(1f, 0.4f, 0.3f));
                }
                return;
            }

            SendIngredientToHeatSlot(freeSlot, ingredient, buttonView.iconImage.transform.position);
        }

        void HandleOvenIngredient(IngredientSO ingredient, TrayButtonView buttonView)
        {
            if (heatStationPresenter == null) return;
            var ovenSlot = heatStationPresenter.GetOvenSlot();

            if (ovenSlot == null)
            {
                ShowStationBusy(heatStationPresenter.ovenRect, "Lò đang bận");
                return;
            }

            SendIngredientToHeatSlot(ovenSlot, ingredient, buttonView.iconImage.transform.position);
        }

        void HandlePanIngredient(IngredientSO ingredient, TrayButtonView buttonView)
        {
            if (heatStationPresenter == null) return;
            var panSlot = heatStationPresenter.GetPanSlot();

            if (panSlot == null)
            {
                ShowStationBusy(heatStationPresenter.panRect, "Chảo đang bận");
                return;
            }

            SendIngredientToHeatSlot(panSlot, ingredient, buttonView.iconImage.transform.position);
        }

        void ShowStationBusy(RectTransform rect, string message)
        {
            if (rect != null)
            {
                rect.DOKill();
                rect.DOShakePosition(0.25f, 10f, 30);
                ShowToast(message, rect.position, new Color(1f, 0.5f, 0.4f));
            }
        }

        void SendIngredientToHeatSlot(HeatSlotView slot, IngredientSO ingredient, Vector3 startPos)
        {
            slot.IsReserved = true;
            float duration = config != null ? config.smartTapFlyDuration : 0.18f;
            FlyArc(ingredient.rawSprite, ingredient.GetDisplayColor(false), startPos, slot.IconWorldPos, duration, () =>
            {
                if (heatStationPresenter != null)
                {
                    heatStationPresenter.PlaceIngredient(slot, ingredient);
                }
                else
                {
                    slot.PlaceIngredient(ingredient);
                }
            });
        }

        void SendIngredientToBoard(BoardView board, IngredientSO ingredient, Sprite sprite, Color tint, Vector3 startPos)
        {
            board.MarkPending(ingredient);
            int version = board.BoardVersion;
            float duration = config != null ? config.smartTapFlyDuration : 0.18f;
            Vector3 targetPos = board.GetSlotWorldPos(ingredient);

            FlyArc(sprite, tint, startPos, targetPos, duration, () =>
            {
                bool versionValid = version == board.BoardVersion;
                bool completed = board.AddIngredient(ingredient, version);
                if (versionValid)
                {
                    workstationPresenter?.NotifyIngredientAdded(board, ingredient);
                }
                if (completed)
                {
                    workstationEventChannel?.RaiseDishCompleted(board.isDeliveryBoard, board.CurrentOrder.Recipe, board.FinishedDishWorldPos);
                }
            });
        }

        void OnHeatSlotClicked(HeatSlotView slot)
        {
            if (isGameFrozen || slot == null) return;

            if (isTrashModeActive)
            {
                if (slot.CurrentState == HeatSlotState.Idle || slot.CurrentIngredient == null)
                {
                    ShowToast("Bếp trống", slot.IconWorldPos, new Color(0.8f, 0.9f, 1f));
                    return;
                }

                if (slot.CurrentState == HeatSlotState.Burnt)
                {
                    int penalty = config != null ? config.burnPenaltyCoins : 15;
                    ClearHeatSlot(slot);
                    DeductEconomy(penalty, true);
                    ShowToast($"-{penalty} xu (cháy)", slot.IconWorldPos, new Color(1f, 0.35f, 0.3f));
                }
                else
                {
                    int fee = config != null ? config.trashFeeCoins : 5;
                    ClearHeatSlot(slot);
                    DeductEconomy(fee, false);
                    ShowToast($"-{fee} xu (dọn bếp)", slot.IconWorldPos, new Color(1f, 0.6f, 0.4f));
                    EmitSparkle(slot.IconWorldPos, new Color(0.7f, 0.7f, 0.7f), 6, 60f);
                }

                CookingServiceLocator.Get<IAudioService>()?.PlaySound(CookingSound.Trash);
                SetTrashMode(false);
                return;
            }

            if (slot.CurrentIngredient == null) return;

            switch (slot.CurrentState)
            {
                case HeatSlotState.Idle:
                    return;
                case HeatSlotState.Cooking:
                    ShowToast("Chưa chín!", slot.IconWorldPos, new Color(0.8f, 0.9f, 1f));
                    return;
                case HeatSlotState.Burnt:
                    int penalty = config != null ? config.burnPenaltyCoins : 15;
                    ClearHeatSlot(slot);
                    DeductEconomy(penalty, true);
                    ShowToast($"-{penalty} xu (cháy)", slot.IconWorldPos, new Color(1f, 0.35f, 0.3f));
                    return;
            }

            // Slot is in Perfect or Warning state
            var ing = slot.CurrentIngredient;
            if (workstationPresenter == null) return;
            var targetBoard = workstationPresenter.GetTargetBoard(ing);

            if (targetBoard == null)
            {
                var fallback = workstationPresenter.ActiveBoard != null
                    ? workstationPresenter.ActiveBoard
                    : (workstationPresenter.boards != null && workstationPresenter.boards.Length > 0 ? workstationPresenter.boards[0] : null);

                if (fallback != null)
                {
                    FlyBounce(slot.IconSprite, slot.IconColor, slot.IconWorldPos, fallback.CenterWorldPos);
                    ShowToast("Chưa cần món này", slot.IconWorldPos, new Color(1f, 0.5f, 0.4f));
                }
                return;
            }

            var sprite = slot.IconSprite;
            var tint = slot.IconColor;
            var fromPos = slot.IconWorldPos;
            ClearHeatSlot(slot);

            SendIngredientToBoard(targetBoard, ing, sprite, tint, fromPos);
        }

        void ClearHeatSlot(HeatSlotView slot)
        {
            if (slot == null) return;
            if (heatStationPresenter != null)
            {
                heatStationPresenter.ClearSlot(slot);
            }
            else
            {
                slot.ClearSlot();
            }
        }

        void DeductEconomy(int penalty, bool resetCombo)
        {
            var economy = CookingServiceLocator.Get<IEconomyService>();
            if (economy != null)
            {
                economy.DeductCoins(penalty);
                if (resetCombo) economy.ResetCombo();
            }
            else
            {
                CookingSession.Coins = Mathf.Max(0, CookingSession.Coins - penalty);
                if (resetCombo) CookingSession.Combo = 0;
                GameProgressStore.SyncFromSession();
                economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, -penalty);
                if (resetCombo) economyEventChannel?.RaiseComboUpdated(0);
            }
        }

        void OnPotClicked()
        {
            if (isGameFrozen || workstationPresenter == null) return;

            if (isTrashModeActive)
            {
                SetTrashMode(false);
            }

            if (potRect != null)
            {
                potRect.DOKill();
                potRect.localScale = Vector3.one;
                isPotHinting = false;
                potRect.DOPunchScale(Vector3.one * 0.08f, 0.2f, 5, 0.5f);
            }

            var broth = recipeDatabase != null ? recipeDatabase.FindIngredientById("broth") : null;
            if (broth == null) return;

            var targetBoard = workstationPresenter.GetTargetBoard(broth);
            if (targetBoard == null)
            {
                RecipeSO brothRequiringRecipe = null;
                if (workstationPresenter.boards != null)
                {
                    foreach (var b in workstationPresenter.boards)
                    {
                        if (b == null || b.CurrentOrder == null || b.CurrentOrder.Recipe == null) continue;
                        int required = b.CurrentOrder.Recipe.GetRequiredAmount(broth);
                        if (required <= 0) continue;

                        b.HaveCounts.TryGetValue(broth.id, out int haveBroth);
                        b.PendingCounts.TryGetValue(broth.id, out int pendingBroth);
                        if (haveBroth + pendingBroth < required)
                        {
                            brothRequiringRecipe = b.CurrentOrder.Recipe;
                            if (b == workstationPresenter.ActiveBoard) break;
                        }
                    }
                }

                string toastMsg = "Không cần nước lèo";
                if (brothRequiringRecipe != null)
                {
                    bool needsRamen = brothRequiringRecipe.requirements != null &&
                        brothRequiringRecipe.requirements.Exists(r => r != null && r.ingredient != null && r.ingredient.id == "ramen");
                    toastMsg = needsRamen ? "Cần mì sợi trước" : "Cần bánh phở trước";
                }
                ShowToast(toastMsg, potRect != null ? potRect.position : transform.position, new Color(1f, 0.5f, 0.4f));
                return;
            }

            Vector3 startPos = potRect != null ? potRect.position : transform.position;
            SendIngredientToBoard(targetBoard, broth, broth.rawSprite, broth.GetDisplayColor(false), startPos);
        }

        void OnTrashClicked(TrayButtonView trashView)
        {
            if (isGameFrozen || trashView == null) return;

            cachedTrashButtonView = trashView;
            var cell = (RectTransform)trashView.transform;
            cell.DOKill();
            cell.localScale = Vector3.one;
            cell.DOPunchScale(Vector3.one * 0.12f, 0.15f, 5, 0.5f);

            SetTrashMode(!isTrashModeActive);
        }

        void SetTrashMode(bool active)
        {
            isTrashModeActive = active;

            if (cachedTrashButtonView == null && trayButtons != null)
            {
                foreach (var btn in trayButtons)
                {
                    if (btn != null && btn.isTrashButton)
                    {
                        cachedTrashButtonView = btn;
                        break;
                    }
                }
            }

            if (cachedTrashButtonView != null)
            {
                cachedTrashButtonView.SetTrashActive(isTrashModeActive);
            }

            if (isTrashModeActive)
            {
                var pos = cachedTrashButtonView != null ? cachedTrashButtonView.transform.position : transform.position;
                ShowToast("Chạm thớt hoặc bếp để dọn rác", pos + Vector3.up * 80f, new Color(1f, 0.85f, 0.4f));
            }

            OnTrashModeChanged?.Invoke(isTrashModeActive);
        }

        void OnTicketClicked(int index)
        {
            if (isGameFrozen || orderPresenter == null || workstationPresenter == null) return;

            if (isTrashModeActive)
            {
                SetTrashMode(false);
            }

            var order = orderPresenter.GetOrder(index);
            if (order != null)
            {
                workstationPresenter.HandleTicketSelected(order);
                UpdateMutualHighlights();
                UpdatePotHint();
            }
        }

        void UpdateMutualHighlights()
        {
            if (workstationPresenter == null || orderPresenter == null) return;
            var activeBoard = workstationPresenter.ActiveBoard;
            var activeOrder = activeBoard != null ? activeBoard.CurrentOrder : null;

            if (orderPresenter.ticketViews != null)
            {
                foreach (var tv in orderPresenter.ticketViews)
                {
                    if (tv != null)
                    {
                        bool isMatch = activeOrder != null && tv.BoundOrder != null && tv.BoundOrder == activeOrder;
                        tv.SetAssignedToWorkstation(isMatch);
                    }
                }
            }
        }

        void UpdatePotHint()
        {
            if (potRect == null || workstationPresenter == null || recipeDatabase == null) return;
            var broth = recipeDatabase.FindIngredientById("broth");
            if (broth == null) return;

            bool canReceiveBroth = workstationPresenter.GetTargetBoard(broth) != null;
            if (canReceiveBroth && !isPotHinting)
            {
                isPotHinting = true;
                potRect.DOKill();
                potRect.DOScale(1.08f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
            else if (!canReceiveBroth && isPotHinting)
            {
                isPotHinting = false;
                potRect.DOKill();
                potRect.localScale = Vector3.one;
            }
        }

        void SetupDeliveryBridge()
        {
            DeliveryController.ExternalRequestProvider = () =>
            {
                return CookingSession.BuildDeliveryRunRequest();
            };

            DeliveryController.ExternalPreviewOrdersProvider = () =>
            {
                return CookingSession.BuildDeliveryRunRequest().Orders;
            };

            DeliveryController.OnExternalRunCompleted = (result) =>
            {
                CookingSession.ApplyDeliveryResult(result);
            };

            DeliveryController.OnExternalReturnRequested = () =>
            {
                LoadSceneHandler.LoadSceneByName("Cooking");
            };

            DeliveryController.ExternalReturnButtonLabel = "Về quán";
            DeliveryController.PendingOrderCount = CookingSession.CookedDeliveryOrders.Count;
            DeliveryController.ExternalIsTutorialCompleted = () => GameProgressStore.IsDeliveryTutorialCompleted;
            DeliveryController.ExternalOnTutorialCompleted = () => GameProgressStore.MarkDeliveryTutorialCompleted();
        }

        void BeginDayEndSequence()
        {
            isGameFrozen = true;
            isShiftEndingPending = true;
            SetTrashMode(false);

            if (shipOverlay != null) shipOverlay.Show(false);
            if (orderPresenter != null && orderPresenter.queuePopup != null && orderPresenter.queuePopup.IsOpen)
            {
                orderPresenter.queuePopup.Close();
            }

            if (orderPresenter != null) orderPresenter.SetPaused(true);
            if (heatStationPresenter != null) heatStationPresenter.SetPaused(true);
            if (workstationPresenter != null) workstationPresenter.SetPaused(true);
        }

        void HandleDeliveryReturn()
        {
            int earned = CookingSession.LastDeliveryEarned;
            int penalty = CookingSession.LastDeliveryPenalty;
            int delivered = CookingSession.LastDeliveredCount;
            int total = CookingSession.LastDeliveryTotalOrders;
            bool lost = CookingSession.LastDeliveryCargoLost;
            int goal = config != null ? config.shipGoal : 3;

            if (CookingSession.IsDayTargetMet && !isShiftEndingPending)
            {
                BeginDayEndSequence();
            }

            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (lost)
                {
                    ShowToast($"HỎNG HÀNG! Phạt {penalty} xu ({delivered}/{total} đơn)", transform.position, new Color(1f, 0.4f, 0.4f));
                }
                else
                {
                    ShowToast($"GIAO XONG! +{earned} xu ({delivered}/{total} đơn)", transform.position, new Color(0.4f, 1f, 0.4f));
                }

                workstationPresenter?.ResetShipProgress();
                CookingSession.HasDeliveryResult = false;
                CookingSession.ClearDeliveryOrders();
                DeliveryController.PendingOrderCount = 0;

                if (CookingSession.DayDeliveryCompleted >= goal)
                {
                    economyEventChannel?.RaiseShipCountUpdated(goal, goal);
                }

                if (CookingSession.IsDayTargetMet && (daySummaryPopup == null || !daySummaryPopup.IsOpen))
                {
                    DOVirtual.DelayedCall(1.2f, () => ShowDaySummary()).SetLink(gameObject);
                }
            }).SetLink(gameObject);
        }

        void ShowDaySummary()
        {
            isShiftEndingPending = false;
            BeginDayEndSequence();

            if (orderPresenter != null) orderPresenter.ClearAllOrders();
            if (heatStationPresenter != null) heatStationPresenter.ClearAllSlots();
            if (workstationPresenter != null) workstationPresenter.ResetAllBoards();

            UpdateMutualHighlights();
            UpdatePotHint();

            int deliveryGoal = config != null ? config.shipGoal : 3;
            int displayShip = Mathf.Min(CookingSession.DayDeliveryCompleted, deliveryGoal);
            economyEventChannel?.RaiseShipCountUpdated(displayShip, deliveryGoal);

            int day = CookingSession.CurrentDay;
            int dineInOrders = CookingSession.DayDineInCompleted;
            int dineInIncome = dineInOrders * 110;
            int deliveryOrders = CookingSession.LastDeliveredCount;
            int deliveryIncome = CookingSession.LastDeliveryEarned;
            int penalty = CookingSession.LastDeliveryPenalty;
            int netIncome = dineInIncome + deliveryIncome - penalty;
            int stars = penalty == 0 && deliveryOrders >= deliveryGoal ? 3 : (deliveryOrders > 0 ? 2 : 1);

            if (daySummaryPopup != null)
            {
                daySummaryPopup.Show(day, dineInOrders, dineInIncome, deliveryOrders, deliveryGoal, deliveryIncome, penalty, netIncome, stars);
            }
            else
            {
                ShowToast($"HOÀN THÀNH NGÀY {day}! LÃI +{netIncome} XU", transform.position, new Color(1f, 0.85f, 0.2f));
                DOVirtual.DelayedCall(2.2f, () => OnNextDayTransition()).SetLink(gameObject);
            }
        }

        void OnNextDayTransition()
        {
            if (daySummaryPopup != null) daySummaryPopup.Close();

            CookingSession.AdvanceToNextDay();
            isGameFrozen = false;
            isShiftEndingPending = false;

            if (heatStationPresenter != null)
            {
                heatStationPresenter.SetPaused(false);
                heatStationPresenter.ClearAllSlots();
            }
            if (workstationPresenter != null)
            {
                workstationPresenter.SetPaused(false);
                workstationPresenter.ResetAllBoards();
                workstationPresenter.ResetShipProgress();
            }
            if (orderPresenter != null)
            {
                orderPresenter.SetPaused(false);
                orderPresenter.InitializeOrders();
            }
            if (workstationPresenter != null && orderPresenter != null)
            {
                workstationPresenter.AssignOrdersToBoards(orderPresenter.ActiveOrders);
                if (workstationPresenter.boards != null && workstationPresenter.boards.Length > 0)
                {
                    workstationPresenter.SetActiveBoard(workstationPresenter.boards[0]);
                }
            }

            UpdateMutualHighlights();
            UpdatePotHint();

            int g = config != null ? config.shipGoal : 3;
            economyEventChannel?.RaiseShipCountUpdated(0, g);
            ShowToast($"BẮT ĐẦU NGÀY {CookingSession.CurrentDay}!", transform.position, new Color(0.4f, 1f, 0.4f));
        }

        public void ResetForSkippedDay(int targetDay)
        {
            CookingSession.CurrentDay = Mathf.Max(1, targetDay);
            CookingSession.DayDineInCompleted = 0;
            CookingSession.DayDeliveryCompleted = 0;
            CookingSession.HasDeliveryResult = false;
            CookingSession.ClearDeliveryOrders();
            DeliveryController.PendingOrderCount = 0;

            isGameFrozen = false;
            isShiftEndingPending = false;
            SetTrashMode(false);

            if (shipOverlay != null) shipOverlay.Show(false);
            if (daySummaryPopup != null && daySummaryPopup.panelRoot != null)
            {
                daySummaryPopup.panelRoot.SetActive(false);
            }

            if (heatStationPresenter != null)
            {
                heatStationPresenter.SetPaused(false);
                heatStationPresenter.SetBurnEnabled(true);
                heatStationPresenter.ClearAllSlots();
            }

            if (workstationPresenter != null)
            {
                workstationPresenter.SetPaused(false);
                workstationPresenter.ResetAllBoards();
                workstationPresenter.ResetShipProgress();
            }

            if (orderPresenter != null)
            {
                orderPresenter.SetPaused(false);
                orderPresenter.SetPatienceDecayEnabled(true);
                orderPresenter.SetAutoSpawnEnabled(true);
                orderPresenter.InitializeOrders();
            }

            if (workstationPresenter != null && orderPresenter != null)
            {
                workstationPresenter.AssignOrdersToBoards(orderPresenter.ActiveOrders);
                if (workstationPresenter.boards != null && workstationPresenter.boards.Length > 0)
                {
                    workstationPresenter.SetActiveBoard(workstationPresenter.boards[0]);
                }
            }

            UpdateMutualHighlights();
            UpdatePotHint();

            int g = config != null ? config.shipGoal : 3;
            economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, 0);
            economyEventChannel?.RaiseComboUpdated(CookingSession.Combo);
            economyEventChannel?.RaiseShipCountUpdated(0, g);
            GameProgressStore.SyncFromSession(true);

            ShowToast($"BẮT ĐẦU NGÀY {CookingSession.CurrentDay}!", transform.position, new Color(0.4f, 1f, 0.4f));
        }

        void OnReturnToMainMenu()
        {
            GameProgressStore.SyncFromSession();
            DOTween.KillAll();
            LoadSceneHandler.LoadSceneByName("MainMenu");
        }

        void OnGoToDelivery()
        {
            GameProgressStore.SyncFromSession();
            DeliveryController.PendingOrderCount = CookingSession.CookedDeliveryOrders.Count;
            DOTween.KillAll();
            LoadSceneHandler.LoadSceneByName("Delivery");
        }

        void OnContinueCooking()
        {
            isGameFrozen = false;
            if (orderPresenter != null) orderPresenter.SetPaused(false);
            if (heatStationPresenter != null) heatStationPresenter.SetPaused(false);
            if (workstationPresenter != null)
            {
                workstationPresenter.SetPaused(false);
                workstationPresenter.ResetShipProgress();
            }
            if (shipOverlay != null) shipOverlay.Show(false);
        }

        // ------------------------------------------------------------- VFX via IVfxService

        public void FlyArc(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld, float duration, Action onArrive)
        {
            var vfx = CookingServiceLocator.Get<IVfxService>();
            if (vfx != null)
            {
                vfx.FlyArc(sprite, tint, fromWorld, toWorld, duration, onArrive);
            }
            else
            {
                onArrive?.Invoke();
            }
        }

        public void FlyBounce(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld)
        {
            var vfx = CookingServiceLocator.Get<IVfxService>();
            float duration = config != null ? config.smartTapFlyDuration : 0.18f;
            if (vfx != null)
            {
                vfx.FlyBounce(sprite, tint, fromWorld, toWorld, duration);
            }
        }

        public void ShowToast(string message, Vector3 worldPos, Color color)
        {
            CookingServiceLocator.Get<IVfxService>()?.ShowToast(message, worldPos, color);
        }

        public void EmitSparkle(Vector3 worldPos, Color color, int count, float distance)
        {
            CookingServiceLocator.Get<IVfxService>()?.EmitSparkle(worldPos, color, count, distance);
        }

        public void EmitPuff(Vector3 worldPos, Color color)
        {
            CookingServiceLocator.Get<IVfxService>()?.EmitPuff(worldPos, color);
        }
    }
}
