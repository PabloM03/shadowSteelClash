using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class WarrokController : NetworkBehaviour
{
    public List<Transform> knights = new List<Transform>();
    private float minDistance = 1.7f;
    private float maxDistance = 8.5f;
    private float rotationSpeed = 2f;
    private Animator animator;
    private int attackType;
    private ParticleSystem myParticleSystem;
    private ParticleSystem fireAttack;
    private Transform knight;
    private HealthController healthController;

    // Los ataques salen de Idle segun attackType y vuelven a Idle al terminar la
    // animacion. Sorteamos uno nuevo justo al volver a Idle, de modo que cada
    // ataque dura lo que dure su animacion y el valor se mantiene estable
    // mientras tanto. Sortearlo en cada frame, como se hacia antes, cambiaba el
    // parametro mas rapido de lo que se replica y cada cliente acababa
    // reproduciendo un ataque distinto.
    [SerializeField] private string estadoIdle = "Mutant Idle";
    private bool estabaEnIdle;

    // true cuando no hay red en absoluto (spawn offline local)
    private bool offlineMode;

    // La vida no viaja por NetworkAnimator, asi que se replica aparte: el dueño
    // la reporta al servidor y el SyncVar la reparte al resto. Empieza en -1
    // para distinguir "todavia no ha llegado nada" de una vida real de 0.
    [SyncVar(hook = nameof(OnHealthChanged))]
    private float syncedHealth = -1f;

    // Sync manual de estado, restaurado tal como estaba en c8f885a. Convive con
    // NetworkTransformHybrid y NetworkAnimator, que cubren lo mismo por su cuenta.
    private const float estadoSyncInterval = 0.05f; // 20 Hz
    private float nextSyncTime;

    private const float healthSyncInterval = 0.2f; // 5 Hz: la vida cambia a golpes
    private float nextHealthSync;
    private float lastReportedHealth = float.NaN;

    void Start()
    {
        // Ojo con offlineMode: en un servidor dedicado NetworkClient.active es
        // false, asi que mirarlo a solas lo daba por offline y el servidor movia
        // al Warrok por su cuenta, divergiendo del cliente que tiene autoridad.
        NetworkIdentity ni = GetComponent<NetworkIdentity>();
        offlineMode = ni == null || (!NetworkServer.active && !NetworkClient.active);

        animator = GetComponent<Animator>();
        healthController = GetComponent<HealthController>();
        enabled = false;
        myParticleSystem = transform.Find("explosion").GetComponent<ParticleSystem>();
        fireAttack = GetComponentsInChildren<ParticleSystem>()[0];
        myParticleSystem.transform.localScale *= transform.localScale.x;
        fireAttack.transform.localScale *= transform.localScale.x;
        minDistance *= transform.localScale.x;
        maxDistance *= transform.localScale.x;
    }

    // Llamado desde Animation Event al inicio de la animación de entrada
    public void StartRotationLogic()
    {
        if (offlineMode || isOwned)
            enabled = true;
    }

    public void particleSystem()
    {
        myParticleSystem.Play();
    }

    public void FireAttack()
    {
        fireAttack.Play();
    }

    void Update()
    {
        // Solo el dueño (o modo offline) ejecuta la IA
        if (!offlineMode && !isOwned)
            return;

        if (knights.Count > 0)
        {
            knight = GetClosestKnight();
            if (knight != null && knight.gameObject.activeInHierarchy)
            {
                if (!animator.enabled) return;

                float distanceToKnight = Vector3.Distance(transform.position, knight.position);
                animator.SetFloat("distance", distanceToKnight);

                bool enRango = distanceToKnight < minDistance;

                if (distanceToKnight < maxDistance && distanceToKnight > minDistance)
                {
                    animator.SetBool("run", true);
                }
                else
                {
                    animator.SetBool("run", false);
                }

                bool enIdle = animator.GetCurrentAnimatorStateInfo(0).IsName(estadoIdle);

                if (enRango)
                {
                    // Se sortea al empezar y despues solo en el flanco de vuelta a
                    // Idle, que es cuando el ataque anterior ha terminado.
                    if (attackType == 0 || (enIdle && !estabaEnIdle))
                        attackType = Random.Range(1, 7);

                    animator.SetInteger("attackType", attackType);
                }
                else
                {
                    attackType = 0;
                    animator.SetInteger("attackType", 0);
                }

                estabaEnIdle = enIdle;

                Vector3 directionToKnight = (knight.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(directionToKnight);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                animator.SetBool("run", false);
                animator.SetInteger("attackType", 0);
                attackType = 0;
            }
        }

        // Enviar estado al servidor para que lo redistribuya a todos
        if (!offlineMode && isOwned && Time.time >= nextSyncTime)
        {
            nextSyncTime = Time.time + estadoSyncInterval;
            CmdSyncState(
                transform.position,
                transform.rotation,
                animator.GetBool("run"),
                animator.GetFloat("distance"),
                animator.GetInteger("attackType")
            );
        }

        ReportarVidaSiCambio();
    }

    [Command]
    private void CmdSyncState(Vector3 pos, Quaternion rot, bool run, float distance, int attackType)
    {
        RpcSyncState(pos, rot, run, distance, attackType);
    }

    [ClientRpc]
    private void RpcSyncState(Vector3 pos, Quaternion rot, bool run, float distance, int attackType)
    {
        if (isOwned) return; // el dueño ya tiene los valores correctos

        transform.position = pos;
        transform.rotation = rot;
        animator.SetBool("run", run);
        animator.SetFloat("distance", distance);
        animator.SetInteger("attackType", attackType);
    }

    // ---------- Sincronizacion ----------
    // La vida no la cubren ni NetworkTransformHybrid ni NetworkAnimator, asi
    // que va aparte por SyncVar.

    private void ReportarVidaSiCambio()
    {
        if (offlineMode || !isOwned || healthController == null) return;
        if (Time.time < nextHealthSync) return;

        nextHealthSync = Time.time + healthSyncInterval;

        float actual = healthController.Health;
        if (!float.IsNaN(lastReportedHealth) && Mathf.Approximately(actual, lastReportedHealth))
            return;

        lastReportedHealth = actual;
        CmdReportHealth(actual);
    }

    [Command]
    private void CmdReportHealth(float value)
    {
        syncedHealth = value;
    }

    private void OnHealthChanged(float anterior, float nueva)
    {
        // El dueño ya tiene el valor bueno; esto es solo para los demas.
        if (isOwned || nueva < 0f) return;

        // El hook puede llegar antes de que Start() haya corrido.
        if (healthController == null)
            healthController = GetComponent<HealthController>();

        if (healthController != null)
            healthController.SetHealthFromNetwork(nueva);
    }

    private Transform GetClosestKnight()
    {
        if (knights.Count == 0 || knights.All(k => k == null))
            return null;
        return knights
            .Where(k => k.GetComponent<HealthController>().Health > 0)
            .OrderBy(k => Vector3.Distance(transform.position, k.position))
            .FirstOrDefault();
    }
}
