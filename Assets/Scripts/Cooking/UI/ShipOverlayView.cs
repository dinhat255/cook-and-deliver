using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View popup khi hoan thanh du 3 hop ship dung TextMeshPro
    public class ShipOverlayView : MonoBehaviour
    {
        [Header("Thanh phan TextMeshPro")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;
        public TextMeshProUGUI deliverButtonText;
        public TextMeshProUGUI continueButtonText;

        [Header("Nut hanh dong")]
        public Button deliverButton;
        public Button continueButton;

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
