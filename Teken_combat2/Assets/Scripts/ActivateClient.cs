using UnityEngine;
using UnityEngine.SceneManagement;

public class ActivateClient : MonoBehaviour
{
    private GameObject selectBuildType;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "scene1 2")
        {
            Debug.Log("Scene 'scene1 2' loaded. Activating SelectBuildType.");
            GetComponent<SelectBuildType>().enabled = true;
        }
    }
}
