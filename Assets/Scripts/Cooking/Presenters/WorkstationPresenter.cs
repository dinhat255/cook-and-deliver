using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Core.Services;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.UI;
using DG.Tweening;
using UnityEngine;

namespace CookAndDeliver.Cooking.Presenters
{
    // Presenter in MVP managing workstation cutting boards, assembly validation, and completion actions
    public class WorkstationPresenter : MonoBehaviour
    {
        [Header("Views")]
        public BoardView[] boards; // [0] Dine-in, [1] Delivery

        [Header("Configuration")]
        public CookingConfigSO config;

        [Header("Event Channels")]
        public OrderEventChannelSO orderEventChannel;
        public WorkstationEventChannelSO workstationEventChannel;
        public EconomyEventChannelSO economyEventChannel;

        BoardView activeBoard;
        bool isPaused;

        public BoardView ActiveBoard => activeBoard;

        public event Action<string, Vector3, Color> OnToastRequested;
        public event Action<Vector3, Color, int, float> OnSparkleRequested;
        public event Action<Sprite, Vector3> OnDishServed;
        public event Action<BoardView> OnActiveBoardChanged;

        void Awake()
        {
            SetupBoards();
        }

        void OnEnable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderExpired += HandleOrderExpired;
            }
        }

        void OnDisable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderExpired -= HandleOrderExpired;
            }
        }

        void Start()
        {
            if (boards != null && boards.Length > 0)
            {
                SetActiveBoard(boards[0]);
            }
        }

        void SetupBoards()
        {
            if (boards == null) return;
            foreach (var board in boards)
            {
                if (board == null) continue;
                var captured = board;
                if (captured.selectBoardButton != null)
                {
                    captured.selectBoardButton.onClick.AddListener(() =>
                    {
                        if (!isPaused) SetActiveBoard(captured);
                    });
                }

                if (captured.actionButton != null)
                {
                    captured.actionButton.onClick.AddListener(() =>
                    {
                        if (!isPaused) HandleBoardAction(captured);
                    });
                }
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void ResetShipProgress()
        {
            var economy = CookingServiceLocator.Get<IEconomyService>();
            if (economy != null)
            {
                economy.ResetShipProgress();
            }
            else
            {
                int goal = config != null ? config.shipGoal : 3;
                economyEventChannel?.RaiseShipCountUpdated(0, goal);
            }
        }

        public void SetActiveBoard(BoardView board)
        {
            activeBoard = board;
            if (boards == null) return;
            foreach (var b in boards)
            {
                if (b != null) b.SetHighlight(b == board);
            }
            OnActiveBoardChanged?.Invoke(activeBoard);
        }

        public bool IsOrderAssigned(OrderModel order)
        {
            if (boards == null || order == null) return false;
            foreach (var b in boards)
            {
                if (b != null && b.CurrentOrder == order) return true;
            }
            return false;
        }

        public void AssignOrdersToBoards(IReadOnlyList<OrderModel> activeOrders)
        {
            if (boards == null || activeOrders == null) return;

            // Direct 1-to-1 parallel assignment:
            // boards[0] (Left) <-> activeOrders[0] (Left Ticket)
            // boards[1] (Right) <-> activeOrders[1] (Right Ticket)
            for (int i = 0; i < boards.Length; i++)
            {
                var board = boards[i];
                if (board == null) continue;

                var order = (i < activeOrders.Count) ? activeOrders[i] : null;

                // Reset board if assigned order expired or was removed
                if (board.CurrentOrder != null && board.CurrentOrder != order)
                {
                    board.ResetBoard();
                }

                // Bind order if board has no order or order changed
                if (order != null && board.CurrentOrder != order)
                {
                    board.BindOrder(order);
                }
            }
        }

        bool ContainsOrder(IReadOnlyList<OrderModel> list, OrderModel target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == target) return true;
            }
            return false;
        }

        public BoardView GetTargetBoard(IngredientSO ingredient)
        {
            var preferred = activeBoard != null ? activeBoard : (boards != null && boards.Length > 0 ? boards[0] : null);
            if (preferred != null && preferred.CanAccept(ingredient)) return preferred;

            if (boards != null)
            {
                foreach (var other in boards)
                {
                    if (other != null && other != preferred && other.CanAccept(ingredient))
                        return other;
                }
            }
            return null;
        }

        public void HandleTicketSelected(OrderModel order)
        {
            if (isPaused || order == null || boards == null) return;

            // 1-to-1 matching: activate the board matching this order
            for (int i = 0; i < boards.Length; i++)
            {
                var b = boards[i];
                if (b != null && b.CurrentOrder == order)
                {
                    SetActiveBoard(b);
                    return;
                }
            }
        }

        public void HandleBoardAction(BoardView board)
        {
            if (isPaused || board == null) return;

            workstationEventChannel?.RaiseBoardActionTriggered(board.isDeliveryBoard, board.actionButtonRect.position);

            if (!board.IsComplete)
            {
                if (board.actionButtonRect != null)
                {
                    board.actionButtonRect.DOKill();
                    board.actionButtonRect.DOShakePosition(0.25f, 10f, 30);
                }
                var vfx = CookingServiceLocator.Get<IVfxService>();
                if (vfx != null)
                {
                    vfx.ShowToast("Chưa đủ món", board.actionButtonRect.position, new Color(1f, 0.5f, 0.4f));
                }
                else
                {
                    OnToastRequested?.Invoke("Chưa đủ món", board.actionButtonRect.position, new Color(1f, 0.5f, 0.4f));
                }
                return;
            }

            var order = board.CurrentOrder;
            if (order == null || order.Recipe == null) return;

            var recipe = order.Recipe;

            var economy = CookingServiceLocator.Get<IEconomyService>();
            int totalEarnings = economy != null
                ? economy.CalculateEarnings(recipe, order.PatienceRatio, out _, out _)
                : recipe.basePrice;

            if (economy != null)
            {
                economy.AddEarnings(totalEarnings);
            }

            orderEventChannel?.RaiseOrderCompleted(order, totalEarnings);

            var sprite = board.FinishedDishSprite;
            var dishPos = board.FinishedDishWorldPos;
            var buttonPos = board.actionButtonRect.position;

            workstationEventChannel?.RaiseDishCompleted(board.isDeliveryBoard, recipe, dishPos);
            board.ResetBoard();

            OnDishServed?.Invoke(sprite, dishPos);

            var vfxService = CookingServiceLocator.Get<IVfxService>();
            if (vfxService != null)
            {
                vfxService.ShowToast($"+{totalEarnings} xu", buttonPos, new Color(1f, 0.85f, 0.25f));
            }
            else
            {
                OnToastRequested?.Invoke($"+{totalEarnings} xu", buttonPos, new Color(1f, 0.85f, 0.25f));
            }

            CookingServiceLocator.Get<IAudioService>()?.PlaySound(CookingSound.Pack);

            if (board.isDeliveryBoard && economy != null)
            {
                int goal = config != null ? config.shipGoal : 3;
                economy.RecordShipOrderCompleted(goal);
            }
        }

        public void ClearActiveBoard()
        {
            if (isPaused) return;

            var b = activeBoard != null ? activeBoard : (boards != null && boards.Length > 0 ? boards[0] : null);
            if (b == null) return;

            if (b.HaveCounts.Count == 0 && !b.IsComplete && b.PendingCounts.Count == 0)
            {
                var vfx = CookingServiceLocator.Get<IVfxService>();
                if (vfx != null)
                {
                    vfx.ShowToast("Thớt trống", b.RectTransform.position, new Color(0.8f, 0.9f, 1f));
                }
                else
                {
                    OnToastRequested?.Invoke("Thớt trống", b.RectTransform.position, new Color(0.8f, 0.9f, 1f));
                }
                return;
            }

            int fee = config != null ? config.trashFeeCoins : 5;
            b.ClearBoard();

            var economy = CookingServiceLocator.Get<IEconomyService>();
            if (economy != null)
            {
                economy.DeductCoins(fee);
            }

            workstationEventChannel?.RaiseBoardCleared(b.isDeliveryBoard);

            var vfxService = CookingServiceLocator.Get<IVfxService>();
            if (vfxService != null)
            {
                vfxService.ShowToast($"-{fee} xu", b.RectTransform.position, new Color(1f, 0.6f, 0.4f));
                vfxService.EmitSparkle(b.CenterWorldPos, new Color(0.7f, 0.7f, 0.7f), 6, 60f);
            }
            else
            {
                OnToastRequested?.Invoke($"-{fee} xu", b.RectTransform.position, new Color(1f, 0.6f, 0.4f));
                OnSparkleRequested?.Invoke(b.CenterWorldPos, new Color(0.7f, 0.7f, 0.7f), 6, 60f);
            }

            CookingServiceLocator.Get<IAudioService>()?.PlaySound(CookingSound.Trash);
        }

        void HandleOrderExpired(OrderModel expiredOrder)
        {
            if (boards == null || expiredOrder == null) return;
            foreach (var b in boards)
            {
                if (b != null && b.CurrentOrder == expiredOrder)
                {
                    b.ResetBoard();
                }
            }
        }
    }
}

