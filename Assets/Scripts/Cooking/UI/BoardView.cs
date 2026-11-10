using System.Collections.Generic;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View managing an assembly workstation cutting board using TextMeshPro
    public class BoardView : MonoBehaviour
    {
        [Header("Board Configuration")]
        public bool isDeliveryBoard;

        [Header("UI References")]
        public Image outlineHighlight;
        public Image finishedDishImage;
        public TextMeshProUGUI boardTitleText;
        public GameObject checklistContainer;
        public ChecklistSlotView[] checklistSlots;
        public Button selectBoardButton;
        public Button actionButton;
        public Image actionButtonBackground;
        public TextMeshProUGUI actionButtonText;
        public RectTransform actionButtonRect;

        public OrderModel CurrentOrder { get; private set; }
        public Dictionary<string, int> HaveCounts { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> PendingCounts { get; } = new Dictionary<string, int>();
        public bool IsComplete { get; private set; }
        public int BoardVersion { get; private set; }

        public RectTransform RectTransform => (RectTransform)transform;
        public Sprite FinishedDishSprite => finishedDishImage != null ? finishedDishImage.sprite : null;
        public Vector3 FinishedDishWorldPos => finishedDishImage != null ? finishedDishImage.transform.position : transform.position;
        public Vector3 CenterWorldPos => checklistContainer != null ? checklistContainer.transform.position : transform.position;

        public Vector3 GetSlotWorldPos(IngredientSO ingredient)
        {
            if (ingredient != null && checklistSlots != null)
            {
                foreach (var slot in checklistSlots)
                {
                    if (slot != null && slot.gameObject.activeSelf && slot.BoundIngredient != null && slot.BoundIngredient.id == ingredient.id)
                    {
                        return slot.transform.position;
                    }
                }
            }
            return CenterWorldPos;
        }

        void Awake()
        {
            SetHighlight(false);
            if (finishedDishImage != null) finishedDishImage.enabled = false;
            RefreshUI();
        }

        public void SetHighlight(bool isActive)
        {
            if (outlineHighlight != null)
                outlineHighlight.color = new Color(1f, 0.85f, 0.2f, isActive ? 0.95f : 0f);
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

        public bool AddIngredient(IngredientSO ingredient, int version)
        {
            if (version != BoardVersion || ingredient == null) return false;

            // Remove from pending
            if (PendingCounts.TryGetValue(ingredient.id, out int pending) && pending > 0)
            {
                PendingCounts[ingredient.id] = pending - 1;
            }

            // Add to have
            HaveCounts.TryGetValue(ingredient.id, out int current);
            HaveCounts[ingredient.id] = current + 1;

            RefreshUI();

            // Punch scale visual feedback on the corresponding slot
            var slot = GetSlotForIngredient(ingredient);
            if (slot != null)
            {
                slot.transform.DOKill();
                slot.transform.localScale = Vector3.one;
                slot.transform.DOPunchScale(Vector3.one * 0.4f, 0.25f, 6, 0.6f);
            }

            // Check if all requirements are fulfilled
            if (CurrentOrder != null && CurrentOrder.Recipe.IsComplete(HaveCounts))
            {
                IsComplete = true;
                if (finishedDishImage != null)
                {
                    finishedDishImage.sprite = CurrentOrder.Recipe.finishedDishSprite;
                    finishedDishImage.enabled = true;
                    finishedDishImage.transform.localScale = Vector3.one;
                    finishedDishImage.transform.DOKill();
                    finishedDishImage.transform.DOPunchScale(Vector3.one * 0.5f, 0.4f, 6, 0.6f);
                }
                if (checklistContainer != null) checklistContainer.SetActive(false);
                RefreshUI();
                return true;
            }

            return false;
        }

        public void ClearBoard()
        {
            HaveCounts.Clear();
            PendingCounts.Clear();
            IsComplete = false;
            BoardVersion++;
            if (finishedDishImage != null) finishedDishImage.enabled = false;
            if (checklistContainer != null) checklistContainer.SetActive(true);
            RefreshUI();
        }

        public void ResetBoard()
        {
            CurrentOrder = null;
            ClearBoard();
            if (checklistSlots != null)
            {
                foreach (var slot in checklistSlots) if (slot != null) slot.Bind(null, 0);
            }
            RefreshUI();
        }

        public void BindOrder(OrderModel order)
        {
            CurrentOrder = order;
            ClearBoard();

            if (order != null && order.Recipe != null && checklistSlots != null)
            {
                var reqs = order.Recipe.requirements;
                for (int i = 0; i < checklistSlots.Length; i++)
                {
                    if (checklistSlots[i] == null) continue;
                    if (i < reqs.Count && reqs[i] != null)
                    {
                        checklistSlots[i].Bind(reqs[i].ingredient, reqs[i].requiredAmount);
                    }
                    else
                    {
                        checklistSlots[i].Bind(null, 0);
                    }
                }
            }

            RefreshUI();
        }

        ChecklistSlotView GetSlotForIngredient(IngredientSO ingredient)
        {
            if (checklistSlots == null || ingredient == null) return null;
            foreach (var slot in checklistSlots)
            {
                if (slot != null && slot.BoundIngredient != null && slot.BoundIngredient.id == ingredient.id)
                    return slot;
            }
            return null;
        }

        void RefreshUI()
        {
            bool isShip = CurrentOrder != null ? CurrentOrder.IsShipOrder : isDeliveryBoard;
            string kind = isShip ? "GIAO SHIP" : "TẠI BÀN";
            if (boardTitleText != null)
            {
                boardTitleText.text = CurrentOrder == null
                    ? $"{kind}  |  chờ đơn..."
                    : $"{kind}  |  {CurrentOrder.Recipe.recipeName}";
            }

            if (checklistSlots != null)
            {
                foreach (var slot in checklistSlots)
                {
                    if (slot != null && slot.BoundIngredient != null)
                    {
                        HaveCounts.TryGetValue(slot.BoundIngredient.id, out int count);
                        slot.UpdateProgress(count);
                    }
                }
            }

            if (actionButtonBackground != null)
            {
                actionButtonBackground.color = IsComplete
                    ? (isShip ? new Color(1f, 0.55f, 0.1f) : new Color(0.2f, 0.75f, 0.3f))
                    : new Color(0.35f, 0.33f, 0.32f);
            }

            if (actionButtonText != null)
            {
                actionButtonText.text = isShip ? "ĐÓNG HỘP" : "RA MÓN";
                actionButtonText.color = IsComplete ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }
}
