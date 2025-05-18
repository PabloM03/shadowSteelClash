using UnityEngine;
using UnityEngine.UI;

public class CameraRaycasterManager : MonoBehaviour
{
    public Camera[] cameras; // Lista de cámaras en la escena
    public Canvas[] canvases; // Lista de Canvas correspondientes

    private int currentCameraIndex = 0;

    public void SwitchToCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= cameras.Length)
        {
            Debug.LogError("Índice de cámara fuera de rango.");
            return;
        }

        // Desactiva las interacciones de todos los Canvas
        for (int i = 0; i < canvases.Length; i++)
        {
            GraphicRaycaster raycaster = canvases[i].GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }

        // Activa la cámara seleccionada
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == cameraIndex);
        }

        // Habilita las interacciones del Canvas correspondiente
        GraphicRaycaster activeRaycaster = canvases[cameraIndex].GetComponent<GraphicRaycaster>();
        if (activeRaycaster != null)
        {
            activeRaycaster.enabled = true;
        }

        currentCameraIndex = cameraIndex;
    }
}
