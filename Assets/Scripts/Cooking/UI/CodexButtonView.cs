using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View attached directly to the CodexButton on HUD for launching the Culinary Codex popup
    public class CodexButtonView : MonoBehaviour
    {
        [Header("Popup Reference")]
        public CulinaryCodexPopupView codexPopup;

        [Header("Button Component")]
        public Button clickButton;

        void Awake()
        {
            if (clickButton == null)
            {
                clickButton = GetComponent<Button>();
            }

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnClick);
            }
        }

        public void OnClick()
        {
            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.DOKill();
                rt.localScale = Vector3.one;
                rt.DOPunchScale(Vector3.one * 0.15f, 0.18f, 5, 0.5f);
            }

            if (codexPopup != null)
            {
                codexPopup.Open();
            }
            else
            {
                var popup = FindFirstObjectByType<CulinaryCodexPopupView>(FindObjectsInactive.Include);
                if (popup != null)
                {
                    popup.Open();
                }
            }
        }
    }
}
