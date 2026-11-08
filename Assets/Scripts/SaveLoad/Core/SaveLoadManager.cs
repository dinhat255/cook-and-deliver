using Cysharp.Threading.Tasks;

namespace CookAndDeliver.SaveLoad.Core
{
    public static class SaveLoadManager
    {
        private static ISaveLoadHandler _saveLoadHandler = new JsonSaveLoadHandler();

        public static ISaveLoadHandler Handler => _saveLoadHandler;

        public static void SetHandler(ISaveLoadHandler handler)
        {
            if (handler != null)
            {
                _saveLoadHandler = handler;
            }
        }

        public static async UniTask<bool> SaveAsync(string key, ISaveLoadObject saveLoadable)
        {
            return await _saveLoadHandler.SaveAsync(key, saveLoadable);
        }

        public static async UniTask<T> LoadAsync<T>(string key) where T : class, ISaveLoadObject
        {
            return await _saveLoadHandler.LoadAsync<T>(key);
        }

        public static async UniTask<bool> DeleteKeyAsync(string key)
        {
            return await _saveLoadHandler.DeleteKeyAsync(key);
        }

        public static async UniTask<bool> DeleteAllAsync()
        {
            return await _saveLoadHandler.DeleteAllAsync();
        }
    }
}
