using TMPro;
using UnityEngine;

namespace CookAndDeliver.Cooking.UI
{
    // View quan ly HUD tren cung dung TextMeshPro
    public class HudView : MonoBehaviour
    {
        [Header("Thanh phan TextMeshPro")]
        public TextMeshProUGUI coinsText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI shipText;

        public void UpdateCoins(int coins)
        {
            if (coinsText != null)
                coinsText.text = $"{coins:N0} xu";
        }

        public void UpdateCombo(int combo)
        {
            if (comboText != null)
                comboText.text = $"COMBO x{combo}";
        }

        public void UpdateShipCount(int current, int goal)
        {
            if (shipText != null)
                shipText.text = $"SHIP: [{current}/{goal}] HỘP";
        }
    }
}
