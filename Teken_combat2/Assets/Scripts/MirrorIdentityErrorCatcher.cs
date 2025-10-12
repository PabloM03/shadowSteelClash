using UnityEngine;

/// <summary>
/// Filtra ÚNICAMENTE el error de Mirror que dice que falta un NetworkIdentity.
/// No toca otros errores ni warnings.
/// </summary>
[DefaultExecutionOrder(-9999)]
public class MirrorIdentityErrorCatcher : MonoBehaviour
{
    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessage;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessage;
    }

    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        // Verifica que sea un error
        if (type == LogType.Error)
        {
            // Si el texto contiene la parte específica del error de Mirror
            if (condition.Contains("requires a NetworkIdentity") ||
                condition.Contains("Please add a NetworkIdentity component"))
            {
                // No mostrarlo en consola (simplemente retornamos y lo "tragamos")
                return;
            }
        }

        // Si no es ese error, lo reenviamos normalmente
        Debug.unityLogger.Log(type, condition);
    }
}
