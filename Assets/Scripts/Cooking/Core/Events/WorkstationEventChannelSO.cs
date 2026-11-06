using System;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Events
{
    // ScriptableObject event channel for workstation cutting board assembly events
    [CreateAssetMenu(fileName = "WorkstationEventChannel", menuName = "Cooking/Events/Workstation Event Channel", order = 3)]
    public class WorkstationEventChannelSO : ScriptableObject
    {
        public event Action<bool, IngredientSO, int, int> OnIngredientAdded; // isDeliveryBoard, ingredient, current, required
        public event Action<bool, RecipeSO, Vector3> OnDishCompleted;       // isDeliveryBoard, recipe, dishPos
        public event Action<bool> OnBoardCleared;                            // isDeliveryBoard
        public event Action<bool, Vector3> OnBoardActionTriggered;           // isDeliveryBoard, buttonPos

        public void RaiseIngredientAdded(bool isDeliveryBoard, IngredientSO ingredient, int currentCount, int requiredCount)
        {
            OnIngredientAdded?.Invoke(isDeliveryBoard, ingredient, currentCount, requiredCount);
        }

        public void RaiseDishCompleted(bool isDeliveryBoard, RecipeSO recipe, Vector3 dishPos)
        {
            OnDishCompleted?.Invoke(isDeliveryBoard, recipe, dishPos);
        }

        public void RaiseBoardCleared(bool isDeliveryBoard)
        {
            OnBoardCleared?.Invoke(isDeliveryBoard);
        }

        public void RaiseBoardActionTriggered(bool isDeliveryBoard, Vector3 buttonPos)
        {
            OnBoardActionTriggered?.Invoke(isDeliveryBoard, buttonPos);
        }
    }
}
