using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class SelectBuildType : MonoBehaviour
{
    [SerializeField]
    private bool startAsServer = false; // Set this to true for server mode, false for client mode

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
        // Cambia "NombreDeTuEscena" por el nombre real de tu escena
        if (scene.name == "scene1 2")
        {
            if (startAsServer)
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
    }
}