using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para manejar escenas
using Mirror;

public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log("Cambiando a la escena: " + sceneName);
    }

    // Salir de una partida online. Antes el boton "Salir" solo cargaba el menu:
    // la conexion Mirror seguia viva (el NetworkManager es DontDestroyOnLoad y
    // no tiene offlineScene), el servidor nunca se enteraba, y el Knight y el
    // Warrok del que se iba se quedaban plantados en la arena para los demas.
    // Al parar el cliente, Mirror avisa al servidor, que destruye todos los
    // objetos de esa conexion y lo replica al resto.
    public void LeaveOnlineMatchAndChangeScene(string sceneName)
    {
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active && NetworkClient.active)
                NetworkManager.singleton.StopHost();
            else if (NetworkClient.active)
                NetworkManager.singleton.StopClient();
        }

        ChangeScene(sceneName);
    }

    // Método para reiniciar la escena actual
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
        Debug.Log("Reiniciando la escena: " + currentScene);
    }
}
