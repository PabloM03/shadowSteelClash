using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TeamManager : NetworkBehaviour
{
    // --- Ajuste para evitar muertes desincronizadas ---
    // Nunca enviamos 0 directamente a los clientes; enviamos un valor diminuto y
    // luego confirmamos la muerte con un RPC específico.
    private const float DeathEpsilon = 0.0001f;

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

            // Solo enviamos al servidor si hay descenso de vida (daño) significativo
            if (Mathf.Abs(currentHealth - lastSentHealth) > 0.1f && currentHealth < lastSentHealth)
            {
                lastSentHealth = currentHealth;
                CmdSendMyHealthToServer(currentHealth);
            }
        }
    }

    // ===== Helpers de aplicación de vida y refresco de UI en clientes =====
    private static void ApplyHealthAndRefreshUI(NetworkIdentity knightNi, float newHealth)
    {
        var hc = knightNi.GetComponent<HealthController>();
        if (hc == null) return;

        // Asignamos SIEMPRE y actualizamos SIEMPRE la UI.
        hc.Health = Mathf.Max(0f, newHealth);
        hc.LifeOfBar(); // Asegúrate de que esta función SIEMPRE dibuja desde hc.Health
    }

    // ===== Server: recibe daño del cliente local y propaga de forma segura =====
    [Command]
    private void CmdSendMyHealthToServer(float newHealth)
    {
        HealthController hc = GetComponent<HealthController>();
        if (hc == null) return;

        // El servidor es autoridad: actualiza su copia
        float clamped = Mathf.Max(0f, newHealth);
        hc.Health = clamped;
        hc.LifeOfBar();

        // Guarda en el registro del servidor
        healthRecords[netId] = clamped;

        if (clamped <= 0f)
        {
            // Fase 1: propagamos un "casi cero" para que los clientes NO ejecuten muerte aún
            RpcUpdateHealthToClients(netId, DeathEpsilon);

            // Fase 2: confirmamos muerte explícitamente (cuando toque)
            RpcConfirmDeath(netId);
        }
        else
        {
            // Vida normal: propagación directa
            RpcUpdateHealthToClients(netId, clamped);
        }
    }

    // ===== Broadcast universal de vida (fase de actualización de barra en todos) =====
    [ClientRpc]
    private void RpcUpdateHealthToClients(uint knightNetId, float newHealth)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            ApplyHealthAndRefreshUI(knightNi, newHealth);
        }
    }

    // ===== Confirmación explícita de muerte por el servidor =====
    [ClientRpc]
    private void RpcConfirmDeath(uint knightNetId)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            // Ahora sí, ponemos 0 exacto y refrescamos UI; si tu HealthController
            // dispara la lógica de "muerte" al ver Health<=0, ocurrirá aquí.
            ApplyHealthAndRefreshUI(knightNi, 0f);
        }
    }

    // ===== Re-sync periódico del cliente local =====
    private void RequestServerResync()
    {
        if (isLocalPlayer)
        {
            CmdRequestMyLatestHealth();
        }
    }

    // ===== Re-sync puntual de mi vida =====
    [Command]
    private void CmdRequestMyLatestHealth(NetworkConnectionToClient sender = null)
    {
        if (healthRecords.TryGetValue(netId, out float savedHealth))
        {
            // Si el servidor tiene guardado <=0, aplicamos mismo patrón de dos fases
            if (savedHealth <= 0f)
            {
                TargetForceHealthSync(sender, netId, DeathEpsilon);
                TargetConfirmDeath(sender, netId);
            }
            else
            {
                TargetForceHealthSync(sender, netId, savedHealth);
            }
        }
    }

    // ===== Re-sync de todos los jugadores para un cliente concreto =====
    [Command]
    private void CmdRequestAllHealth(NetworkConnectionToClient sender = null)
    {
        foreach (var pair in healthRecords)
        {
            uint kId = pair.Key;
            float h  = pair.Value;

            if (h <= 0f)
            {
                TargetForceHealthSync(sender, kId, DeathEpsilon);
                // Confirmación de muerte separada
                TargetConfirmDeath(sender, kId);
            }
            else
            {
                TargetForceHealthSync(sender, kId, h);
            }
        }
    }

    // ===== Target RPCs para forzar estado en un cliente específico =====
    [TargetRpc]
    private void TargetForceHealthSync(NetworkConnection target, uint knightNetId, float health)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            ApplyHealthAndRefreshUI(knightNi, health);
        }
    }

    [TargetRpc]
    private void TargetConfirmDeath(NetworkConnection target, uint knightNetId)
    {
        if (NetworkClient.spawned.TryGetValue(knightNetId, out NetworkIdentity knightNi))
        {
            ApplyHealthAndRefreshUI(knightNi, 0f);
        }
    }

    // ===== Asignación de equipos (sin cambios funcionales) =====
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
