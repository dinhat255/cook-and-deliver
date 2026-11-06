using System.Collections.Generic;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Core.Models;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.Cooking.UI;
using UnityEngine;

namespace CookAndDeliver.Cooking.Presenters
{
    // Presenter in MVP managing heat stations, slot state machine timers, and views
    public class HeatStationPresenter : MonoBehaviour
    {
        [Header("Views")]
        public HeatSlotView[] grillViews;
        public HeatSlotView ovenView;
        public HeatSlotView panView;
        public RectTransform grillRect, ovenRect, panRect, potRect;

        [Header("Event Channels")]
        public StationEventChannelSO stationEventChannel;

        readonly List<(HeatSlotModel model, HeatSlotView view)> slots = new List<(HeatSlotModel, HeatSlotView)>();
        bool isPaused;

        void Awake()
        {
            InitializeSlots();
        }

        void InitializeSlots()
        {
            slots.Clear();

            // 3 Grill slots
            if (grillViews != null)
            {
                for (int i = 0; i < grillViews.Length; i++)
                {
                    var model = new HeatSlotModel($"Grill_{i}", 3f, 6f, 8f, true);
                    slots.Add((model, grillViews[i]));
                }
            }

            // Oven slot
            if (ovenView != null)
            {
                var model = new HeatSlotModel("Oven", 2f, 0f, 0f, false);
                slots.Add((model, ovenView));
            }

            // Pan slot
            if (panView != null)
            {
                var model = new HeatSlotModel("Pan", 2.5f, 4.5f, 6f, true);
                slots.Add((model, panView));
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void Tick(float deltaTime)
        {
            if (isPaused) return;

            foreach (var (model, view) in slots)
            {
                if (view != null)
                {
                    view.Tick(deltaTime);
                }
            }
        }

        public HeatSlotView GetFreeGrillSlot()
        {
            if (grillViews == null) return null;
            foreach (var view in grillViews)
            {
                if (view != null && view.IsFree) return view;
            }
            return null;
        }

        public HeatSlotView GetOvenSlot() => (ovenView != null && ovenView.IsFree) ? ovenView : null;
        public HeatSlotView GetPanSlot() => (panView != null && panView.IsFree) ? panView : null;

        public void PlaceIngredient(HeatSlotView slotView, IngredientSO ingredient)
        {
            if (slotView == null || ingredient == null) return;
            slotView.PlaceIngredient(ingredient);
            stationEventChannel?.RaiseSlotStateChanged(slotView.name, ingredient, HeatSlotState.Cooking);
        }

        public void ClearSlot(HeatSlotView slotView)
        {
            if (slotView == null) return;
            slotView.ClearSlot();
            stationEventChannel?.RaiseSlotStateChanged(slotView.name, null, HeatSlotState.Idle);
        }
    }
}
