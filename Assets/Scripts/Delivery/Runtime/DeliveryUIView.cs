using System;
using System.Collections.Generic;
using CookAndDeliver.Delivery.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookAndDeliver.Delivery
{
    public sealed class DeliveryUIView : MonoBehaviour
    {
        [Serializable]
        public struct DishSpriteEntry
        {
            public string recipeId;
            public string dishName;
            public Sprite sprite;
        }

        public TMP_Text score, orderCount, integrity, orderName, direction, routeCaption, hint, countdownText;
        public TMP_Text readyDescription, readyRecord, resultsTitle, resultsScore, resultsSummary, resultRecord, toast;
        public Image integrityFill, progressFill;
        public Image orderIcon, directionIcon;
        public Sprite[] foodSprites;
        [Header("Clean Dish Catalog & Ready Slots")]
        public DishSpriteEntry[] dishCatalog;
        public GameObject[] readyFoodSlots;
        public Image[] readyFoodIcons;
        public Sprite arrowLeft, arrowRight;
        public GameObject readyPanel, pausePanel, resultsPanel, countdownPanel, toastPanel;
        public Button startButton, pauseButton, resumeButton, restartButton, pauseHomeButton, resultReplayButton, resultHomeButton;
        public RectTransform resultContent;
        public ScrollRect resultScroll;
        public DeliveryResultRow resultRowPrefab;
        public float warningSeconds = 3f;
        private readonly List<DeliveryResultRow> rows = new List<DeliveryResultRow>();
        private IReadOnlyList<DeliveryOrder> readyPreviewOrders;
        private float toastTime;
        private int readyOrderCount = 3;

        // Called every rendered frame, independently of the 10 Hz text/score refresh.
        public void AnimateDeliveryWarning(DeliverySimulation sim, float pulse)
        {
            if (sim.State == DeliveryState.Paused) return;
            var warning = sim.State == DeliveryState.Running && sim.DistanceToNextStop <= sim.BaseSpeed * warningSeconds;
            directionIcon.gameObject.SetActive(warning);
            directionIcon.rectTransform.localScale = Vector3.one * (warning ? Mathf.Lerp(.92f, 1.12f, pulse) : 1);
            var ink = new Color32(31, 20, 5, 255);
            directionIcon.color = warning ? Color.Lerp(ink, new Color(1, .4f, .02f), pulse) : ink;
            if (!warning) return;
            directionIcon.sprite = sim.NextStopLane == 0 ? arrowLeft : arrowRight;
            var color = directionIcon.color;
            color.a = Mathf.Lerp(.35f, 1, pulse);
            directionIcon.color = color;
        }

        private void Start() { FocusVisiblePanel(); }

        private void FocusVisiblePanel()
        {
            if (EventSystem.current == null) return;
            var target = readyPanel.activeSelf ? startButton : pausePanel.activeSelf ? resumeButton : resultsPanel.activeSelf ? resultReplayButton : null;
            EventSystem.current.SetSelectedGameObject(target != null ? target.gameObject : null);
        }

        public void SetState(DeliveryState state)
        {
            readyPanel.SetActive(state == DeliveryState.Ready);
            pausePanel.SetActive(state == DeliveryState.Paused);
            resultsPanel.SetActive(state == DeliveryState.Results);
            countdownPanel.SetActive(state == DeliveryState.Countdown);
            pauseButton.interactable = state == DeliveryState.Running || state == DeliveryState.Countdown;
            if (state != DeliveryState.Running) { toastPanel.SetActive(false); toastTime = 0; }
            if (state == DeliveryState.Paused) UpdateReturnButtonLabel(pauseHomeButton);
            FocusVisiblePanel();
        }

        public void SetReady(DeliveryConfigSO config, DeliveryProgress data)
        {
            var externalPreview = DeliveryController.ExternalPreviewOrdersProvider?.Invoke();
            if (externalPreview != null && externalPreview.Count > 0)
            {
                readyPreviewOrders = externalPreview;
            }
            else if (config != null)
            {
                readyPreviewOrders = config.CreateRequest(0).Orders;
            }
            else
            {
                readyPreviewOrders = null;
            }

            readyOrderCount = readyPreviewOrders != null && readyPreviewOrders.Count > 0
                ? readyPreviewOrders.Count
                : (DeliveryController.PendingOrderCount > 0 ? DeliveryController.PendingOrderCount : (config != null ? config.settings.OrderCount : 3));

            readyDescription.text = readyOrderCount + " đơn";
            readyRecord.text = "Kỷ lục " + data.BestFor(readyOrderCount);
            UpdateReadyFoodSlots();
            UpdateReturnButtonLabel(pauseHomeButton);
            UpdateReturnButtonLabel(resultHomeButton);
        }

        public void Render(DeliverySimulation sim)
        {
            score.text = sim.Score.ToString();
            var total = sim.Request?.Orders.Count ?? readyOrderCount;
            orderCount.text = sim.DeliveredCount + "/" + total;
            integrity.text = sim.Integrity + "%";
            integrityFill.fillAmount = sim.Integrity / 100f;
            integrityFill.color = sim.Integrity > 50 ? new Color32(56, 202, 62, 255) : new Color32(222, 122, 82, 255);
            progressFill.fillAmount = sim.State == DeliveryState.Ready ? 0f : sim.CurrentOrderProgress;
            countdownText.text = Mathf.Max(1, Mathf.CeilToInt(sim.CountdownRemaining)).ToString();
            hint.gameObject.SetActive(sim.State == DeliveryState.Running && sim.Distance < 400);
            if (sim.Request == null || sim.State == DeliveryState.Ready)
            {
                if (readyPreviewOrders != null && readyPreviewOrders.Count > 0)
                {
                    orderName.text = readyPreviewOrders[0].Name;
                    orderIcon.sprite = SpriteForOrder(readyPreviewOrders[0]);
                }
                else
                {
                    orderName.text = "Giao ship";
                    orderIcon.sprite = FallbackFoodSprite();
                }
                direction.text = "";
                routeCaption.text = readyOrderCount + " đơn";
                directionIcon.gameObject.SetActive(false);
                orderIcon.color = Color.white;
                return;
            }
            var index = Mathf.Min(sim.CurrentOrderIndex, total - 1);
            var currentOrder = sim.Request.Orders[index];
            orderName.text = currentOrder.Name;
            routeCaption.text = (index + 1) + "/" + total;
            orderIcon.sprite = SpriteForOrder(currentOrder);
            orderIcon.color = Color.white;
            var warning = sim.State == DeliveryState.Running && sim.DistanceToNextStop <= sim.BaseSpeed * warningSeconds;
            direction.text = warning ? "Giao" : Mathf.Max(0, Mathf.CeilToInt(sim.DistanceToNextStop / 10)) + " m";
            direction.color = warning ? new Color32(185, 72, 12, 255) : new Color32(56, 58, 66, 255);
            directionIcon.gameObject.SetActive(warning);
            directionIcon.sprite = sim.NextStopLane == 0 ? arrowLeft : arrowRight;
            hint.text = "Vuốt để đổi làn";
        }

        private void UpdateReadyFoodSlots()
        {
            if (readyFoodIcons == null || readyFoodIcons.Length == 0) return;

            for (int i = 0; i < readyFoodIcons.Length; i++)
            {
                bool hasItem = readyPreviewOrders != null && i < readyPreviewOrders.Count;
                if (readyFoodSlots != null && i < readyFoodSlots.Length && readyFoodSlots[i] != null)
                {
                    readyFoodSlots[i].SetActive(hasItem);
                }
                if (hasItem && readyFoodIcons[i] != null)
                {
                    readyFoodIcons[i].sprite = SpriteForOrder(readyPreviewOrders[i]);
                    readyFoodIcons[i].color = Color.white;
                }
            }
        }

        private void UpdateReturnButtonLabel(Button button)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;

            if (DeliveryController.OnExternalReturnRequested != null && !string.IsNullOrEmpty(DeliveryController.ExternalReturnButtonLabel))
            {
                label.text = DeliveryController.ExternalReturnButtonLabel;
            }
            else
            {
                label.text = "Về đầu";
            }
        }

        private Sprite SpriteForOrder(DeliveryOrder order)
        {
            if (order == null) return FallbackFoodSprite();

            if (dishCatalog != null && dishCatalog.Length > 0)
            {
                // 1. Exact or prefix match by Recipe ID (CookingSession formats IDs as "{recipeId}_{index}_{hash}")
                int bestIdLen = -1;
                Sprite bestById = null;
                for (int i = 0; i < dishCatalog.Length; i++)
                {
                    var entry = dishCatalog[i];
                    if (entry.sprite == null || string.IsNullOrEmpty(entry.recipeId)) continue;
                    if (string.Equals(order.Id, entry.recipeId, StringComparison.OrdinalIgnoreCase) ||
                        order.Id.StartsWith(entry.recipeId + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (entry.recipeId.Length > bestIdLen)
                        {
                            bestIdLen = entry.recipeId.Length;
                            bestById = entry.sprite;
                        }
                    }
                }
                if (bestById != null) return bestById;

                // 2. Exact match by dish name
                for (int i = 0; i < dishCatalog.Length; i++)
                {
                    var entry = dishCatalog[i];
                    if (entry.sprite == null || string.IsNullOrEmpty(entry.dishName)) continue;
                    if (string.Equals(order.Name, entry.dishName, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.sprite;
                    }
                }

                // 3. Longest partial match by dish name (e.g., "Mì Kim Chi" before "Mì Xào")
                string lower = order.Name.ToLowerInvariant();
                int bestNameLen = -1;
                Sprite bestByName = null;
                for (int i = 0; i < dishCatalog.Length; i++)
                {
                    var entry = dishCatalog[i];
                    if (entry.sprite == null || string.IsNullOrEmpty(entry.dishName)) continue;
                    string catalogLower = entry.dishName.ToLowerInvariant();
                    if (lower.Contains(catalogLower) || catalogLower.Contains(lower))
                    {
                        if (catalogLower.Length > bestNameLen)
                        {
                            bestNameLen = catalogLower.Length;
                            bestByName = entry.sprite;
                        }
                    }
                }
                if (bestByName != null) return bestByName;

                // 4. Keyword match onto clean catalog entries
                string keywordId = lower.Contains("bánh mì") ? "banh_mi_thit_nuong"
                    : lower.Contains("phở") ? "pho_bo_tai"
                    : lower.Contains("tteok") ? "tteokbokki_cay"
                    : lower.Contains("mì") ? "mi_xao_kimchi_thit_heo"
                    : null;
                if (keywordId != null)
                {
                    for (int i = 0; i < dishCatalog.Length; i++)
                    {
                        if (dishCatalog[i].sprite != null && string.Equals(dishCatalog[i].recipeId, keywordId, StringComparison.OrdinalIgnoreCase))
                        {
                            return dishCatalog[i].sprite;
                        }
                    }
                }

                for (int i = 0; i < dishCatalog.Length; i++)
                {
                    if (dishCatalog[i].sprite != null) return dishCatalog[i].sprite;
                }
            }

            return FallbackFoodSprite();
        }

        private Sprite FallbackFoodSprite()
        {
            if (foodSprites != null && foodSprites.Length > 0)
            {
                for (int i = 0; i < foodSprites.Length; i++)
                {
                    if (foodSprites[i] != null) return foodSprites[i];
                }
            }
            return orderIcon != null ? orderIcon.sprite : null;
        }

        public void Notify(string message)
        {
            toast.text = message;
            toastPanel.SetActive(true);
            toastTime = 2f;
        }
        public void Tick(float delta)
        {
            if (toastTime <= 0) return;
            toastTime -= delta;
            if (toastTime <= 0) toastPanel.SetActive(false);
        }

        public void ShowResult(DeliveryRunResult result, DeliveryProgress progress, bool saved)
        {
            resultsTitle.text = result.Reason == EndReason.CargoLost ? "HỎNG HÀNG" : "HOÀN TẤT";
            resultsScore.text = result.Score.ToString();
            resultsSummary.text = result.DeliveredCount + "/" + result.OrderCount + " đơn";
            resultRecord.text = saved ? "Kỷ lục " + progress.BestFor(result.OrderCount) : "Chưa lưu được";

            UpdateReturnButtonLabel(resultHomeButton);
            UpdateReturnButtonLabel(pauseHomeButton);

            for (var i = 0; i < result.Orders.Count; i++)
            {
                if (i >= rows.Count) rows.Add(Instantiate(resultRowPrefab, resultContent));
                rows[i].gameObject.SetActive(true);
                rows[i].Set(result.Orders[i], i);
            }
            for (var i = result.Orders.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            resultScroll.verticalNormalizedPosition = 1;
        }
    }
}
