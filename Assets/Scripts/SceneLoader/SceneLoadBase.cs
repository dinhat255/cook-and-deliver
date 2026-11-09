using System.Collections.Generic;
using UnityEngine;

// Manages and aggregates loading screen visual effects
public class SceneLoadBase : MonoBehaviour
{
    private float _maxEffectTime = 0f;
    [SerializeField] private List<LoaderEffectBase> _loadEffectList = new List<LoaderEffectBase>();

    private void OnValidate()
    {
        CalculateMaxEffectTime();
    }

    private void CalculateMaxEffectTime()
    {
        _maxEffectTime = 0f;
        for (int i = 0; i < _loadEffectList.Count; i++)
        {
            if (_loadEffectList[i] != null)
            {
                float effectTime = _loadEffectList[i].GetEffectTime();
                if (effectTime > _maxEffectTime)
                {
                    _maxEffectTime = effectTime;
                }
            }
        }
    }

    public void InitLoader()
    {
        for (int i = 0; i < _loadEffectList.Count; i++)
        {
            if (_loadEffectList[i] != null)
            {
                _loadEffectList[i].InitLoader();
            }
        }
    }

    public void StartLoading()
    {
        for (int i = 0; i < _loadEffectList.Count; i++)
        {
            if (_loadEffectList[i] != null)
            {
                _loadEffectList[i].StartLoading();
            }
        }
    }

    public void EndLoading()
    {
        for (int i = 0; i < _loadEffectList.Count; i++)
        {
            if (_loadEffectList[i] != null)
            {
                _loadEffectList[i].EndLoading();
            }
        }
    }

    public float GetEffectTime()
    {
        CalculateMaxEffectTime();
        return _maxEffectTime > 0f ? _maxEffectTime : 0.35f;
    }
}
