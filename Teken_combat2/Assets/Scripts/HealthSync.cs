using Mirror;
using UnityEngine;

// Transporte de red de la vida, compartido por Knight y Warrok.
//
// HealthController sigue siendo un MonoBehaviour normal y guarda la vida en
// local como siempre. Este componente solo hace dos cosas:
//   1. Decir quien tiene autoridad sobre la vida de ESTE personaje: su dueño,
//      el servidor si no tiene dueño, o la maquina local si no hay red.
//   2. Replicar la vida al resto en cuanto cambia, sin sondeo ni retardo.
//
// Sustituye al sondeo por Update() que TeamManager hacia para los Knights y al
// SyncVar que WarrokController tenia solo para los Warroks: ahora los dos pasan
// por aqui y se comportan igual.
[RequireComponent(typeof(HealthController))]
public class HealthSync : NetworkBehaviour
{
    // -1 = "todavia no ha llegado ningun valor", para no confundirlo con 0.
    [SyncVar(hook = nameof(OnSyncedHealthChanged))]
    private float syncedHealth = -1f;

    private HealthController hc;

    private void Awake()
    {
        hc = GetComponent<HealthController>();
    }

    private static bool SinRed => !NetworkClient.active && !NetworkServer.active;

    // Solo una maquina aplica el daño y decide las animaciones de ESTE personaje;
    // el resto recibe el resultado replicado. Si lo aplicaran todas, cada una
    // calcularia un resultado distinto segun su propia vision con latencia de la
    // pelea, y la vida divergiria entre pantallas.
    //
    // Manda el cliente dueño. El servidor solo manda sobre objetos sin dueño (en
    // este juego no hay: Knights y Warroks siempre pertenecen a una conexion).
    public bool TengoAutoridad()
    {
        if (SinRed) return true;
        if (isOwned) return true;
        return isServer && connectionToClient == null;
    }

    // Llamado por HealthController justo cuando la vida cambia de verdad en la
    // maquina con autoridad. Sale al momento: sin sondeo, sin retardo añadido.
    public void ReportHealth(float value)
    {
        if (SinRed) return;
        if (isServer) syncedHealth = value;
        else CmdReportHealth(value);
    }

    [Command]
    private void CmdReportHealth(float value)
    {
        syncedHealth = value;

        // En un servidor dedicado el hook no se dispara al escribir en local, asi
        // que se aplica a mano para que su copia tambien refleje la vida real.
        if (hc == null) hc = GetComponent<HealthController>();
        if (hc != null) hc.SetHealthFromNetwork(value);
    }

    private void OnSyncedHealthChanged(float _, float newValue)
    {
        // El dueño ya tiene el valor bueno; esto es para los demas.
        if (isOwned || newValue < 0f) return;
        if (hc == null) hc = GetComponent<HealthController>();
        if (hc != null) hc.SetHealthFromNetwork(newValue);
    }
}
