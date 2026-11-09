using CookAndDeliver.Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // Controller for the Main Menu scene handling scene launches, continue runs, and audio toggle
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        public Button playButton;
        public Button soundButton;
        public Button quitButton;

        [Header("Information Labels")]
        public TextMeshProUGUI playButtonText;
        public TextMeshProUGUI progressSummaryText;
        public TextMeshProUGUI soundButtonText;

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundToggleClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        void Start()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            int day = CookingSession.CurrentDay > 0 ? CookingSession.CurrentDay : 1;
            int coins = CookingSession.Coins;

            if (playButtonText != null)
            {
                playButtonText.text = day > 1 ? $"TIẾP TỤC · NGÀY {day}" : "VÀO BẾP MỞ QUÁN";
            }

            if (progressSummaryText != null)
            {
                progressSummaryText.text = $"Tiến trình: Ngày {day}  ·  Ví: {coins} xu";
            }

            if (soundButtonText != null)
            {
                bool isMuted = AudioListener.volume <= 0.01f;
                soundButtonText.text = isMuted ? "ÂM THANH: TẮT" : "ÂM THANH: BẬT";
            }
        }

        void OnPlayClicked()
        {
            LoadSceneHandler.LoadSceneByName("Cooking");
        }

        void OnSoundToggleClicked()
        {
            bool isMuted = AudioListener.volume <= 0.01f;
            AudioListener.volume = isMuted ? 1f : 0f;
            UpdateUI();
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
