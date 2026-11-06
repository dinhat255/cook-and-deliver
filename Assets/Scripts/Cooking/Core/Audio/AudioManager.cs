using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum AudioType
    {
        None,
        SFX,
        Music
    }

    private Dictionary<int, AudioType> _mixerGroupNameMap = new Dictionary<int, AudioType>
    {
        {"SFX".GetHashCode(), AudioType.SFX},
        {"Music".GetHashCode(), AudioType.Music}
    };

    private Dictionary<AudioType, List<AudioSource>> _audioSourceMap = new Dictionary<AudioType, List<AudioSource>>();
    [SerializeField]private bool _isFadingOut = false;

    [Header("Snapshots")]
    [SerializeField] private AudioMixerSnapshot _defaultSnapshot;
    [SerializeField] private AudioMixerSnapshot _fadeoutSnapshot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public void RegisterAudioSource(AudioSource audioSource)
    {
        if (audioSource == null || !audioSource.transform.IsChildOf(transform)) return;

        AudioType audioType = GetAudioType(audioSource);
        if (!_audioSourceMap.TryGetValue(audioType, out var list))
        {
            list = new List<AudioSource>();
            _audioSourceMap[audioType] = list;
        }

        if (!list.Contains(audioSource))
        {
            list.Add(audioSource);
        }
    }

    public void UnregisterAudioSource(AudioSource audioSource)
    {
        if (audioSource == null || !audioSource.transform.IsChildOf(transform)) return;

        AudioType audioType = GetAudioType(audioSource);
        if (_audioSourceMap.TryGetValue(audioType, out var list))
        {
            list.Remove(audioSource);
        }
    }

    public AudioSource GetAvailableAudioSource(AudioType audioType)
    {
        if (!_audioSourceMap.TryGetValue(audioType, out var list) || list.Count == 0)
        {
            Debug.LogError($"AudioSource for {audioType} is not registered in the AudioManager.");
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].isActiveAndEnabled && !list[i].loop)
            {
                return list[i];
            }
        }

        AudioSource template = list[0];
        AudioSource newSource = Instantiate(template, transform);
        newSource.name = $"{template.name}_{list.Count}";
        newSource.Stop();
        newSource.clip = null;
        newSource.loop = false;

        if (!list.Contains(newSource))
        {
            list.Add(newSource);
        }

        return newSource;
    }

    public AudioSource GetAudioSource(AudioType audioType)
    {
        return GetAvailableAudioSource(audioType);
    }

    public AudioType GetAudioType(AudioSource audioSource)
    {
        AudioMixerGroup audioMixerGroup = audioSource.outputAudioMixerGroup;
        if (audioMixerGroup != null && _mixerGroupNameMap.TryGetValue(audioMixerGroup.name.GetHashCode(), out AudioType audioType))
        {
            return audioType;
        }
        else
        {
            Debug.LogError($"AudioMixerGroup {(audioMixerGroup != null ? audioMixerGroup.name : "null")} is not registered in the AudioManager.");
            return AudioType.None;
        }
    }

    public void PlayOneShot(AudioClipSO audioClipSO)
    {
        AudioSource audioSource = GetAvailableAudioSource(audioClipSO.audioType);
        audioSource?.PlayOneShot(audioClipSO.GetAudioClip(), audioClipSO.Volume);
    }

    public async void PlayLoop(AudioClipSO audioClipSO)
    {
        if (_isFadingOut)
        {
            await UniTask.WaitWhile(() => _isFadingOut);
        }

        AudioSource audioSource = GetAvailableAudioSource(audioClipSO.audioType);
        if (audioSource != null)
        {
            audioSource.clip = audioClipSO.GetAudioClip();
            audioSource.volume = audioClipSO.Volume;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void StopLoop(AudioClipSO audioClipSO)
    {
        if (!_audioSourceMap.TryGetValue(audioClipSO.audioType, out var list)) return;

        AudioClip targetClip = audioClipSO.GetAudioClip();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].isPlaying && list[i].clip == targetClip)
            {
                StopLoop(list[i]);
                break; 
            }
        }
    }

    public void StopLoop(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }
    }

    public async void FadeOut(float fadeDuration, Action onComplete = null)
    {
        _isFadingOut = true;
        _fadeoutSnapshot.TransitionTo(fadeDuration);
        await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), DelayType.Realtime);
        onComplete?.Invoke();
        _isFadingOut = false;
    }

    public async void FadeIn(float fadeDuration, Action onComplete = null)
    {
        if (_isFadingOut)
        {
            await UniTask.WaitWhile(() => _isFadingOut);
        }

        _defaultSnapshot.TransitionTo(fadeDuration);
        await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), DelayType.Realtime);
        onComplete?.Invoke();
    }
}
