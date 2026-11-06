using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IAudioService integrating with AudioManager and AudioClipSO assets
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("AudioClipSO Assets (Data-Driven Audio)")]
        public AudioClipSO tapClipSO;
        public AudioClipSO sizzleClipSO;
        public AudioClipSO dingClipSO;
        public AudioClipSO burntClipSO;
        public AudioClipSO trashClipSO;
        public AudioClipSO coinClipSO;
        public AudioClipSO packClipSO;

        [Header("Background Music")]
        public AudioClipSO defaultBgmClipSO;

        [Header("Fallback AudioSource (if AudioManager is uninitialized)")]
        public AudioSource fallbackAudioSource;

        void Awake()
        {
            if (fallbackAudioSource == null)
            {
                fallbackAudioSource = GetComponent<AudioSource>();
            }
            CookingServiceLocator.Register<IAudioService>(this);
        }

        void Start()
        {
            if (defaultBgmClipSO != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoop(defaultBgmClipSO);
                AudioManager.Instance.FadeIn(1.5f);
            }
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IAudioService>();
            if (defaultBgmClipSO != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoop(defaultBgmClipSO);
            }
        }

        public void PlaySound(CookingSound sound)
        {
            AudioClipSO clipSO = sound switch
            {
                CookingSound.Tap => tapClipSO,
                CookingSound.Sizzle => sizzleClipSO,
                CookingSound.Ding => dingClipSO,
                CookingSound.Burnt => burntClipSO,
                CookingSound.Trash => trashClipSO,
                CookingSound.Coin => coinClipSO,
                CookingSound.Pack => packClipSO,
                _ => null
            };

            if (clipSO != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOneShot(clipSO);
                return;
            }

            // Fallback playback if AudioManager or clipSO is missing
            if (clipSO != null && fallbackAudioSource != null)
            {
                var clip = clipSO.GetAudioClip();
                if (clip != null)
                {
                    fallbackAudioSource.PlayOneShot(clip, clipSO.Volume);
                }
            }
        }

        public void PlayBGM(AudioClipSO bgmClip)
        {
            if (bgmClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoop(bgmClip);
            }
        }

        public void StopBGM(AudioClipSO bgmClip)
        {
            if (bgmClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoop(bgmClip);
            }
        }
    }
}
