using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EstablecerModo : MonoBehaviour
{
    public TMP_InputField inputField;

    public void SeleccionarModo(string modo)
    {
        MatchInfo.mode = modo;
        MatchInfo.idSolicitado = inputField.text.Trim();
        SceneManager.LoadScene("Scene_Loading");
    }
}