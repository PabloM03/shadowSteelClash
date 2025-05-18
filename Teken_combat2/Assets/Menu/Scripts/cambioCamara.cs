using UnityEngine;

public class CambioCamara : MonoBehaviour
{
    public Camera targetCamera; // La cámara a activar
    public Camera currentCamera; // La cámara actual

    void Start()
    {
        // Configurar las cámaras al inicio
        if (currentCamera != null)
        {
            currentCamera.gameObject.SetActive(true);
        }
        if (targetCamera != null)
        {
            targetCamera.gameObject.SetActive(false);
        }
    }

    public void CambiarCamara()
    {
        // Desactivar la cámara actual
        if (currentCamera != null)
        {
            currentCamera.gameObject.SetActive(false);
        }

        // Activar la nueva cámara y actualizar la referencia
        if (targetCamera != null)
        {
            targetCamera.gameObject.SetActive(true);
            currentCamera = targetCamera;
        }
    }
}
