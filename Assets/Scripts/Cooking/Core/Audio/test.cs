using UnityEngine;
using UnityEngine.InputSystem;
public class test : MonoBehaviour
{
    [SerializeField] private AudioClipSO _bgAudioClipSO;
    [SerializeField] private AudioClipSO _bgAudioClipSO1;
    private bool _isPlayingSO1 = false;
    private void Start()
    {
        AudioManager.Instance.PlayLoop(_bgAudioClipSO);
        _isPlayingSO1 = false;
    }
    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AudioManager.Instance.FadeOut(1f,
            onComplete: () =>
            {
                var currentAudioClipSO = _isPlayingSO1 ? _bgAudioClipSO1 : _bgAudioClipSO;
                var nextAudioClipSO = _isPlayingSO1 ? _bgAudioClipSO : _bgAudioClipSO1;
                _isPlayingSO1 = !_isPlayingSO1;
                AudioManager.Instance.StopLoop(currentAudioClipSO);
                AudioManager.Instance.PlayLoop(nextAudioClipSO);
                AudioManager.Instance.FadeIn(1f, onComplete: () =>
                {

                });
            });
        }
    }
}