using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookAndDeliver.SaveLoad.Core
{
    public class JsonSaveLoadHandler : ISaveLoadHandler
    {
        private readonly string _saveDirectoryPath;
        private readonly string _saveFileName;
        private bool _useEncryption;
        private SaveTree _saveTree;

        public bool UseEncryption
        {
            get => _useEncryption;
            set => _useEncryption = value;
        }

        public JsonSaveLoadHandler(bool useEncryption = false, string subFolder = "SaveData", string fileName = "savedata.json")
        {
            _saveDirectoryPath = Path.Combine(Application.persistentDataPath, subFolder);
            _saveFileName = fileName;
            _useEncryption = useEncryption;
            _saveTree = new SaveTree();
        }

        private string FullFilePath => Path.Combine(_saveDirectoryPath, _saveFileName);

        public async UniTask<T> LoadAsync<T>(string key) where T : class, ISaveLoadObject
        {
            string filePath = FullFilePath;

            if (!File.Exists(filePath))
            {
                return null;
            }

            if (_saveTree == null || _saveTree.IsEmpty())
            {
                try
                {
                    string rawContent = await File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(rawContent))
                    {
                        if (_useEncryption)
                        {
                            rawContent = EncryptUtil.DecryptString(rawContent);
                        }
                        _saveTree = JsonUtility.FromJson<SaveTree>(rawContent) ?? new SaveTree();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[JsonSaveLoadHandler] Failed to load save file at {filePath}: {ex.Message}");
                    _saveTree = new SaveTree();
                    return null;
                }
            }

            string payload = _saveTree.Get(key);
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(payload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadHandler] Failed to deserialize payload for key '{key}': {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> SaveAsync(string key, ISaveLoadObject saveLoadable)
        {
            if (saveLoadable == null)
            {
                Debug.LogWarning($"[JsonSaveLoadHandler] Attempted to save null object with key: '{key}'");
                return false;
            }

            string filePath = FullFilePath;

            if (!Directory.Exists(_saveDirectoryPath))
            {
                Directory.CreateDirectory(_saveDirectoryPath);
            }

            if ((_saveTree == null || _saveTree.IsEmpty()) && File.Exists(filePath))
            {
                try
                {
                    string rawContent = await File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(rawContent))
                    {
                        if (_useEncryption)
                        {
                            rawContent = EncryptUtil.DecryptString(rawContent);
                        }
                        _saveTree = JsonUtility.FromJson<SaveTree>(rawContent) ?? new SaveTree();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[JsonSaveLoadHandler] Could not parse existing save, creating fresh: {ex.Message}");
                    _saveTree = new SaveTree();
                }
            }

            if (_saveTree == null)
            {
                _saveTree = new SaveTree();
            }

            string payload = saveLoadable.Serialize();
            _saveTree.Set(key, payload);

            try
            {
                string serializedTree = _saveTree.Serialize();
                if (_useEncryption)
                {
                    serializedTree = EncryptUtil.EncryptString(serializedTree);
                }

                await File.WriteAllTextAsync(filePath, serializedTree);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadHandler] Failed to write save file to {filePath}: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> DeleteKeyAsync(string key)
        {
            if (_saveTree == null) return false;
            bool removed = _saveTree.Remove(key);
            if (!removed) return false;

            string filePath = FullFilePath;
            if (File.Exists(filePath))
            {
                try
                {
                    string serializedTree = _saveTree.Serialize();
                    if (_useEncryption)
                    {
                        serializedTree = EncryptUtil.EncryptString(serializedTree);
                    }
                    await File.WriteAllTextAsync(filePath, serializedTree);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonSaveLoadHandler] Failed to update file after deleting key '{key}': {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        public UniTask<bool> DeleteAllAsync()
        {
            _saveTree?.Clear();
            string filePath = FullFilePath;
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonSaveLoadHandler] Failed to delete save file at {filePath}: {ex.Message}");
                    return UniTask.FromResult(false);
                }
            }
            return UniTask.FromResult(true);
        }
    }
}
