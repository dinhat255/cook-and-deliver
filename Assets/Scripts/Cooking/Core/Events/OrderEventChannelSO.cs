using System;
using CookAndDeliver.Cooking.Core;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Events
{
    // ScriptableObject event channel for decoupled order lifecycle events
    [CreateAssetMenu(fileName = "OrderEventChannel", menuName = "Cooking/Events/Order Event Channel", order = 1)]
    public class OrderEventChannelSO : ScriptableObject
    {
        public event Action<OrderModel> OnOrderSpawned;
        public event Action<OrderModel, int> OnOrderCompleted;
        public event Action<OrderModel> OnOrderExpired;

        public void RaiseOrderSpawned(OrderModel order)
        {
            OnOrderSpawned?.Invoke(order);
        }

        public void RaiseOrderCompleted(OrderModel order, int earnings)
        {
            OnOrderCompleted?.Invoke(order, earnings);
        }

        public void RaiseOrderExpired(OrderModel order)
        {
            OnOrderExpired?.Invoke(order);
        }
    }
}
