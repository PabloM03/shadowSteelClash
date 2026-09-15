using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

// Asignacion de equipos y registro de Warroks como enemigos.
//
// La sincronizacion de vida que vivia aqui (sondeo en Update + Cmd + Rpc con
// registro por netId) se ha movido a HealthSync, compartido por Knight y Warrok.
// Era un segundo sistema paralelo al de HealthController, llegaba con hasta un
// segundo de retraso, y tenia comentada la llamada a Die(): la muerte de los
// Knights nunca llegaba de forma fiable a los demas clientes.
public class TeamManager : NetworkBehaviour
{
    private static void TryColorEnemies(HealthController hc)
    {
        if (hc == null) return;
        foreach (Transform enemy in hc.enemies)
        {
            if (enemy == null) continue;
            Image bar = enemy.Find("Canvas/background/LifeBar")?.GetComponent<Image>();
            if (bar != null) bar.color = Color.red;
        }
    }

    // ====== Ciclo de vida en cliente ======
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            Invoke(nameof(UpdateLocalEnemyBars), 1f);
        }
    }

    private void UpdateLocalEnemyBars()
    {
        TryColorEnemies(GetComponent<HealthController>());
    }

    // ====== Servidor ======
    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(AssignTeams), 0.5f);

        // Nota: no se reasignan equipos cuando alguien se desconecta. AssignTeams
        // reparte por paridad del indice, asi que quitar a uno de en medio
        // reordena la lista y puede convertir en enemigos a dos que eran
        // aliados. Para que el que se va deje de contar basta con que su
        // Transform quede destruido: HealthController lo limpia de enemies en el
        // siguiente frame, la barra se oculta y nadie gana por un rival ausente.
    }

    // ====== Registro de Warroks ======

    // Llamado por el owner del knight tras spawnear su warrok
    [Command]
    public void CmdRegisterWarrok(uint warrokNetId, uint ownerKnightNetId)
    {
        RpcRegisterWarrok(warrokNetId, ownerKnightNetId);
    }

    [ClientRpc]
    private void RpcRegisterWarrok(uint warrokNetId, uint ownerKnightNetId)
    {
        StartCoroutine(ApplyWarrokRegistration(warrokNetId, ownerKnightNetId));
    }

    private IEnumerator ApplyWarrokRegistration(uint warrokNetId, uint ownerKnightNetId)
    {
        // Esperar a que ambos objetos estén disponibles en este cliente
        while (!NetworkClient.spawned.ContainsKey(warrokNetId) || !NetworkClient.spawned.ContainsKey(ownerKnightNetId))
            yield return null;

        var warrokT = NetworkClient.spawned[warrokNetId].transform;
        var ownerT = NetworkClient.spawned[ownerKnightNetId].transform;
        var ownerHC = ownerT.GetComponent<HealthController>();
        if (ownerHC == null) yield break;

        // Solo actuar si el jugador local es enemigo del owner del warrok
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) yield break;
        if (!ownerHC.enemies.Contains(localPlayer.transform)) yield break;

        var localHC = localPlayer.GetComponent<HealthController>();
        var warrokHC = warrokT.GetComponent<HealthController>();
        var warrokWC = warrokT.GetComponent<WarrokController>();

        // El warrok me ataca a mí
        if (warrokHC != null && !warrokHC.enemies.Contains(localPlayer.transform))
            warrokHC.enemies.Add(localPlayer.transform);
        if (warrokWC != null && !warrokWC.knights.Contains(localPlayer.transform))
            warrokWC.knights.Add(localPlayer.transform);

        // Yo recibo daño del warrok
        if (localHC != null && !localHC.enemies.Contains(warrokT))
            localHC.enemies.Add(warrokT);
    }

    // ====== Teams (tu lógica original, intacta) ======
    [Server]
    private void AssignTeams()
    {
        GameObject[] allKnights = GameObject.FindGameObjectsWithTag("Player")
            .OrderBy(obj => obj.name)
            .ToArray();

        for (int i = 0; i < allKnights.Length; i++)
        {
            List<uint> enemyNetIds = new List<uint>();

            for (int j = 0; j < allKnights.Length; j++)
            {
                if (i == j) continue;

                bool isKnightEven = (i % 2 == 0);
                bool isOtherEven = (j % 2 == 0);

                if (isKnightEven != isOtherEven)
                {
                    NetworkIdentity enemyNI = allKnights[j].GetComponent<NetworkIdentity>();
                    if (enemyNI != null)
                        enemyNetIds.Add(enemyNI.netId);
                }
            }

            NetworkIdentity knightNI = allKnights[i].GetComponent<NetworkIdentity>();
            if (knightNI != null)
            {
                RpcUpdateEnemyList(knightNI.netId, enemyNetIds);
            }
        }

        // Re-registrar Warroks existentes para que nuevos jugadores también los tengan como enemigos
        foreach (var knight in allKnights)
        {
            var kc = knight.GetComponent<KnightController>();
            if (kc == null) continue;
            uint wNetId = kc.GetWarrokNetId();
            if (wNetId == 0) continue;
            var ni = knight.GetComponent<NetworkIdentity>();
            if (ni != null)
                RpcRegisterWarrok(wNetId, ni.netId);
        }
    }

    [ClientRpc]
    private void RpcUpdateEnemyList(uint knightNetId, List<uint> enemyNetIds)
    {
        NetworkIdentity knightNi;
        if (NetworkClient.spawned.TryGetValue(knightNetId, out knightNi))
        {
            HealthController hc = knightNi.GetComponent<HealthController>();
            if (hc != null)
            {
                hc.enemies.Clear();
                foreach (uint enemyNetId in enemyNetIds)
                {
                    NetworkIdentity enemyNi;
                    if (NetworkClient.spawned.TryGetValue(enemyNetId, out enemyNi))
                    {
                        hc.enemies.Add(enemyNi.transform);
                    }
                }

                if (knightNi.isLocalPlayer)
                {
                    TryColorEnemies(hc);
                }
            }
        }
    }
}
