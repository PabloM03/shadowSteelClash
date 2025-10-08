using UnityEngine;
using Mirror;

public class DisconnectClient : MonoBehaviour
{
    public void OnDisconnectButtonPressed()
    {
        if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}
