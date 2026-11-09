using UnityEngine;

// Provides simple button hook methods for scene transitions
public class LoadSceneFunc : MonoBehaviour
{
    public void LoadSceneByIndex(int numScene)
    {
        LoadSceneHandler.LoadSceneByIndex(numScene);
    }

    public void LoadNextScene()
    {
        LoadSceneHandler.LoadNextScene();
    }

    public void LoadSceneByName(string name)
    {
        LoadSceneHandler.LoadSceneByName(name);
    }
}
