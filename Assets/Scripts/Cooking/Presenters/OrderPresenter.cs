using System.Collections.Generic;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.UI;
using DG.Tweening;
using UnityEngine;

namespace CookAndDeliver.Cooking.Presenters
{
    // Presenter in MVP managing order spawning, 2-ticket active KDS rail, sliding queue animations, and queue preview popup
    public class OrderPresenter : MonoBehaviour
    {
        [Header("Configuration & Database")]
        public CookingConfigSO config;
        public RecipeDatabaseSO recipeDatabase;

        [Header("Views (2 Active Tickets)")]
        public TicketView[] ticketViews;
        public OrderQueuePopupView queuePopup;

        [Header("Event Channels")]
        public OrderEventChannelSO orderEventChannel;

        readonly List<OrderModel> activeOrders = new List<OrderModel>();
        readonly List<OrderModel> queuedOrders = new List<OrderModel>();

        float spawnTimer;
        bool isPaused;
        bool isSliding;

        Vector2[] defaultTicketPositions;

        public IReadOnlyList<OrderModel> ActiveOrders => activeOrders;
        public IReadOnlyList<OrderModel> QueuedOrders => queuedOrders;
        public int QueuedCount => queuedOrders.Count;

        void Awake()
        {
            RecordDefaultPositions();

            if (queuePopup != null)
            {
                queuePopup.OnOpenStateChanged += HandlePopupStateChanged;
            }
        }

        void OnEnable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderCompleted += HandleOrderCompleted;
            }
        }

        void OnDisable()
        {
            if (orderEventChannel != null)
            {
                orderEventChannel.OnOrderCompleted -= HandleOrderCompleted;
            }
        }

        void HandleOrderCompleted(OrderModel order, int earnings)
        {
            RemoveOrder(order);
        }

        void OnDestroy()
        {
            if (queuePopup != null)
            {
                queuePopup.OnOpenStateChanged -= HandlePopupStateChanged;
            }
        }

        void RecordDefaultPositions()
        {
            if (ticketViews == null || ticketViews.Length == 0) return;
            defaultTicketPositions = new Vector2[ticketViews.Length];
            for (int i = 0; i < ticketViews.Length; i++)
            {
                if (ticketViews[i] != null)
                {
                    defaultTicketPositions[i] = ((RectTransform)ticketViews[i].transform).anchoredPosition;
                }
            }
        }

        void Start()
        {
            spawnTimer = config != null ? config.orderSpawnIntervalMin : 8f;
        }

        void HandlePopupStateChanged(bool isOpen)
        {
            SetPaused(isOpen);
            if (isOpen && queuePopup != null)
            {
                queuePopup.PopulateQueue(queuedOrders);
            }
        }

        public void InitializeOrders()
        {
            activeOrders.Clear();
            activeOrders.Add(null);
            activeOrders.Add(null);
            queuedOrders.Clear();

            SpawnOrder(false);
            SpawnOrder(true);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void Tick(float deltaTime)
        {
            if (isPaused) return;

            // Tick patience timers on active orders
            for (int i = 0; i < activeOrders.Count; i++)
            {
                var order = activeOrders[i];
                if (order == null) continue;

                order.Tick(deltaTime);

                if (order.IsExpired)
                {
                    RemoveOrderWithSlide(order);
                    orderEventChannel?.RaiseOrderExpired(order);
                }
            }

            // Spawn new orders (active max 2, queue max 8)
            int activeCount = 0;
            for (int i = 0; i < activeOrders.Count; i++)
            {
                if (activeOrders[i] != null) activeCount++;
            }

            int maxTotal = 8;
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f && (activeCount + queuedOrders.Count) < maxTotal)
            {
                float shipChance = config != null ? config.shipOrderProbability : 0.4f;
                SpawnOrder(Random.value < shipChance);

                float minInterval = config != null ? config.orderSpawnIntervalMin : 7f;
                float maxInterval = config != null ? config.orderSpawnIntervalMax : 11f;
                spawnTimer = Random.Range(minInterval, maxInterval);
            }

            UpdatePatienceBars();
        }

        public void SpawnOrder(bool isShip)
        {
            if (recipeDatabase == null) return;
            var recipe = recipeDatabase.GetRandomRecipe();
            if (recipe == null) return;

            float patience = config != null ? config.orderPatienceDuration : 60f;
            var order = new OrderModel(recipe, isShip, patience);

            while (activeOrders.Count < 2) activeOrders.Add(null);

            // Find first empty active slot: 0 (Left lane) or 1 (Right lane)
            int targetSlot = -1;
            if (activeOrders[0] == null) targetSlot = 0;
            else if (activeOrders[1] == null) targetSlot = 1;

            if (targetSlot != -1)
            {
                activeOrders[targetSlot] = order;
                AnimateTicketEntrance(targetSlot, order);
            }
            else
            {
                queuedOrders.Add(order);
                queuePopup?.UpdateBadgeCount(queuedOrders.Count);
            }

            orderEventChannel?.RaiseOrderSpawned(order);
            UpdateViews();
        }

        public void RemoveOrder(OrderModel order)
        {
            if (order == null) return;
            RemoveOrderWithSlide(order);
        }

        void RemoveOrderWithSlide(OrderModel order)
        {
            int index = activeOrders.IndexOf(order);
            if (index < 0)
            {
                queuedOrders.Remove(order);
                queuePopup?.UpdateBadgeCount(queuedOrders.Count);
                return;
            }

            // Refill from queue for THIS slot only
            OrderModel incoming = null;
            if (queuedOrders.Count > 0)
            {
                incoming = queuedOrders[0];
                queuedOrders.RemoveAt(0);
                queuePopup?.UpdateBadgeCount(queuedOrders.Count);
            }

            activeOrders[index] = incoming;

            // Animate only THIS slot independently
            AnimateLaneExitAndRefill(index, incoming);
        }

        void AnimateLaneExitAndRefill(int laneIndex, OrderModel incomingOrder)
        {
            if (ticketViews == null || laneIndex >= ticketViews.Length)
            {
                UpdateViews();
                return;
            }

            var tv = ticketViews[laneIndex];
            if (tv == null)
            {
                UpdateViews();
                return;
            }

            var rt = (RectTransform)tv.transform;
            if (defaultTicketPositions == null || defaultTicketPositions.Length <= laneIndex)
            {
                RecordDefaultPositions();
            }

            Vector2 defaultPos = defaultTicketPositions != null && defaultTicketPositions.Length > laneIndex
                ? defaultTicketPositions[laneIndex]
                : Vector2.zero;

            // Lane 0 exits left (-260f), Lane 1 exits right (+260f)
            float exitOffset = (laneIndex == 0) ? -260f : 260f;
            rt.DOKill();
            rt.DOAnchorPosX(defaultPos.x + exitOffset, 0.22f).SetEase(Ease.InCubic).OnComplete(() =>
            {
                if (incomingOrder != null)
                {
                    // Entrance: slide in from same outer edge
                    rt.anchoredPosition = new Vector2(defaultPos.x + exitOffset, defaultPos.y);
                    tv.BindOrder(incomingOrder);
                    rt.DOAnchorPos(defaultPos, 0.26f).SetEase(Ease.OutBack);
                }
                else
                {
                    tv.BindOrder(null);
                    rt.anchoredPosition = defaultPos;
                }
                UpdateViews();
            });
        }

        void AnimateTicketEntrance(int slotIndex, OrderModel order)
        {
            if (ticketViews == null || slotIndex >= ticketViews.Length) return;
            var tv = ticketViews[slotIndex];
            if (tv == null) return;

            var rt = (RectTransform)tv.transform;
            if (defaultTicketPositions == null || defaultTicketPositions.Length <= slotIndex)
            {
                RecordDefaultPositions();
            }

            Vector2 targetPos = defaultTicketPositions != null && defaultTicketPositions.Length > slotIndex
                ? defaultTicketPositions[slotIndex]
                : Vector2.zero;

            rt.DOKill();
            // Slot 0 enters from left (-260f), Slot 1 enters from right (+260f)
            float startOffsetX = (slotIndex == 0) ? -260f : 260f;
            rt.anchoredPosition = new Vector2(targetPos.x + startOffsetX, targetPos.y);
            tv.BindOrder(order);
            rt.DOAnchorPos(targetPos, 0.28f).SetEase(Ease.OutBack);
        }

        public OrderModel GetOrder(int index)
        {
            if (index >= 0 && index < activeOrders.Count) return activeOrders[index];
            return null;
        }

        public void UpdateViews()
        {
            if (ticketViews == null) return;

            for (int i = 0; i < ticketViews.Length; i++)
            {
                var view = ticketViews[i];
                if (view == null) continue;

                var order = (i < activeOrders.Count) ? activeOrders[i] : null;
                if (view.BoundOrder != order)
                {
                    view.BindOrder(order);
                }
            }

            queuePopup?.UpdateBadgeCount(queuedOrders.Count);
        }

        void UpdatePatienceBars()
        {
            if (ticketViews == null) return;
            for (int i = 0; i < ticketViews.Length; i++)
            {
                var view = ticketViews[i];
                var order = (i < activeOrders.Count) ? activeOrders[i] : null;
                if (view != null && order != null)
                {
                    view.UpdatePatience(order.PatienceRatio);
                }
            }
        }
    }
}
