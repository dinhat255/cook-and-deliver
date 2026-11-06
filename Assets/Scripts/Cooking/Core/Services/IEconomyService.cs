using CookAndDeliver.Cooking.Data;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Interface for managing economy, tip calculations, combo multipliers, and ship goals
    public interface IEconomyService : IService
    {
        int Coins { get; }
        int Combo { get; }
        int CompletedShipCount { get; }

        int CalculateEarnings(RecipeSO recipe, float patienceRatio, out int tip, out float comboBonus);
        void AddEarnings(int amount);
        void DeductCoins(int penalty);
        void ResetCombo();
        bool RecordShipOrderCompleted(int goal);
        void ResetShipProgress();
    }
}
