using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Application.isBatchMode && SceneManager.GetActiveScene().name != "scene1 2")
        {
            Debug.Log("[ServerBootstrap] Batch mode detectado, cargando scene1 2...");
            SceneManager.LoadScene("scene1 2");
        }
    }
}
