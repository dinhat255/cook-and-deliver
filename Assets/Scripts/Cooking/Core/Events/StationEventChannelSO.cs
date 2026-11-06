using System;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Events
{
    // ScriptableObject event channel for heat station events (cooking, perfect, warning, burnt)
    [CreateAssetMenu(fileName = "StationEventChannel", menuName = "Cooking/Events/Station Event Channel", order = 2)]
    public class StationEventChannelSO : ScriptableObject
    {
        public event Action<string, IngredientSO, HeatSlotState> OnSlotStateChanged;
        public event Action<string, Vector3> OnSlotCookPerfect;
        public event Action<string, Vector3, int> OnSlotBurnt;
        public event Action<string, Vector3> OnStationBusy;

        public void RaiseSlotStateChanged(string slotId, IngredientSO ingredient, HeatSlotState state)
        {
            OnSlotStateChanged?.Invoke(slotId, ingredient, state);
        }

        public void RaiseSlotCookPerfect(string slotId, Vector3 worldPos)
        {
            OnSlotCookPerfect?.Invoke(slotId, worldPos);
        }

        public void RaiseSlotBurnt(string slotId, Vector3 worldPos, int penalty)
        {
            OnSlotBurnt?.Invoke(slotId, worldPos, penalty);
        }

        public void RaiseStationBusy(string stationName, Vector3 worldPos)
        {
            OnStationBusy?.Invoke(stationName, worldPos);
        }
    }
}
