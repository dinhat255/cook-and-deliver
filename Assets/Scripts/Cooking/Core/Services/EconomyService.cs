using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IEconomyService managing coin transactions, combos, and ship delivery progress
    public class EconomyService : MonoBehaviour, IEconomyService
    {
        [Header("Configuration")]
        public CookingConfigSO config;

        [Header("Event Channels")]
        public EconomyEventChannelSO economyEventChannel;

        public int Coins => CookingSession.Coins;
        public int Combo => CookingSession.Combo;
        public int CompletedShipCount { get; private set; }

        void Awake()
        {
            CookingServiceLocator.Register<IEconomyService>(this);
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IEconomyService>();
        }

        public int CalculateEarnings(RecipeSO recipe, float patienceRatio, out int tip, out float comboBonus)
        {
            if (recipe == null)
            {
                tip = 0;
                comboBonus = 1f;
                return 0;
            }

            float tipThreshold = config != null ? config.tipThresholdRatio : 0.5f;
            float tipPercent = config != null ? config.tipPercent : 0.2f;
            tip = patienceRatio > tipThreshold ? Mathf.RoundToInt(recipe.basePrice * tipPercent) : 0;

            int currentCombo = CookingSession.Combo;
            float stepBonus = config != null ? config.comboMultiplierPerStep : 0.1f;
            comboBonus = 1f + stepBonus * Mathf.Min(currentCombo, 10);

            return Mathf.RoundToInt((recipe.basePrice + tip) * comboBonus);
        }

        public void AddEarnings(int amount)
        {
            CookingSession.Combo++;
            CookingSession.Coins += amount;
            CookingSession.CompletedOrdersCount++;

            GameProgressStore.SyncFromSession();

            economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, amount);
            economyEventChannel?.RaiseComboUpdated(CookingSession.Combo);
        }

        public void DeductCoins(int penalty)
        {
            CookingSession.Coins = Mathf.Max(0, CookingSession.Coins - penalty);
            GameProgressStore.SyncFromSession();
            economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, -penalty);
        }

        public void ResetCombo()
        {
            CookingSession.Combo = 0;
            economyEventChannel?.RaiseComboUpdated(0);
        }

        public bool RecordShipOrderCompleted(int goal)
        {
            CompletedShipCount++;
            economyEventChannel?.RaiseShipCountUpdated(CompletedShipCount, goal);

            if (CompletedShipCount >= goal)
            {
                economyEventChannel?.RaiseCookingDeepFrozen();
                return true;
            }
            return false;
        }

        public void ResetShipProgress()
        {
            CompletedShipCount = 0;
            int goal = config != null ? config.shipGoal : 3;
            economyEventChannel?.RaiseShipCountUpdated(CompletedShipCount, goal);
        }
    }
}
