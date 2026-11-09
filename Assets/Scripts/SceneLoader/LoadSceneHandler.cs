using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneHandler : MonoBehaviour
{
    [SerializeField] private SceneLoadBase _sceneLoader;
    [SerializeField] private Camera _cam;

    public const string SceneLoaderPath = "SceneLoader";
    private static int _nextSceneIndex = -1;
    private static string _nextSceneName = string.Empty;
    private static int _prevSceneIndex = -1;
    private static string _prevSceneName = string.Empty;
    private static bool _lock = false;

    public static bool Lock => _lock;

    public static Func<UniTask> LoadSceneEvent;
    public static UniTask RaiseLoadSceneEvent()
    {
        return LoadSceneEvent != null ? LoadSceneEvent.Invoke() : UniTask.CompletedTask;
    }

    private void Awake()
    {
        if (_sceneLoader == null)
        {
            _sceneLoader = GetComponentInChildren<SceneLoadBase>(true);
        }

        if (_sceneLoader != null)
        {
            _sceneLoader.InitLoader();
        }

        if (_cam != null)
        {
            _cam.gameObject.SetActive(false);
        }
    }

    public static void LoadSceneByIndex(int index)
    {
        if (_lock) return;
        Scene activeScene = SceneManager.GetActiveScene();
        _prevSceneIndex = activeScene.buildIndex;
        _prevSceneName = activeScene.name;
        _nextSceneIndex = index;
        _nextSceneName = string.Empty;
        SceneManager.LoadScene(SceneLoaderPath, LoadSceneMode.Additive);
    }

    public static void LoadNextScene()
    {
        if (_lock) return;
        Scene activeScene = SceneManager.GetActiveScene();
        _prevSceneIndex = activeScene.buildIndex;
        _prevSceneName = activeScene.name;
        _nextSceneIndex = (_prevSceneIndex + 1) % Mathf.Max(1, SceneManager.sceneCountInBuildSettings);
        _nextSceneName = string.Empty;
        SceneManager.LoadScene(SceneLoaderPath, LoadSceneMode.Additive);
    }

    public static void LoadSceneByName(string name)
    {
        if (_lock) return;
        Scene activeScene = SceneManager.GetActiveScene();
        _prevSceneIndex = activeScene.buildIndex;
        _prevSceneName = activeScene.name;
        _nextSceneName = name;
        _nextSceneIndex = ResolveBuildIndex(name);
        SceneManager.LoadScene(SceneLoaderPath, LoadSceneMode.Additive);
    }

    private static int ResolveBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string fileName = Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(fileName, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private async void Start()
    {
        await HelpLoadingScene();
    }

    private async UniTask HelpLoadingScene()
    {
        try
        {
            _lock = true;

            // Trigger screen fade in effect
            if (_sceneLoader != null)
            {
                _sceneLoader.StartLoading();
            }

            float effectTime = _sceneLoader != null ? _sceneLoader.GetEffectTime() : 0.35f;
            int delayMs = Mathf.Max(50, Mathf.RoundToInt(effectTime * 1000f));
            await UniTask.Delay(delayMs, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Enable temporary loader camera while old scene is being unloaded
            if (_cam != null)
            {
                _cam.gameObject.SetActive(true);
            }

            // Unload the previous scene
            if (_prevSceneIndex >= 0 && _prevSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                await SceneManager.UnloadSceneAsync(_prevSceneIndex);
            }
            else if (!string.IsNullOrEmpty(_prevSceneName))
            {
                Scene prev = SceneManager.GetSceneByName(_prevSceneName);
                if (prev.IsValid() && prev.isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(prev);
                }
            }

            // Load the target scene additively
            AsyncOperation loadOperation;
            if (_nextSceneIndex >= 0)
            {
                loadOperation = SceneManager.LoadSceneAsync(_nextSceneIndex, LoadSceneMode.Additive);
            }
            else if (!string.IsNullOrEmpty(_nextSceneName))
            {
                loadOperation = SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Additive);
            }
            else
            {
                throw new InvalidOperationException("No valid next scene specified for SceneLoader.");
            }

            await UniTask.WaitUntil(() => loadOperation.isDone, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Set the newly loaded scene as active
            Scene nextScene = _nextSceneIndex >= 0
                ? SceneManager.GetSceneByBuildIndex(_nextSceneIndex)
                : SceneManager.GetSceneByName(_nextSceneName);

            if (nextScene.IsValid() && nextScene.isLoaded)
            {
                SceneManager.SetActiveScene(nextScene);
            }
            else
            {
                Debug.LogWarning($"Failed to set next scene as active (Index: {_nextSceneIndex}, Name: {_nextSceneName}).");
            }

            // Turn off loader camera before revealing new scene
            if (_cam != null)
            {
                _cam.gameObject.SetActive(false);
            }

            // Trigger screen fade out effect
            if (_sceneLoader != null)
            {
                _sceneLoader.EndLoading();
            }

            await UniTask.Delay(delayMs, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Unload the SceneLoader additive scene
            await SceneManager.UnloadSceneAsync(SceneLoaderPath);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Scene loading canceled because the loader object was destroyed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during scene loading: {ex}");
        }
        finally
        {
            _lock = false;
        }
    }
}
