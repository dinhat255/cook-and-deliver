using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "New Audio Clip SO", menuName = "AudioClipSO/SequenceAudioClipSO")]
public class SequenceAudioClipSO : AudioClipSO
{
    public List<AudioClip> audioClips;
    private int _currentIndex = 0;
    public override AudioClip GetAudioClip()
    {
        if (audioClips == null || audioClips.Count == 0)
        {
            Debug.LogWarning("No audio clips available in the sequence.");
            return null;
        }

        AudioClip clip = audioClips[_currentIndex];
        _currentIndex = (_currentIndex + 1) % audioClips.Count; // Loop back to the start
        return clip;
    }
}