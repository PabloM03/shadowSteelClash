using UnityEngine;
using Mirror;

public class UnirseManager : NetworkBehaviour
{
    //public override void OnStartLocalPlayer()
    public void empezarUnirse()
    {
        // Solo el jugador local debe iniciar esta solicitud
        CmdUnirsePartida(MatchInfo.mode, MatchInfo.idSolicitado);
    }

    [Command]
    public void CmdUnirsePartida(string modo, string id)
    {
        var servidor = FindObjectOfType<SalaManager>();
        Debug.LogError("Dentro.");
        if (servidor == null)
        {
            Debug.LogError("No se encontró el SalaManager en el servidor.");
            return;
        }

        switch (modo)
        {
            case "unirse":
                servidor.CrearSala(connectionToClient);
                break;
            case "crear":
                servidor.UnirseAleatoria(connectionToClient);
                break;
            case "buscar":
                servidor.UnirsePorID(id, connectionToClient);
                break;
            default:
                Debug.LogWarning($"Modo desconocido: {modo}");
                break;
        }
    }
}
