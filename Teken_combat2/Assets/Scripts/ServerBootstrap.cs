using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrap : MonoBehaviour
{
    // Ticks por segundo del servidor dedicado. Sin tope, Unity headless corre el
    // bucle tan rapido como puede y se come un core entero incluso con la arena
    // vacia, lo que en una VM pequeña deja al sistema sin margen.
    private const int ServerTickRate = 30;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!Application.isBatchMode) return;

        QualitySettings.vSyncCount = 0; // vSync no aplica sin pantalla y anularia el tope
        Application.targetFrameRate = ServerTickRate;
        Debug.Log($"[ServerBootstrap] Modo servidor: framerate limitado a {ServerTickRate} fps");

        if (SceneManager.GetActiveScene().name != "scene1 2")
        {
            Debug.Log("[ServerBootstrap] Batch mode detectado, cargando scene1 2...");
            SceneManager.LoadScene("scene1 2");
        }
    }
}
