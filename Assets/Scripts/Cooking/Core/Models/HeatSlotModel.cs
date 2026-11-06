using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Models
{
    // Pure C# domain model tracking the state and timers of a heating station slot
    public class HeatSlotModel
    {
        public string SlotId { get; }
        public HeatSlotState State { get; private set; } = HeatSlotState.Idle;
        public IngredientSO CurrentIngredient { get; private set; }
        public bool IsReserved { get; set; }
        public float Timer { get; private set; }

        public float CookDuration { get; private set; } = 3f;
        public float WarnDuration { get; private set; } = 6f;
        public float BurnDuration { get; private set; } = 8f;
        public bool CanBurn { get; private set; } = true;

        public bool IsFree => State == HeatSlotState.Idle && !IsReserved;

        public HeatSlotModel(string slotId, float defaultCook = 3f, float defaultWarn = 6f, float defaultBurn = 8f, bool allowBurn = true)
        {
            SlotId = slotId;
            CookDuration = defaultCook;
            WarnDuration = defaultWarn;
            BurnDuration = defaultBurn;
            CanBurn = allowBurn;
        }

        public void PlaceIngredient(IngredientSO ingredient)
        {
            CurrentIngredient = ingredient;
            IsReserved = false;
            State = HeatSlotState.Cooking;
            Timer = 0f;

            if (ingredient != null)
            {
                if (ingredient.cookDuration > 0) CookDuration = ingredient.cookDuration;
                if (ingredient.warnDuration > 0) WarnDuration = ingredient.warnDuration;
                if (ingredient.burnDuration > 0) BurnDuration = ingredient.burnDuration;
                CanBurn = ingredient.canBurn;
            }
        }

        public void Clear()
        {
            State = HeatSlotState.Idle;
            IsReserved = false;
            CurrentIngredient = null;
            Timer = 0f;
        }

        // Returns true if state changed
        public bool Tick(float deltaTime, out HeatSlotState previousState)
        {
            previousState = State;
            if (State == HeatSlotState.Idle || CurrentIngredient == null) return false;

            Timer += deltaTime;

            float activeWarn = CanBurn ? WarnDuration : float.PositiveInfinity;
            float activeBurn = CanBurn ? BurnDuration : float.PositiveInfinity;

            if (Timer >= activeBurn) State = HeatSlotState.Burnt;
            else if (Timer >= activeWarn) State = HeatSlotState.Warning;
            else if (Timer >= CookDuration) State = HeatSlotState.Perfect;
            else State = HeatSlotState.Cooking;

            return previousState != State;
        }
    }
}
