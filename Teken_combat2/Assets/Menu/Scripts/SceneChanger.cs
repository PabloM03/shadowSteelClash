using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para manejar escenas

public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
