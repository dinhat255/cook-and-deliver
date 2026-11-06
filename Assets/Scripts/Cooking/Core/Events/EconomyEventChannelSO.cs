using System;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Events
{
    // ScriptableObject event channel for coins, combos, and delivery session state
    [CreateAssetMenu(fileName = "EconomyEventChannel", menuName = "Cooking/Events/Economy Event Channel", order = 4)]
    public class EconomyEventChannelSO : ScriptableObject
    {
        public event Action<int, int> OnCoinsUpdated;          // totalCoins, delta
        public event Action<int> OnComboUpdated;               // currentCombo
        public event Action<int, int> OnShipCountUpdated;      // currentShip, goal
        public event Action OnCookingDeepFrozen;               // triggered when ship goal reached

        public void RaiseCoinsUpdated(int totalCoins, int delta)
        {
            OnCoinsUpdated?.Invoke(totalCoins, delta);
        }

        public void RaiseComboUpdated(int currentCombo)
        {
            OnComboUpdated?.Invoke(currentCombo);
        }

        public void RaiseShipCountUpdated(int currentShip, int goal)
        {
            OnShipCountUpdated?.Invoke(currentShip, goal);
        }

        public void RaiseCookingDeepFrozen()
        {
            OnCookingDeepFrozen?.Invoke();
        }
    }
}
