using System;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Core.Services;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.UI;
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

        [Header("VFX & UI Pool Layer")]
        public RectTransform flyLayer;

        [Header("Event Channels")]
        public OrderEventChannelSO orderEventChannel;
        public StationEventChannelSO stationEventChannel;
        public WorkstationEventChannelSO workstationEventChannel;
        public EconomyEventChannelSO economyEventChannel;

        bool isGameFrozen;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            SetupHeatSlotClicks();
            SetupTrayButtons();
            SetupPotButton();
            SetupOverlay();
            SetupPresenterEvents();
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

        void Start()
        {
            if (orderPresenter != null)
            {
                orderPresenter.InitializeOrders();
            }

            if (workstationPresenter != null && workstationPresenter.boards != null && workstationPresenter.boards.Length > 0)
            {
                workstationPresenter.SetActiveBoard(workstationPresenter.boards[0]);
            }

            UpdateMutualHighlights();

            // Sync initial HUD
            economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, 0);
            economyEventChannel?.RaiseComboUpdated(CookingSession.Combo);
            int goal = config != null ? config.shipGoal : 3;
            economyEventChannel?.RaiseShipCountUpdated(0, goal);
        }

        void Update()
        {
            if (isGameFrozen) return;

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
            if (orderPresenter != null) orderPresenter.SetPaused(true);
            if (heatStationPresenter != null) heatStationPresenter.SetPaused(true);
            if (workstationPresenter != null) workstationPresenter.SetPaused(true);

            DOVirtual.DelayedCall(0.6f, () =>
            {
                if (shipOverlay != null) shipOverlay.Show(true);
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
            });
        }

        // ------------------------------------------------------------- Tray & Station Actions

        void OnTrayIngredientClicked(TrayButtonView buttonView)
        {
            if (isGameFrozen || buttonView == null || buttonView.boundIngredient == null) return;

            var ing = buttonView.boundIngredient;
            var cell = (RectTransform)buttonView.transform;
            cell.DOKill();
            cell.localScale = Vector3.one;
            cell.DOPunchScale(Vector3.one * 0.12f, 0.15f, 5, 0.5f);

            switch (ing.route)
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

        void HandleDirectIngredient(IngredientSO ingredient, TrayButtonView buttonView)
        {
            if (workstationPresenter == null) return;
            var targetBoard = workstationPresenter.GetTargetBoard(ingredient);

            if (targetBoard == null)
            {
                var fallback = workstationPresenter.ActiveBoard != null
                    ? workstationPresenter.ActiveBoard
                    : (workstationPresenter.boards != null && workstationPresenter.boards.Length > 0 ? workstationPresenter.boards[0] : null);

                if (fallback != null)
                {
                    FlyBounce(ingredient.rawSprite, Color.white, buttonView.iconImage.transform.position, fallback.CenterWorldPos);
                    fallback.RectTransform.DOKill();
                    fallback.RectTransform.DOShakePosition(0.25f, 12f, 30);
                    ShowToast("Không cần món này", fallback.RectTransform.position, new Color(1f, 0.5f, 0.4f));
                }
                return;
            }

            SendIngredientToBoard(targetBoard, ingredient, ingredient.rawSprite, Color.white, buttonView.iconImage.transform.position);
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
            FlyArc(ingredient.rawSprite, Color.white, startPos, slot.IconWorldPos, duration, () =>
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
                if (board.AddIngredient(ingredient, version))
                {
                    workstationEventChannel?.RaiseDishCompleted(board.isDeliveryBoard, board.CurrentOrder.Recipe, board.FinishedDishWorldPos);
                }
            });
        }

        void OnHeatSlotClicked(HeatSlotView slot)
        {
            if (isGameFrozen || slot == null || slot.CurrentIngredient == null) return;

            switch (slot.CurrentState)
            {
                case HeatSlotState.Idle:
                    return;
                case HeatSlotState.Cooking:
                    ShowToast("Chưa chín!", slot.IconWorldPos, new Color(0.8f, 0.9f, 1f));
                    return;
                case HeatSlotState.Burnt:
                    int penalty = config != null ? config.burnPenaltyCoins : 15;
                    slot.ClearSlot();
                    CookingSession.Coins = Mathf.Max(0, CookingSession.Coins - penalty);
                    CookingSession.Combo = 0;
                    economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, -penalty);
                    economyEventChannel?.RaiseComboUpdated(0);
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
            slot.ClearSlot();

            SendIngredientToBoard(targetBoard, ing, sprite, tint, fromPos);
        }

        void OnPotClicked()
        {
            if (isGameFrozen || workstationPresenter == null) return;

            if (potRect != null)
            {
                potRect.DOKill();
                potRect.localScale = Vector3.one;
                potRect.DOPunchScale(Vector3.one * 0.08f, 0.2f, 5, 0.5f);
            }

            var broth = recipeDatabase != null ? recipeDatabase.FindIngredientById("broth") : null;
            if (broth == null) return;

            var targetBoard = workstationPresenter.GetTargetBoard(broth);
            if (targetBoard == null)
            {
                ShowToast("Cần bánh phở trước", potRect != null ? potRect.position : transform.position, new Color(1f, 0.5f, 0.4f));
                return;
            }

            Vector3 startPos = potRect != null ? potRect.position : transform.position;
            SendIngredientToBoard(targetBoard, broth, broth.rawSprite, Color.white, startPos);
        }

        void OnTrashClicked(TrayButtonView trashView)
        {
            if (isGameFrozen || trashView == null || workstationPresenter == null) return;

            var cell = (RectTransform)trashView.transform;
            cell.DOKill();
            cell.localScale = Vector3.one;
            cell.DOPunchScale(Vector3.one * 0.12f, 0.15f, 5, 0.5f);

            workstationPresenter.ClearActiveBoard();
        }

        void OnTicketClicked(int index)
        {
            if (isGameFrozen || orderPresenter == null || workstationPresenter == null) return;

            var order = orderPresenter.GetOrder(index);
            if (order != null)
            {
                workstationPresenter.HandleTicketSelected(order);
                UpdateMutualHighlights();
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

        void OnGoToDelivery()
        {
            DOTween.KillAll();
            SceneManager.LoadScene("Delivery");
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
