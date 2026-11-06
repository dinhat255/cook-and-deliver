using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;

namespace CookAndDeliver.Cooking.Core.Models
{
    // Pure C# domain model tracking the state of an assembly workstation cutting board
    public class BoardModel
    {
        public bool IsDeliveryBoard { get; }
        public OrderModel CurrentOrder { get; private set; }
        public Dictionary<string, int> HaveCounts { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> PendingCounts { get; } = new Dictionary<string, int>();
        public bool IsComplete { get; private set; }
        public int Version { get; private set; }

        public BoardModel(bool isDeliveryBoard)
        {
            IsDeliveryBoard = isDeliveryBoard;
        }

        public void BindOrder(OrderModel order)
        {
            CurrentOrder = order;
            ClearIngredients();
        }

        public void ClearIngredients()
        {
            HaveCounts.Clear();
            PendingCounts.Clear();
            IsComplete = false;
            Version++;
        }

        public void ResetBoard()
        {
            CurrentOrder = null;
            ClearIngredients();
        }

        public bool CanAccept(IngredientSO ingredient)
        {
            if (CurrentOrder == null || CurrentOrder.Recipe == null || IsComplete || ingredient == null)
                return false;

            return CurrentOrder.Recipe.CanAccept(ingredient, HaveCounts, PendingCounts);
        }

        public void MarkPending(IngredientSO ingredient)
        {
            if (ingredient == null) return;
            PendingCounts.TryGetValue(ingredient.id, out int count);
            PendingCounts[ingredient.id] = count + 1;
        }

        public bool AddIngredient(IngredientSO ingredient, int versionSnapshot)
        {
            if (versionSnapshot != Version || ingredient == null) return false;

            if (PendingCounts.TryGetValue(ingredient.id, out int pending) && pending > 0)
            {
                PendingCounts[ingredient.id] = pending - 1;
            }

            HaveCounts.TryGetValue(ingredient.id, out int current);
            HaveCounts[ingredient.id] = current + 1;

            if (CurrentOrder != null && CurrentOrder.Recipe.IsComplete(HaveCounts))
            {
                IsComplete = true;
                return true;
            }

            return false;
        }

        public int GetCount(string ingredientId)
        {
            HaveCounts.TryGetValue(ingredientId, out int count);
            return count;
        }
    }
}
