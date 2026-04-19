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

    // true cuando no hay NetworkIdentity (spawn offline local)
    private bool offlineMode;
    private bool wasInAttackRange;

    private const float syncInterval = 0.05f; // 20 Hz
    private float nextSyncTime;

    void Start()
    {
        offlineMode = GetComponent<NetworkIdentity>() == null;
        animator = GetComponent<Animator>();
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
                animator.SetInteger("attackType", 0);
                animator.SetFloat("distance", distanceToKnight);

                if (distanceToKnight < maxDistance && distanceToKnight > minDistance)
                {
                    animator.SetBool("run", true);
                    wasInAttackRange = false;
                }
                else
                {
                    animator.SetBool("run", false);
                    if (distanceToKnight < minDistance)
                    {
                        if (!wasInAttackRange)
                        {
                            wasInAttackRange = true;
                            attackType = Random.Range(1, 7);
                            animator.SetInteger("attackType", attackType);
                        }
                    }
                    else
                    {
                        wasInAttackRange = false;
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

        // Enviar estado al servidor para que lo redistribuya a todos
        if (!offlineMode && isOwned && Time.time >= nextSyncTime)
        {
            nextSyncTime = Time.time + syncInterval;
            CmdSyncState(
                transform.position,
                transform.rotation,
                animator.GetBool("run"),
                animator.GetFloat("distance"),
                animator.GetInteger("attackType")
            );
        }
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
