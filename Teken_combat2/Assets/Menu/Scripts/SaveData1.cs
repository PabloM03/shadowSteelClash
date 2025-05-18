using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveData1 : MonoBehaviour
{
    public Toggle checkbox;

    public void SaveAndChangeScene(string sceneName)
    {
        SceneData.isCheckboxChecked = checkbox.isOn;
        if(gameObject.name == "1vs1")
        {
            SceneData.numberValue1 = 2;
        }
        if(gameObject.name == "2vs2")
        {
            SceneData.numberValue1 = 4;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}