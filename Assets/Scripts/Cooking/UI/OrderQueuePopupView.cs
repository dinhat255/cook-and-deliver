using System.Collections.Generic;
using CookAndDeliver.Cooking.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View controlling the upcoming orders queue preview popup
    public class OrderQueuePopupView : MonoBehaviour
    {
        [Header("Root & Controls")]
        public GameObject panelRoot;
        public Button closeButton;
        public Button backdropButton;

        [Header("Badge / Launcher")]
        public Button openBadgeButton;
        public TextMeshProUGUI badgeCountText;

        [Header("Queue Content")]
        public Transform itemsContainer;
        public GameObject queueItemPrefab;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public System.Action<bool> OnOpenStateChanged;

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (backdropButton != null) backdropButton.gameObject.SetActive(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);

            if (openBadgeButton != null)
                openBadgeButton.onClick.AddListener(Open);
        }

        public void UpdateBadgeCount(int count)
        {
            if (badgeCountText != null)
            {
                badgeCountText.text = count > 0 ? $"+{count}" : "0";
            }

            if (openBadgeButton != null)
            {
                // Pulse or highlight if orders are waiting
                openBadgeButton.interactable = count > 0;
            }
        }

        public void Open()
        {
            if (backdropButton != null)
            {
                backdropButton.gameObject.SetActive(true);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                OnOpenStateChanged?.Invoke(true);
            }
        }

        public void Close()
        {
            if (backdropButton != null)
            {
                backdropButton.gameObject.SetActive(false);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                OnOpenStateChanged?.Invoke(false);
            }
        }

        public void PopulateQueue(IReadOnlyList<OrderModel> orders)
        {
            if (itemsContainer == null) return;

            // Clear old items
            for (int i = itemsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(itemsContainer.GetChild(i).gameObject);
            }

            if (orders == null || orders.Count == 0) return;

            // Instantiate row items
            foreach (var order in orders)
            {
                if (order == null || order.Recipe == null) continue;
                CreateQueueRow(order);
            }
        }

        void CreateQueueRow(OrderModel order)
        {
            if (queueItemPrefab != null)
            {
                var rowGo = Instantiate(queueItemPrefab, itemsContainer);
                rowGo.SetActive(true);
                var text = rowGo.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    string kind = order.IsShipOrder ? "<color=#FF8C32>[SHIP]</color>" : "<color=#EAEAEA>[TẠI BÀN]</color>";
                    text.text = $"{kind} {order.Recipe.recipeName}";
                }
            }
        }
    }
}
