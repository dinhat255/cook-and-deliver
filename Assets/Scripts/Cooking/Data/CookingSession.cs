using System;
using System.Collections.Generic;
using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    [Serializable]
    public class CookedDeliveryOrder
    {
        public string OrderId;
        public string DishName;
        public int BaseReward;

        public CookedDeliveryOrder(string id, string dishName, int baseReward)
        {
            OrderId = id;
            DishName = dishName;
            BaseReward = baseReward;
        }
    }

    // Persistent session data bridging Cooking and Delivery across scene loads
    public static class CookingSession
    {
        public static int Coins { get; set; }
        public static int Combo { get; set; }
        public static int CompletedOrdersCount { get; set; }

        // Day-based progression
        public static int CurrentDay { get; set; } = 1;
        public static int DayTargetDineIn { get; set; } = 4;
        public static int DayTargetDelivery { get; set; } = 3;
        public static int DayDineInCompleted { get; set; }
        public static int DayDeliveryCompleted { get; set; }

        public static bool IsDayTargetMet =>
            DayDineInCompleted >= DayTargetDineIn && DayDeliveryCompleted >= DayTargetDelivery;

        // Cooked orders queue destined for delivery
        public static readonly List<CookedDeliveryOrder> CookedDeliveryOrders = new List<CookedDeliveryOrder>();

        // Delivery results buffer
        public static bool HasDeliveryResult { get; set; }
        public static int LastDeliveryEarned { get; set; }
        public static int LastDeliveryPenalty { get; set; }
        public static int LastDeliveredCount { get; set; }
        public static int LastDeliveryTotalOrders { get; set; }
        public static bool LastDeliveryCargoLost { get; set; }

        public static void ResetSessionForTest()
        {
            ResetSession();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Coins = 0;
            Combo = 0;
            CompletedOrdersCount = 0;
            CurrentDay = 1;
            DayTargetDineIn = 4;
            DayTargetDelivery = 3;
            DayDineInCompleted = 0;
            DayDeliveryCompleted = 0;
            CookedDeliveryOrders.Clear();
            HasDeliveryResult = false;
            LastDeliveryEarned = 0;
            LastDeliveryPenalty = 0;
            LastDeliveredCount = 0;
            LastDeliveryTotalOrders = 0;
            LastDeliveryCargoLost = false;
        }

        public static void AddCookedDeliveryOrder(string id, string dishName, int baseReward)
        {
            string prefix = string.IsNullOrEmpty(id) ? "ship" : id;
            string cleanId = $"{prefix}_{CookedDeliveryOrders.Count + 1}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            string cleanName = string.IsNullOrEmpty(dishName) ? "Món giao hàng" : dishName;
            int reward = Mathf.Max(50, baseReward);
            CookedDeliveryOrders.Add(new CookedDeliveryOrder(cleanId, cleanName, reward));
        }

        public static void ClearDeliveryOrders()
        {
            CookedDeliveryOrders.Clear();
        }

        public static DeliveryRunRequest BuildDeliveryRunRequest()
        {
            var list = new List<DeliveryOrder>();
            var usedIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < CookedDeliveryOrders.Count; i++)
            {
                var item = CookedDeliveryOrders[i];
                string uniqueId = item.OrderId;
                if (string.IsNullOrEmpty(uniqueId) || !usedIds.Add(uniqueId))
                {
                    uniqueId = $"ship_{i + 1}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                    usedIds.Add(uniqueId);
                }

                list.Add(new DeliveryOrder(uniqueId, item.DishName, item.BaseReward));
            }

            if (list.Count == 0)
            {
                list.Add(new DeliveryOrder("ship_fb_01", "Phở Bò Tái", 120));
                list.Add(new DeliveryOrder("ship_fb_02", "Bánh Mì Thịt Nướng", 100));
                list.Add(new DeliveryOrder("ship_fb_03", "Tteokbokki Cay", 110));
            }

            return new DeliveryRunRequest(Environment.TickCount, list);
        }

        public static void ApplyDeliveryResult(DeliveryRunResult result)
        {
            if (result == null) return;

            LastDeliveredCount = result.DeliveredCount;
            LastDeliveryTotalOrders = result.OrderCount;
            LastDeliveryCargoLost = result.Reason == EndReason.CargoLost;
            LastDeliveryEarned = result.Score;
            LastDeliveryPenalty = 0;

            if (LastDeliveryCargoLost)
            {
                int undelivered = Mathf.Max(0, result.OrderCount - result.DeliveredCount);
                LastDeliveryPenalty = undelivered * 50;
            }

            int netCoins = LastDeliveryEarned - LastDeliveryPenalty;
            Coins = Mathf.Max(0, Coins + netCoins);
            DayDeliveryCompleted += result.DeliveredCount;
            HasDeliveryResult = true;
            GameProgressStore.SyncFromSession();
        }

        public static void AdvanceToNextDay()
        {
            CurrentDay++;
            DayDineInCompleted = 0;
            DayDeliveryCompleted = 0;
            ClearDeliveryOrders();
            GameProgressStore.SyncFromSession();
        }
    }
}
