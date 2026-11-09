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
        public TMP_Text score, orderCount, integrity, orderName, direction, routeCaption, hint, countdownText;
        public TMP_Text readyDescription, readyRecord, resultsTitle, resultsScore, resultsSummary, resultRecord, toast;
        public Image integrityFill, progressFill;
        public Image orderIcon, directionIcon;
        public Sprite[] foodSprites;
        public Sprite arrowLeft, arrowRight;
        public GameObject readyPanel, pausePanel, resultsPanel, countdownPanel, toastPanel;
        public Button startButton, pauseButton, resumeButton, restartButton, pauseHomeButton, resultReplayButton, resultHomeButton;
        public RectTransform resultContent;
        public ScrollRect resultScroll;
        public DeliveryResultRow resultRowPrefab;
        public float warningSeconds = 3f;
        private readonly List<DeliveryResultRow> rows = new List<DeliveryResultRow>();
        private float toastTime;
        private int readyOrderCount = 3;

        // Called every rendered frame, independently of the 10 Hz text/score refresh.
        public void AnimateDeliveryWarning(DeliverySimulation sim, float pulse)
        {
            if (sim.State == DeliveryState.Paused) return;
            var warning = sim.State == DeliveryState.Running && sim.DistanceToNextStop <= sim.BaseSpeed * warningSeconds;
            directionIcon.gameObject.SetActive(warning);
            directionIcon.rectTransform.localScale = Vector3.one * (warning ? Mathf.Lerp(.92f, 1.12f, pulse) : 1);
            var ink = new Color32(36, 29, 26, 255);
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
            FocusVisiblePanel();
        }

        public void SetReady(DeliveryConfigSO config, DeliveryProgress data)
        {
            readyOrderCount = config.settings.OrderCount;
            readyDescription.text = config.settings.OrderCount + " đơn";
            readyRecord.text = "Kỷ lục " + data.BestFor(config.settings.OrderCount);
        }

        public void Render(DeliverySimulation sim)
        {
            score.text = sim.Score.ToString();
            var total = sim.Request?.Orders.Count ?? readyOrderCount;
            orderCount.text = sim.DeliveredCount + "/" + total;
            integrity.text = sim.Integrity + "%";
            integrityFill.fillAmount = sim.Integrity / 100f;
            integrityFill.color = sim.Integrity > 50 ? new Color32(56, 202, 62, 255) : new Color32(222, 122, 82, 255);
            progressFill.fillAmount = sim.CurrentOrderProgress;
            countdownText.text = Mathf.Max(1, Mathf.CeilToInt(sim.CountdownRemaining)).ToString();
            hint.gameObject.SetActive(sim.State == DeliveryState.Running && sim.Distance < 400);
            if (sim.Request == null || sim.State == DeliveryState.Ready)
            {
                orderName.text = "Giao ship";
                direction.text = "";
                routeCaption.text = readyOrderCount + " đơn";
                directionIcon.gameObject.SetActive(false);
                orderIcon.sprite = foodSprites[0];
                orderIcon.color = Color.white;
                return;
            }
            var index = Mathf.Min(sim.CurrentOrderIndex, total - 1);
            orderName.text = sim.Request.Orders[index].Name;
            routeCaption.text = (index + 1) + "/" + total;
            orderIcon.sprite = FoodFor(orderName.text);
            orderIcon.color = orderIcon.sprite == foodSprites[2] ? new Color32(36, 29, 26, 255) : Color.white;
            var warning = sim.State == DeliveryState.Running && sim.DistanceToNextStop <= sim.BaseSpeed * warningSeconds;
            direction.text = warning ? "Giao" : Mathf.Max(0, Mathf.CeilToInt(sim.DistanceToNextStop / 10)) + " m";
            direction.color = warning ? new Color32(185, 72, 12, 255) : new Color32(56, 58, 66, 255);
            directionIcon.gameObject.SetActive(warning);
            directionIcon.sprite = sim.NextStopLane == 0 ? arrowLeft : arrowRight;
            hint.text = "Vuốt để đổi làn";
        }

        private Sprite FoodFor(string name)
        {
            var lower = name.ToLowerInvariant();
            var index = lower.Contains("bánh mì") ? 1 : lower.Contains("trà") ? 2 : lower.Contains("mì") ? 3 : lower.Contains("bánh") ? 4 : lower.Contains("cơm") ? 0 : 2;
            return foodSprites[index % foodSprites.Length];
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
