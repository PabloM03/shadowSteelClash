using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

// Solo el dueño ejecuta la IA. Todo lo que decide llega al resto por los dos
// componentes de red del prefab: NetworkTransformHybrid (posicion y rotacion) y
// NetworkAnimator (run, distance, attackType y los triggers). La vida va por
// HealthSync. Aqui ya no hay ningun sync manual: el CmdSyncState que habia
// escribia posicion y parametros encima de lo que esos componentes ya
// replicaban, y los dos sistemas se pisaban.
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

    // Los ataques salen de Idle segun attackType y vuelven a Idle al terminar la
    // animacion. Se sortea uno nuevo justo al volver a Idle, de modo que cada
    // ataque dura lo que dure su animacion y el valor se mantiene estable
    // mientras tanto. Sortearlo en cada frame cambiaba el parametro mas rapido
    // de lo que se replica y cada cliente reproducia un ataque distinto.
    [SerializeField] private string estadoIdle = "Mutant Idle";
    private bool estabaEnIdle;

    // true cuando no hay red en absoluto (spawn offline local)
    private bool offlineMode;

    void Start()
    {
        // En un servidor dedicado NetworkClient.active es false: mirarlo a solas
        // lo daba por offline y el servidor movia al Warrok por su cuenta.
        NetworkIdentity ni = GetComponent<NetworkIdentity>();
        offlineMode = ni == null || (!NetworkServer.active && !NetworkClient.active);

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
    }

    private Transform GetClosestKnight()
    {
        if (knights.Count == 0 || knights.All(k => k == null))
            return null;
        return knights
            .Where(k => k != null && k.GetComponent<HealthController>().Health > 0)
            .OrderBy(k => Vector3.Distance(transform.position, k.position))
            .FirstOrDefault();
    }
}
