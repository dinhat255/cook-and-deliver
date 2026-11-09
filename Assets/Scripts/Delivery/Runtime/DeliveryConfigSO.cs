using System;
using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Delivery
{
    [CreateAssetMenu(menuName = "Delivery/Standalone configuration")]
    public sealed class DeliveryConfigSO : ScriptableObject
    {
        public DeliverySettings settings = new DeliverySettings();
        [Range(.01f, .3f)] public float swipeWidthFraction = .06f;
        [Min(1f)] public float swipeHorizontalRatio = 1.25f;
        public string[] sampleOrderNames = { "Cơm gà nóng", "Bánh mì giòn", "Trà sữa mát", "Mì xào rau", "Bánh ngọt" };
        [Min(1)] public int baseReward = 100;

        public DeliveryRunRequest CreateRequest(int seed)
        {
            Validate();
            var orders = new DeliveryOrder[settings.OrderCount];
            for (var i = 0; i < orders.Length; i++)
                orders[i] = new DeliveryOrder("sample-" + (i + 1), sampleOrderNames[i % sampleOrderNames.Length], baseReward);
            return new DeliveryRunRequest(seed, orders);
        }

        public void Validate()
        {
            if (settings == null) throw new ArgumentException("Delivery settings are missing.");
            settings.Validate();
            if (sampleOrderNames == null || sampleOrderNames.Length == 0 || Array.Exists(sampleOrderNames, string.IsNullOrWhiteSpace))
                throw new ArgumentException("Delivery requires named sample orders.");
            if (baseReward < 1 || float.IsNaN(swipeWidthFraction) || float.IsInfinity(swipeWidthFraction)
                || swipeWidthFraction <= 0 || swipeWidthFraction > 1
                || float.IsNaN(swipeHorizontalRatio) || float.IsInfinity(swipeHorizontalRatio) || swipeHorizontalRatio < 1)
                throw new ArgumentException("Invalid Delivery input/reward settings.");
        }
    }
}
