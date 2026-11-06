using UnityEngine;
public class BGAudioRunner : MonoBehaviour
{
    [SerializeField] private AudioClipSO _bgAudioClipSO;
    private void Awake()
    {
        AudioManager.Instance.PlayLoop(_bgAudioClipSO);
        AudioManager.Instance.FadeIn(2f, onComplete: () =>
        {
            Debug.Log("Fade In Complete");
        });
    }
    private void OnDestroy()
    {
        AudioManager.Instance.FadeOut(2f, 
        onComplete: () => AudioManager.Instance.StopLoop(_bgAudioClipSO));
    }
}