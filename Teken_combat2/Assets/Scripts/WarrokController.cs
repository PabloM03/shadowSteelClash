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

    // true cuando no hay red en absoluto (spawn offline local)
    private bool offlineMode;

    // La vida no viaja por NetworkAnimator, asi que se replica aparte: el dueño
    // la reporta al servidor y el SyncVar la reparte al resto. Empieza en -1
    // para distinguir "todavia no ha llegado nada" de una vida real de 0.
    [SyncVar(hook = nameof(OnHealthChanged))]
    private float syncedHealth = -1f;

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
                animator.SetInteger("attackType", 0);

                if (distanceToKnight < maxDistance && distanceToKnight > minDistance)
                {
                    animator.SetBool("run", true);
                }
                else
                {
                    animator.SetBool("run", false);
                    if (distanceToKnight < minDistance)
                    {
                        attackType = Random.Range(1, 7);
                        animator.SetInteger("attackType", attackType);
                    }
                }

                Vector3 directionToKnight = (knight.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(directionToKnight);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                animator.SetBool("run", false);
            }
        }

        ReportarVidaSiCambio();
    }

    // ---------- Sincronizacion ----------
    // Posicion y rotacion las lleva NetworkTransformHybrid.
    // Parametros y triggers del animator, NetworkAnimator.
    // Aqui solo queda la vida, que no cubre ninguno de los dos.

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
