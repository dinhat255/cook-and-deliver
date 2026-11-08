using Cysharp.Threading.Tasks;

namespace CookAndDeliver.SaveLoad.Core
{
    public interface ISaveLoadHandler
    {
        UniTask<bool> SaveAsync(string key, ISaveLoadObject saveLoadable);
        UniTask<T> LoadAsync<T>(string key) where T : class, ISaveLoadObject;
        UniTask<bool> DeleteKeyAsync(string key);
        UniTask<bool> DeleteAllAsync();
    }
}
