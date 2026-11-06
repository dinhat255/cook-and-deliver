using UnityEngine;
using UnityEngine.Audio;
public class AudioSourceHandler : MonoBehaviour
{
    private AudioSource _audioSource;
    public AudioMixerGroup audioMixerGroup { 
        get => _audioSource.outputAudioMixerGroup; 
        private set => _audioSource.outputAudioMixerGroup = value; 
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        AudioManager.Instance.RegisterAudioSource(_audioSource);
    }
    private void OnDestroy()
    {
        AudioManager.Instance.UnregisterAudioSource(_audioSource);
    }


}