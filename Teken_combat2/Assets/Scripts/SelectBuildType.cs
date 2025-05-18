using UnityEngine;
using Mirror;

public class SelectBuildType : MonoBehaviour
{
    [SerializeField]
    private bool startAsServer = false; // Set this to true for server mode, false for client mode

    void Start()
    {
        // Automatically start as server or client based on the configuration
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

        // Desactiva el NetworkManagerHUD si está presente
        var networkManagerHUD = FindObjectOfType<NetworkManagerHUD>();
        if (networkManagerHUD != null)
        {
            networkManagerHUD.enabled = false;
            Debug.Log("NetworkManagerHUD desactivado");
        }
    }
}
