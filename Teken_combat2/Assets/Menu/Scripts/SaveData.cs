using UnityEngine;
using TMPro; // Necesario para TextMeshPro
using UnityEngine.UI; // Necesario para Checkbox

public class SaveData : MonoBehaviour
{
    public TextMeshProUGUI textValue1; // Asocia en el Inspector
    public TextMeshProUGUI textValue2; // Asocia en el Inspector
    public TextMeshProUGUI textValue3; // Asocia en el Inspector
    public Toggle checkbox; // Asocia en el Inspector

    public void SaveAndChangeScene(string sceneName)
    {
        // Guardar los valores en la clase estática
        SceneData.numberValue1 = float.Parse(textValue1.text);
        SceneData.numberValue2 = float.Parse(textValue2.text);
        SceneData.numberValue3 = float.Parse(textValue3.text);
        SceneData.isCheckboxChecked = checkbox.isOn;

        // Cambiar de escena
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
