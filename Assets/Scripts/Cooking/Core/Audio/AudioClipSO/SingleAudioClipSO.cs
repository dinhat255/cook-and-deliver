using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Clip SO", menuName = "AudioClipSO/SingleAudioClipSO")]
public class SingleAudioClipSO : AudioClipSO
{
    public AudioClip audioClip;
    public override AudioClip GetAudioClip()
    {
        return audioClip;
    }
}