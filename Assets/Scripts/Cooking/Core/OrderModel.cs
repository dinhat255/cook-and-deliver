using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core
{
    // Model dai dien cho mot phieu order dang active tren ray KDS
    public class OrderModel
    {
        public RecipeSO Recipe { get; private set; }
        public bool IsShipOrder { get; private set; }
        public float CurrentPatience { get; set; }
        public float MaxPatience { get; private set; }

        public float PatienceRatio => Mathf.Clamp01(CurrentPatience / MaxPatience);
        public bool IsExpired => CurrentPatience <= 0f;

        public OrderModel(RecipeSO recipe, bool isShipOrder, float maxPatience)
        {
            Recipe = recipe;
            IsShipOrder = isShipOrder;
            MaxPatience = maxPatience;
            CurrentPatience = maxPatience;
        }

        public void Tick(float deltaTime)
        {
            CurrentPatience = Mathf.Max(0f, CurrentPatience - deltaTime);
        }
    }
}
