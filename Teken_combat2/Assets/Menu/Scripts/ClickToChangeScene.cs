using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToChangeScene : MonoBehaviour
{
    public string sceneName;

    public void OnMouseDown()
    {
        SceneManager.LoadScene(sceneName);
    }
}
