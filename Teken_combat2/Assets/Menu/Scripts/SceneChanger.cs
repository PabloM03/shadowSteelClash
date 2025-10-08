using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para manejar escenas

public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log("Cambiando a la escena: " + sceneName);
    }

    // Método para reiniciar la escena actual
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
        Debug.Log("Reiniciando la escena: " + currentScene);
    }
}
