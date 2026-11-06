using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Clip SO", menuName = "AudioClipSO/RaandomAudioClipSO")]
public class RandomAudioClipSO : AudioClipSO
{
    public List<AudioClip> audioClips;
    public override AudioClip GetAudioClip()
    {
        if (audioClips == null || audioClips.Count == 0)
        {
            Debug.LogWarning("No audio clips available.");
            return null;
        }   
        return audioClips[Random.Range(0, audioClips.Count)];
    }
}