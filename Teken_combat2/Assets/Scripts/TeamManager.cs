using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TeamManager : NetworkBehaviour
{
    // --- Ajuste para evitar jitter de 0 exacto en barras antes de confirmar muerte ---
    private const float DeathEpsilon = 0.0001f;
    private const float HealthDeltaThreshold = 0.01f; // todo cambio real de vida (daño o curación)

    private float lastSendTime = 0f;
    private float lastSentHealth = -1f;

    // Registro maestro en servidor: última vida válida por netId
    private static readonly Dictionary<uint, float> healthRecords = new Dictionary<uint, float>();

    // ====== Helpers comunes ======
    private static void ApplyHealthAndRefreshUI(NetworkIdentity knightNi, float newHealth)
    {
        var hc = knightNi.GetComponent<HealthController>();
        if (hc == null) return;

        hc.Health = Mathf.Max(0f, newHealth);
        hc.LifeOfBar(); // Debe dibujar desde hc.Health SIEMPRE
    }

    private static void TryColorEnemies(HealthController hc)
    {
        if (hc == null) return;
        foreach (Transform enemy in hc.enemies)
        {
            Image bar = enemy.Find("Canvas/background/LifeBar")?.GetComponent<Image>();
            if (bar != null) bar.color = Color.red;
        }
    }

    // ====== Ciclo de vida en cliente ======
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        var hc = GetComponent<HealthController>();
        if (hc != null)
        {
            lastSentHealth = hc.Health;
        }

        // Reintento de sincronización individual (por si hay drift)
        InvokeRepeating(nameof(RequestServerResync), 2f, 5f);

        // Solicitar sincronización completa del estado a la entrada
        Invoke(nameof(RequestAllPlayersHealth), 1f);
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
        TryColorEnemies(GetComponent<HealthController>());
    }

    private void RequestAllPlayersHealth()
    {
        if (isLocalPlayer)
        {
            CmdRequestAllHealth();
        }
    }

    private void RequestServerResync()
    {
        if (isLocalPlayer)
        {
            CmdRequestMyLatestHealth();
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        var hc = GetComponent<HealthController>();
        if (hc == null) return;

        float currentHealth = hc.Health;

        // >>>>> OWNER-AUTHORITATIVE <<<<<
        // Enviamos al servidor todo cambio significativo (daño o curación)
        if (Mathf.Abs(currentHealth - lastSentHealth) > HealthDeltaThreshold || Time.time - lastSendTime >= 1f)
        {
            lastSentHealth = currentHealth;
            lastSendTime = Time.time;
            CmdSendMyHealthToServer(currentHealth);
        }
    }

    // ====== Servidor: autoridad, guarda y propaga ======
    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(AssignTeams), 0.5f);
    }

    [Command]
    private void CmdSendMyHealthToServer(float newHealth)
    {
        var hc = GetComponent<HealthController>();
        if (hc == null) return;

        // El servidor es autoridad final
        float clamped = Mathf.Max(0f, newHealth);
        hc.Health = clamped;
        hc.LifeOfBar();

        healthRecords[netId] = clamped;

        if (clamped <= 0f)
        {
            // 1) Propaga "casi cero" para que barras no disparen muerte prematura
            RpcSyncHealth(netId, DeathEpsilon);

            // 2) Confirma 0 definitivo
            RpcSyncHealth(netId, 0f);

            // 3) Ejecuta muerte en servidor y en todos los clientes
            //hc.Die();             // servidor también muere (coherencia server-side)
            //RpcInvokeDeath(netId); // clientes mueren
        }
        else
        {
            RpcSyncHealth(netId, clamped);
        }
    }

    [Command]
    private void CmdRequestMyLatestHealth(NetworkConnectionToClient sender = null)
    {
        float savedHealth;
        if (!healthRecords.TryGetValue(netId, out savedHealth)) return;

        if (savedHealth <= 0f)
        {
            TargetForceHealthSync(sender, netId, DeathEpsilon);
            TargetForceHealthSync(sender, netId, 0f);
            TargetInvokeDeath(sender, netId);
        }
        else
        {
            TargetForceHealthSync(sender, netId, savedHealth);
        }
    }

    [Command]
    private void CmdRequestAllHealth(NetworkConnectionToClient sender = null)
    {
        foreach (var pair in healthRecords)
        {
            uint kId = pair.Key;
            float h = pair.Value;

            if (h <= 0f)
            {
                TargetForceHealthSync(sender, kId, DeathEpsilon);
                TargetForceHealthSync(sender, kId, 0f);
                TargetInvokeDeath(sender, kId);
            }
            else
            {
                TargetForceHealthSync(sender, kId, h);
            }
        }
    }

    // ====== RPCs de sincronización ======
    [ClientRpc]
    private void RpcSyncHealth(uint knightNetId, float newHealth)
    {
        NetworkIdentity knightNi;
        if (NetworkClient.spawned.TryGetValue(knightNetId, out knightNi))
        {
            ApplyHealthAndRefreshUI(knightNi, newHealth);
        }
    }

    // Llama a Die() en TODAS las instancias del jugador indicado
    [ClientRpc]
    private void RpcInvokeDeath(uint knightNetId)
    {
        NetworkIdentity knightNi;
        if (NetworkClient.spawned.TryGetValue(knightNetId, out knightNi))
        {
            var hc = knightNi.GetComponent<HealthController>();
            if (hc == null) return;

            // Con tu HealthController actual, Die() es internal: llamamos directo
            hc.Die();
        }
    }

    [TargetRpc]
    private void TargetForceHealthSync(NetworkConnectionToClient target, uint knightNetId, float health)
    {
        NetworkIdentity knightNi;
        if (NetworkClient.spawned.TryGetValue(knightNetId, out knightNi))
        {
            ApplyHealthAndRefreshUI(knightNi, health);
        }
    }

    [TargetRpc]
    private void TargetInvokeDeath(NetworkConnectionToClient target, uint knightNetId)
    {
        NetworkIdentity knightNi;
        if (NetworkClient.spawned.TryGetValue(knightNetId, out knightNi))
        {
            var hc = knightNi.GetComponent<HealthController>();
            if (hc == null) return;

            hc.Die();
        }
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
