
using UnityEngine;

public abstract class AudioClipSO : ScriptableObject
{
    public AudioManager.AudioType audioType;
    [Range(0f, 1f)]
    public float Volume = 1f;
    public abstract AudioClip GetAudioClip();
}