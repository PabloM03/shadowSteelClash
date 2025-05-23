using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TeamManager : NetworkBehaviour
{
    private float lastSentHealth = -1f;

    // Diccionario en el servidor con la última vida válida de cada jugador
    private static readonly Dictionary<uint, float> healthRecords = new();

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        var hc = GetComponent<HealthController>();
        if (hc != null)
        {
            lastSentHealth = hc.Health;
        }

        // Reintento de sincronización individual
        InvokeRepeating(nameof(RequestServerResync), 2f, 5f);

        // Solicitar sincronización completa del estado del servidor
        Invoke(nameof(RequestAllPlayersHealth), 1f);
    }

    private void RequestAllPlayersHealth()
    {
        if (isLocalPlayer)
        {
            CmdRequestAllHealth();
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HealthController hc = GetComponent<HealthController>();
        if (hc != null)
        {
            float currentHealth = hc.Health;

            if (Mathf.Abs(currentHealth - lastSentHealth) > 0.1f && currentHealth < lastSentHealth)
            {
                lastSentHealth = currentHealth;
                CmdSendMyHealthToServer(currentHealth);
            }
        }
    }

    [Command]
    private void CmdSendMyHealthToServer(float newHealth)
    {
        HealthController hc = GetComponent<HealthController>();
        if (hc != null)
        {
            hc.Health = newHealth;
            hc.LifeOfBar();

            // Guarda el valor en el servidor para sincronización futura
            healthRecords[netId] = newHealth;

            // Reenvía a todos los clientes
            RpcUpdateHealthToClients(netId, newHealth);
        }
    }

    [ClientRpc]
    private void RpcUpdateHealthToClients(uint knightNetId, float newHealth)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            HealthController hc = knightNi.GetComponent<HealthController>();
            if (hc != null)
            {
                hc.Health = newHealth;
                hc.LifeOfBar();
            }
        }
    }

    private void RequestServerResync()
    {
        if (isLocalPlayer)
        {
            CmdRequestMyLatestHealth();
        }
    }

    [Command]
    private void CmdRequestMyLatestHealth(NetworkConnectionToClient sender = null)
    {
        if (healthRecords.TryGetValue(netId, out float savedHealth))
        {
            TargetForceHealthSync(sender, netId, savedHealth);
        }
    }

    [Command]
    private void CmdRequestAllHealth(NetworkConnectionToClient sender = null)
    {
        foreach (var pair in healthRecords)
        {
            TargetForceHealthSync(sender, pair.Key, pair.Value);
        }
    }

    [TargetRpc]
    private void TargetForceHealthSync(NetworkConnection target, uint knightNetId, float health)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            HealthController hc = knightNi.GetComponent<HealthController>();
            if (hc != null)
            {
                hc.Health = health;
                hc.LifeOfBar();
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(AssignTeams), 0.5f);
    }

    [Server]
    private void AssignTeams()
    {
        GameObject[] allKnights = GameObject.FindGameObjectsWithTag("Player")
            .OrderBy(obj => obj.name)
            .ToArray();

        for (int i = 0; i < allKnights.Length; i++)
        {
            List<uint> enemyNetIds = new();

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
    }

    [ClientRpc]
    private void RpcUpdateEnemyList(uint knightNetId, List<uint> enemyNetIds)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            HealthController hc = knightNi.GetComponent<HealthController>();
            if (hc != null)
            {
                hc.enemies.Clear();
                foreach (uint enemyNetId in enemyNetIds)
                {
                    if (NetworkClient.spawned.TryGetValue(enemyNetId, out NetworkIdentity enemyNi))
                    {
                        hc.enemies.Add(enemyNi.transform);
                    }
                }

                if (knightNi.isLocalPlayer)
                {
                    ColorMyEnemies(hc);
                }
            }
        }
    }

    private void ColorMyEnemies(HealthController hc)
    {
        foreach (Transform enemy in hc.enemies)
        {
            Image bar = enemy.Find("Canvas/background/LifeBar")?.GetComponent<Image>();
            if (bar != null)
                bar.color = Color.red;
        }
    }

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
        HealthController hc = GetComponent<HealthController>();
        if (hc != null)
            ColorMyEnemies(hc);
    }
}
