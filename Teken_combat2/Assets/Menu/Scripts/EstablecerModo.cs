using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EstablecerModo : MonoBehaviour
{
    public TMP_InputField inputField;

    // Los 3 botones de multijugador siguen llamando aqui con "unirse"/"crear"/"buscar".
    // Con un unico servidor no hay salas que elegir: todos entran a la misma arena.
    public void SeleccionarModo(string modo)
    {
        SceneManager.LoadScene("scene1 2");
    }
}
