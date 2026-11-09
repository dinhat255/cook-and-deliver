using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeableLoader : LoaderEffectBase
{
    [SerializeField] private Image _fadeImg;
    [SerializeField] private float _fadeTime = 0.35f;
    [SerializeField] private Color _fadeColor = new Color(0.07843138f, 0.07058824f, 0.06666667f, 1f); // #141211 warm dark tone matching PillarboxBackground

    public override void InitLoader()
    {
        if (_fadeImg == null)
        {
            _fadeImg = GetComponent<Image>();
        }

        if (_fadeImg != null)
        {
            Color c = _fadeColor;
            c.a = 0f;
            _fadeImg.color = c;
            _fadeImg.raycastTarget = false;
        }
    }

    public override void StartLoading()
    {
        if (_fadeImg == null)
        {
            _fadeImg = GetComponent<Image>();
        }

        if (_fadeImg != null)
        {
            _fadeImg.DOKill();
            Color c = _fadeColor;
            c.a = 0f;
            _fadeImg.color = c;
            _fadeImg.raycastTarget = true;
            _fadeImg.DOFade(1f, _fadeTime).SetUpdate(true);
        }
    }

    public override void EndLoading()
    {
        if (_fadeImg == null)
        {
            _fadeImg = GetComponent<Image>();
        }

        if (_fadeImg != null)
        {
            _fadeImg.DOKill();
            _fadeImg.DOFade(0f, _fadeTime).SetUpdate(true).OnComplete(() =>
            {
                if (_fadeImg != null)
                {
                    _fadeImg.raycastTarget = false;
                }
            });
        }
    }

    public override float GetEffectTime()
    {
        return _fadeTime;
    }
}
