using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WarrokController : MonoBehaviour
{
    public List<Transform> knights = new List<Transform>(); // Lista de caballeros
    private float minDistance = 1.7f; // Distancia a la que el enemigo detecta al caballero
    private float maxDistance = 8.5f; // Distancia a la que el enemigo deja de detectar al caballero
    private float rotationSpeed = 2f; // Velocidad a la que el enemigo rota hacia el caballero
    //private float angleMargin = 30f; // Margen de error en grados para considerar que está orientado
    private Animator animator;
    private int attackType; // Variable para seleccionar la animación de ataque
    private ParticleSystem myParticleSystem; // Sistema de particulas
    private ParticleSystem fireAttack; // Sistema de particulas
    private Transform knight;



    void Start()
    {
        animator = GetComponent<Animator>();
        enabled = false; // Desactiva el script al inicio;
	myParticleSystem = transform.Find("explosion").GetComponent<ParticleSystem>();
	fireAttack = GetComponentsInChildren<ParticleSystem>()[0];
	myParticleSystem.transform.localScale*= transform.localScale.x;
	fireAttack.transform.localScale*= transform.localScale.x;
	minDistance*= transform.localScale.x;
	maxDistance*= transform.localScale.x;
    }

    // Este método será llamado por el Animation Event al inicio de la animación
    public void StartRotationLogic()
    {
        enabled = true; // Habilita el script para que Update se ejecute
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
	knight = GetClosestKnight();
	if(knight != null && knight.gameObject.activeInHierarchy)
	{
	    if(!animator.enabled)return;

            // Calcula la distancia entre el enemigo y el caballero
            float distanceToKnight = Vector3.Distance(transform.position, knight.position);
            //animator.SetBool("turnLeft", false);
            //animator.SetBool("turnRight", false);
            animator.SetInteger("attackType", 0); // Restablece el trigger específico del ataque
	    animator.SetFloat("distance", distanceToKnight);

            // Si la distancia está entre minDistance y maxDistance, activar el modo "run"
            if ((distanceToKnight < maxDistance) && (distanceToKnight > minDistance))
            {
                // Activa el bool "run" en el Animator
                animator.SetBool("run", true);
            }
            else
            {
                // Desactiva el bool "run" en el Animator
                animator.SetBool("run", false);

                if (distanceToKnight < minDistance)
                {
	            // Asigna un número aleatorio entre 1 y 5 a attackType al inicio de cada Update
                    attackType = Random.Range(1, 7);
                    animator.SetInteger("attackType", attackType); // Activa el trigger específico del ataque
                }
            }

            // Calcula la dirección hacia el caballero
            Vector3 directionToKnight = (knight.position - transform.position).normalized;

            // Calcula la rotación hacia el caballero
            Quaternion lookRotation = Quaternion.LookRotation(directionToKnight);

            // Suaviza la rotación hacia el caballero
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    	}
	else
	{
	    animator.SetBool("run", false);
	}
    }

    private Transform GetClosestKnight()
    {
        // Filtra los caballeros con Health > 0 y luego encuentra el más cercano usando LINQ
        return knights
            .Where(knight => knight.GetComponent<HealthController>().Health > 0) // Filtra caballeros con Health > 0
            .OrderBy(knight => Vector3.Distance(transform.position, knight.position)) // Ordena por distancia
            .FirstOrDefault(); // Obtiene el primero (el más cercano) o null si la lista está vacía
    }
}
