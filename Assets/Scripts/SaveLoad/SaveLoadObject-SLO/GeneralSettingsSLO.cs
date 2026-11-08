using System;
using CookAndDeliver.SaveLoad.Core;
using UnityEngine;

namespace CookAndDeliver.SaveLoad.Objects
{
    [Serializable]
    public class GeneralSettingsSLO : ISaveLoadObject
    {
        public const string DefaultPlayerName = "Chef";

        [SerializeField] private string _playerName = DefaultPlayerName;
        [SerializeField] private float _bgmVolume = 1.0f;
        [SerializeField] private float _sfxVolume = 1.0f;
        [SerializeField] private bool _isMuted = false;
        [SerializeField] private string _language = "vi";

        public string PlayerName
        {
            get => string.IsNullOrWhiteSpace(_playerName) ? DefaultPlayerName : _playerName;
            set => _playerName = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();
        }

        public float BgmVolume
        {
            get => Mathf.Clamp01(_bgmVolume);
            set => _bgmVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => Mathf.Clamp01(_sfxVolume);
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public bool IsMuted
        {
            get => _isMuted;
            set => _isMuted = value;
        }

        public string Language
        {
            get => string.IsNullOrEmpty(_language) ? "vi" : _language;
            set => _language = value;
        }

        public GeneralSettingsSLO()
        {
            _playerName = DefaultPlayerName;
            _bgmVolume = 1.0f;
            _sfxVolume = 1.0f;
            _isMuted = false;
            _language = "vi";
        }

        public GeneralSettingsSLO(string playerName, float bgmVolume = 1.0f, float sfxVolume = 1.0f, bool isMuted = false, string language = "vi")
        {
            PlayerName = playerName;
            BgmVolume = bgmVolume;
            SfxVolume = sfxVolume;
            IsMuted = isMuted;
            Language = language;
        }

        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
