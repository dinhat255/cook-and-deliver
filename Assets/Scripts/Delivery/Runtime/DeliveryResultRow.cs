using CookAndDeliver.Delivery.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Delivery
{
    public sealed class DeliveryResultRow : MonoBehaviour
    {
        public TMP_Text title, details, reward;
        public Image status;
        public Sprite deliveredIcon, failedIcon;
        public void Set(OrderResult order, int index)
        {
            title.text = order.Order.Name;
            var delivered = order.Outcome == OrderOutcome.Delivered;
            details.text = delivered ? order.Integrity + "%" : order.Outcome == OrderOutcome.Missed ? "Lỡ đơn" : "Hỏng";
            reward.text = delivered ? "+" + order.Reward : "—";
            status.color = delivered ? new Color32(75, 164, 119, 255) : new Color32(218, 128, 93, 255);
            status.sprite = delivered ? deliveredIcon : failedIcon;
        }
    }
}
