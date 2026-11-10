using System;
using CookAndDeliver.SaveLoad.Core;
using CookAndDeliver.SaveLoad.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // Centralizes loading and serialized saving of GameProgressSLO so all scenes share one consistent state
    public static class GameProgressStore
    {
        public const string SaveKey = "GameProgress";

        private static GameProgressSLO _cachedProgress;
        private static UniTask<GameProgressSLO> _loadTask;
        private static bool _isLoaded;
        private static bool _isLoading;
        private static bool _isSaving;
        private static bool _savePending;

        public static bool IsLoaded => _isLoaded;

        public static GameProgressSLO Progress
        {
            get
            {
                if (_cachedProgress == null)
                {
                    _cachedProgress = new GameProgressSLO();
                }
                return _cachedProgress;
            }
        }

        public static bool IsCookingTutorialCompleted => Progress.IsCookingTutorialCompleted;
        public static bool IsDeliveryTutorialCompleted => Progress.IsDeliveryTutorialCompleted;

        public static void ResetCacheForEditorTest()
        {
            ResetStaticState();
            CookingSession.ResetSessionForTest();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _cachedProgress = null;
            _loadTask = default;
            _isLoaded = false;
            _isLoading = false;
            _isSaving = false;
            _savePending = false;
        }

        public static UniTask<GameProgressSLO> EnsureLoadedAsync()
        {
            if (_isLoaded && _cachedProgress != null)
            {
                return UniTask.FromResult(_cachedProgress);
            }

            if (_isLoading)
            {
                return _loadTask;
            }

            _isLoading = true;
            _loadTask = LoadInternalAsync();
            return _loadTask;
        }

        private static async UniTask<GameProgressSLO> LoadInternalAsync()
        {
            try
            {
                var loaded = await SaveLoadManager.LoadAsync<GameProgressSLO>(SaveKey);
                _cachedProgress = loaded ?? new GameProgressSLO();
                if (_cachedProgress.CurrentDay < 1)
                {
                    _cachedProgress.CurrentDay = 1;
                }
                ApplyToSession();
                _isLoaded = true;
                return _cachedProgress;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameProgressStore] Failed to load progress: {ex.Message}");
                _cachedProgress = new GameProgressSLO();
                ApplyToSession();
                _isLoaded = true;
                return _cachedProgress;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public static void ApplyToSession()
        {
            if (_cachedProgress == null) return;
            CookingSession.Coins = _cachedProgress.TotalCoins;
            CookingSession.CurrentDay = Mathf.Max(1, _cachedProgress.CurrentDay);
            CookingSession.CompletedOrdersCount = _cachedProgress.TotalOrdersCompleted;
        }

        public static void SyncFromSession(bool save = true)
        {
            var progress = Progress;
            progress.TotalCoins = CookingSession.Coins;
            progress.CurrentDay = Mathf.Max(1, CookingSession.CurrentDay);
            progress.TotalOrdersCompleted = Mathf.Max(progress.TotalOrdersCompleted, CookingSession.CompletedOrdersCount);
            progress.RecordCombo(CookingSession.Combo);

            if (save && _isLoaded)
            {
                SaveAsync().Forget();
            }
        }

        public static void MarkCookingTutorialCompleted(bool save = true)
        {
            if (Progress.IsCookingTutorialCompleted) return;
            Progress.IsCookingTutorialCompleted = true;
            if (save)
            {
                SaveAsync().Forget();
            }
        }

        public static void MarkDeliveryTutorialCompleted(bool save = true)
        {
            if (Progress.IsDeliveryTutorialCompleted) return;
            Progress.IsDeliveryTutorialCompleted = true;
            if (save)
            {
                SaveAsync().Forget();
            }
        }

        public static void ResetAll()
        {
            _cachedProgress = new GameProgressSLO();
            _isLoaded = true;
            ApplyToSession();
            SaveAsync().Forget();
        }

        public static async UniTask SaveAsync()
        {
            if (_isSaving)
            {
                _savePending = true;
                return;
            }

            _isSaving = true;
            try
            {
                do
                {
                    _savePending = false;
                    await SaveLoadManager.SaveAsync(SaveKey, Progress);
                }
                while (_savePending);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameProgressStore] Failed to save progress: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }
    }
}
