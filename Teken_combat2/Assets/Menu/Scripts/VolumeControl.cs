using UnityEngine;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    public Slider volumeSlider; // Referencia al Slider de la UI
    public AudioSource audioSource; // Referencia al AudioSource para controlar el volumen

    void Start()
    {
        // Inicializar el valor del Slider con el volumen actual del AudioSource
        if (audioSource != null && volumeSlider != null)
        {
            volumeSlider.value = audioSource.volume;
            volumeSlider.onValueChanged.AddListener(CambiarVolumen);
        }
    }

    public void CambiarVolumen(float volumen)
    {
        if (audioSource != null)
        {
            audioSource.volume = volumen; // Cambiar el volumen del AudioSource
        }
    }
}
