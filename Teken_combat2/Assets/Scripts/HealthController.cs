using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para usar List
using UnityEngine.UI;
using Mirror;



public class HealthController : MonoBehaviour
{
    public List<Transform> enemies = new List<Transform>();
    private HashSet<Transform> defeatedEnemies = new HashSet<Transform>();
    [SerializeField] private float health = 100f; // Ahora visible en el Inspector
    HealthController enemyHealth;
    private Image lifeOfBar;
    private bool live;
    private bool iddle;
    private bool shield=false;
    private float y;
    public float maxHealth; 
    
    public float Health
    {
        get { return health; }
        set { health = value; }
    }


    private Animator animator; // Declarar la variable Animator
    private NetworkAnimator netAnimator; // opcional: solo si el objeto se replica
    public float power=1f;
    private Sounds sounds;


    void Start()
    {
        // Inicializar el Animator obteniéndolo del mismo objeto
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
        live = true;
        iddle = false;
        health *= power;
        maxHealth = Mathf.Max(health, 0);
        lifeOfBar = transform.Find("Canvas/background/LifeBar").GetComponentInChildren<Image>();
        y = transform.position.y;
        sounds = GetComponent<Sounds>();
        LifeOfBar();
    }

    void Update()
    {
        int enemiesCount = (enemies != null) ? enemies.Count : 0;

        if (enemies != null && enemies.Count > 0)
        {
            foreach (Transform enemy in enemies)
            {
                if (enemy == null) continue; // Saltar si el enemigo es nulo

                enemyHealth = enemy.GetComponent<HealthController>();
                if (enemyHealth == null) continue;

                // Verifica si el enemigo está derrotado y si aún no se ha activado "win" para él
                if (enemyHealth.Health <= 0 && health > 0 && live && !defeatedEnemies.Contains(enemy))
                {
                    FireTrigger("win");
                    defeatedEnemies.Add(enemy); // Marca el enemigo como derrotado
                }
            }
        }

        //Debug.Log(defeatedEnemies.Count + "/" + enemiesCount + "/" + health);

        //enemies.RemoveAll(e => e == null);

        //defeatedEnemies.RemoveWhere(e => e == null || !enemies.Contains(e));

        bool mandoYo = MandaLasAnimaciones();

        if (defeatedEnemies.Count == enemiesCount)
        {
            if (mandoYo) animator.SetBool("WIN", true);
            if (lifeOfBar != null && lifeOfBar.transform != null && lifeOfBar.transform.parent != null)
                lifeOfBar.transform.parent.transform.gameObject.SetActive(false);
        }
        else
        {
            if (mandoYo) animator.SetBool("WIN", false);
            if (live && lifeOfBar != null && lifeOfBar.transform != null && lifeOfBar.transform.parent != null)
                lifeOfBar.transform.parent.gameObject.SetActive(true);
        }

        if(health<0 && iddle)
        {
            FireTrigger("death");
        }

        iddle=false;

        if (health > 0 && !live)
        {
            // Intento de resucitar si la vida volvió a ser mayor que 0
            Resucitate();
        }

        if((health<=0 || !live) && animator.enabled == false) Die();
        
    }

    // Función que recibe un string con los valores de ataque
    public void ApplyAttack(string attackParams)
    {
	int type = 1;
	Debug.Log("Vida del "+ this.gameObject.name + ": " + health);
        // Divide el string en sus valores (ángulo, distancia, daño)
        string[] parameters = attackParams.Split('/');
        if (parameters.Length < 3) return;

        float angleRange = float.Parse(parameters[0]);
        float maxDistance = float.Parse(parameters[1]);
        float damage = float.Parse(parameters[2]);
	if (parameters.Length == 4) type = int.Parse(parameters[3]);

	if (power>1) maxDistance+=power;

        // Comprobar si el receptor está dentro del área de ataque
         List<Transform> dangersEnemies=IsTargetInRange(angleRange, maxDistance);
	if(dangersEnemies.Count>0) sounds.AttackSound(type);
       
	foreach (Transform enemy in dangersEnemies)
	{ 
	    enemyHealth = enemy.GetComponent<HealthController>();
            // Llama a HealthUpdate en el receptor si está en el rango de ataque 
            enemyHealth.HealthUpdate(damage*power,this.transform); // Aplica el daño al receptor
            
        }
    }

    private List<Transform> IsTargetInRange(float angleRange, float maxDistance)
    {
        List<Transform> dangersEnemies = new List<Transform>();
        float distancePlus = 0;

        if (enemies == null || enemies.Count == 0)
            return dangersEnemies;

        foreach (Transform enemy in enemies)
        {
            if (enemy == null) continue;

            // Verifica si el enemigo está dentro del ángulo especificado
            if (IsFacingEnemy(enemy, angleRange))
            {
                // Calcula la distancia al enemigo
                var enemyHc = enemy.GetComponent<HealthController>();
                float enemyPower = (enemyHc != null) ? enemyHc.power : 0f;
                if (enemyPower > power) distancePlus = (enemyPower - power);
                float distance = Vector3.Distance(transform.position, enemy.position) - distancePlus;

                // Verifica si el enemigo está dentro del rango de distancia y ángulo
                if (distance <= maxDistance)
                {
                    dangersEnemies.Add(enemy);
                }
            }
            distancePlus = 0;
        }

        return dangersEnemies;
    }

    private bool IsFacingEnemy(Transform enemy, float angleRange)
    {
        if (enemy == null) return false;

        // Dirección hacia el enemigo
        Vector3 directionToEnemy = (enemy.position - transform.position).normalized;

        // Calcula el ángulo entre el forward del personaje y la dirección hacia el enemigo
        float angle = Vector3.Angle(transform.forward, directionToEnemy);

        // Retorna true si el ángulo está dentro del rango especificado
        return angle <= angleRange;
    }





    public void HealthUpdate(float damage, Transform enemy)
    {
        UpdatePositionRelativeToEnemy(enemy);
        bool isFacingEnemy = IsFacingEnemy(enemy,30);
        
        // Comprobar si el objeto tiene un escudo activo
        bool hasShield = GetComponent<KnightController>()?.HasShield(isFacingEnemy) ?? false;

        // Si tiene escudo, no aplicamos el daño y salimos de la función
        if (hasShield || shield)
        {
	    Debug.Log(this.gameObject.name + " tiene escudo activo. No se aplicará daño. Vida: " + health);
	
        FireTrigger("shieldReaction");
        sounds.ShieldSound();
	
            return;
        }

        // Si no tiene escudo, aplicar daño
        health -= damage;
        animator.SetFloat("health", health);
        Debug.Log(this.gameObject.name + ": Vida actual: " + health);

        LifeOfBar(); //Actualizar Barra de vida
        sounds.HurtSound();
        if (health <= 0 || !live)
        {
            Die();
	    return;
        }

	    FireTrigger("coupReaction");
    }


    internal void Die()
    {
        // Update() llama aqui en cada frame mientras el personaje esta muerto, y
        // HealthUpdate tambien entra si le pegan a un cadaver. Antes daba igual
        // porque el trigger se quedaba en local sobre un animator desactivado,
        // pero ahora se replica por red: sin este guard la animacion de muerte
        // se repite en los demas clientes cada vez que golpean al muerto.
        if (!live) return;

        Debug.Log(this.gameObject.name + " ha muerto.");
        ClearTrigger("win");
        FireTrigger("death");
        live = false;
        y = 0;
        
        lifeOfBar.transform.parent.gameObject.SetActive(false);

        StartCoroutine(DeathAfterDelay());

        IEnumerator DeathAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            // Igualar la rotación X a 180º
            Vector3 rot = transform.eulerAngles;
            rot.x = 0f;
            transform.eulerAngles = rot;
            // Establece la altura a 0.4 si después de 4 segundos sigue por encima de 0.4
            if (transform.position.y > 0.4f)
            {
                Vector3 pos = transform.position;
                pos.y = 0.4f;
                transform.position = pos;
            }
            // Desactivar el collider para evitar más interacciones
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            // Desactivar rigidbody si existe
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
            // Desactivar la barra de vida
            lifeOfBar.transform.parent.gameObject.SetActive(false);

            // Desactivar NetworkTransformHybrid para evitar problemas de sincronización
            if (GetComponent<NetworkTransformHybrid>() != null)
            GetComponent<NetworkTransformHybrid>().enabled = false;
            ClearTrigger("resucitate");
        }
    }

    private void finish()
    {
        var netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isClient && netIdentity.isClientOnly && netIdentity.isLocalPlayer == false && netIdentity.isServer == false)
            return; // Si no es el jugador local, no ejecutar la animación de muerte
        animator.enabled = false;
        FireTrigger("death");
    }


    private void UpdatePositionRelativeToEnemy(Transform enemy)
    {
        Vector3 directionToEnemy = (enemy.position - transform.position).normalized;
        float angle = Vector3.SignedAngle(transform.forward, directionToEnemy, Vector3.up);

        // Determinar el cuadrante en el que se encuentra el enemigo
        if (angle > -45 && angle <= 45)
        {
            animator.SetInteger("Position", 1); // Enemigo delante
        }
        else if (angle > 45 && angle <= 135)
        {
            animator.SetInteger("Position", 4); // Enemigo a la derecha
        }
        else if (angle > 135 || angle <= -135)
        {
            animator.SetInteger("Position", 3); // Enemigo detrás
        }
        else if (angle > -135 && angle <= -45)
        {
            animator.SetInteger("Position", 2); // Enemigo a la izquierda
        }
    }

    public void deathAnimationUpdate()
    {
	iddle = true;
    }

    public void ControllerLayer(int layerIndex)
    {
        if (animator != null && layerIndex < animator.layerCount && !live)
        {
            animator.SetLayerWeight(layerIndex, 0); // desactiva la capa estableciendo su peso en 0
        }
    }

    public void ResetWin()
    {
	ClearTrigger("win");
    }

    // Resucitar si la vida es mayor que 0 y actualmente está marcado como muerto (live == false)
    public void Resucitate()
    {
        //imprime la vida actual y el estado de live para depuración
        Debug.Log("Resucitate Check - Vida: " + health + ", live: " + live);
        //lanza un bool para resucitar animacion
        
        ClearTrigger("death");
        FireTrigger("resucitate");
        // Reactivar NetworkTransformHybrid para la sincronización
        if (GetComponent<NetworkTransformHybrid>() != null)
        GetComponent<NetworkTransformHybrid>().enabled = true;

        // Reactivar animator
        if (animator != null)
        {
            animator.enabled = true;
            ClearTrigger("death");
        }

        // Reactivar barra de vida (Canvas/background)
        if (lifeOfBar != null && lifeOfBar.transform.parent != null)
        {
            lifeOfBar.transform.parent.gameObject.SetActive(true);
        }

        // Reactivar collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // Reactivar rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Reset flags
        live = true;
        iddle = false;

        // Actualizar UI y animator con la nueva vida
        LifeOfBar();
        if (animator != null)
        {
            animator.SetFloat("health", health);
        }

        //Reactivar network transform
        if (GetComponent<NetworkTransformHybrid>() != null)
        GetComponent<NetworkTransformHybrid>().enabled = true;

    }


    public void LifeOfBar()
    {
        lifeOfBar.fillAmount = health /  maxHealth;
    }


    // Aplica la vida que llega replicada desde el dueño del personaje.
    // No reproduce sonidos ni reacciones: los triggers del animator ya viajan
    // por NetworkAnimator, y duplicarlos aqui los dispararia dos veces.
    public void SetHealthFromNetwork(float value)
    {
        if (Mathf.Approximately(health, value)) return;

        health = value;

        if (animator != null)
            animator.SetFloat("health", health);

        // Start() puede no haber corrido aun cuando llega el primer valor.
        if (lifeOfBar != null && maxHealth > 0f)
            LifeOfBar();

        // La muerte no es solo animacion: tambien apaga collider, rigidbody y
        // barra de vida, y eso hay que hacerlo en cada maquina.
        if (health <= 0f && live && animator != null)
            Die();
    }


    public void ActivateShield(float duration)
    {
	    StartCoroutine(TemporarilySetTrue(duration)); // Inicia la corutina al inicio
    }

    private IEnumerator TemporarilySetTrue(float seconds)
    {
	shield = true;
        yield return new WaitForSeconds(seconds); // Espera el tiempo especificado
        shield = false; // Cambia la variable a false
    }

    // Mirror NO replica los triggers del Animator por su cuenta: hay que
    // dispararlos a traves de NetworkAnimator. Los parametros bool, float e int
    // si viajan solos, y por eso el Knight (que solo usa SetBool) se veia bien
    // mientras estas reacciones se quedaban en local.
    // Si no hay red, o si no somos el cliente dueño, se dispara en local: el no
    // dueño lo recibira replicado desde quien tiene la autoridad.
    private void FireTrigger(string triggerName)
    {
        // Quien no manda no dispara nada: le llegara replicado. Si lo disparase
        // aqui ademas, cada instancia animaria por su cuenta y se duplicaria.
        if (!MandaLasAnimaciones()) return;

        if (PuedeReplicarTriggers())
            netAnimator.SetTrigger(triggerName);
        else
            animator.SetTrigger(triggerName);
    }

    private void ClearTrigger(string triggerName)
    {
        if (!MandaLasAnimaciones()) return;

        if (PuedeReplicarTriggers())
            netAnimator.ResetTrigger(triggerName);
        else
            animator.ResetTrigger(triggerName);
    }

    // Solo la instancia con autoridad decide las animaciones; las demas las
    // reciben. Sin red manda siempre la local. En el servidor dedicado tampoco
    // manda, porque la autoridad la tiene el cliente dueno.
    private bool MandaLasAnimaciones()
    {
        if (netAnimator == null) return true;
        if (!NetworkClient.active && !NetworkServer.active) return true;
        return netAnimator.isOwned;
    }

    // NetworkAnimator con clientAuthority solo acepta triggers del cliente
    // dueño; en cualquier otro caso avisa por consola y los descarta.
    private bool PuedeReplicarTriggers()
    {
        return netAnimator != null && NetworkClient.active && netAnimator.isOwned;
    }
}
