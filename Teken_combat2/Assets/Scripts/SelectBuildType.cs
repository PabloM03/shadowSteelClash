using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class SelectBuildType : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Solo se usa dentro del editor. En una build el modo se detecta solo: " +
             "si corre headless (batchmode / sin GPU) arranca como servidor, si no como cliente.")]
    private bool startAsServer = false;

    private bool yaArrancado;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "scene1 2")
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "scene1 2") return;

        // Start() y el evento sceneLoaded pueden dispararse ambos para la misma escena.
        if (yaArrancado) return;
        if (NetworkServer.active || NetworkClient.active) return;
        yaArrancado = true;

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[SelectBuildType] No hay NetworkManager en la escena");
            return;
        }

        if (DebeArrancarComoServidor())
        {
            Debug.Log("Iniciando como Servidor...");
            NetworkManager.singleton.StartServer();
        }
        else
        {
            Debug.Log("Iniciando como Cliente...");
            NetworkManager.singleton.StartClient();
        }

        var networkManagerHUD = FindObjectOfType<NetworkManagerHUD>();
        if (networkManagerHUD != null)
        {
            networkManagerHUD.enabled = false;
            Debug.Log("NetworkManagerHUD desactivado");
        }
    }

    // En el editor manda el checkbox del inspector, para poder probar ambos modos.
    // En una build el modo se deduce del entorno, de forma que el binario del
    // servidor no puede arrancar como cliente por haber dejado el flag mal puesto.
    private bool DebeArrancarComoServidor()
    {
#if UNITY_EDITOR
        return startAsServer;
#else
        return EsHeadless();
#endif
    }

    private static bool EsHeadless()
    {
        if (Application.isBatchMode) return true;

        // Sin GPU real (-nographics) solo tiene sentido en un servidor dedicado.
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            return true;

        foreach (string arg in System.Environment.GetCommandLineArgs())
        {
            if (arg == "-batchmode" || arg == "-nographics" || arg == "-server")
                return true;
        }

        return false;
    }
}
