using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para usar List
using UnityEngine.UI;



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
    private float duration = 4f;
    public float maxHealth; 
    
    public float Health
    {
        get { return health; }
        set { health = value; }
    }


    private Animator animator; // Declarar la variable Animator
    public float power=1f;
    private Sounds sounds;


    void Start()
    {
        // Inicializar el Animator obteniéndolo del mismo objeto
        animator = GetComponent<Animator>();
	live=true;
	iddle =false;
	health*=power;
	maxHealth=Mathf.Max(health, 0);
	lifeOfBar = transform.Find("Canvas/background/LifeBar").GetComponentInChildren<Image>();	
	y= transform.position.y;
	sounds = GetComponent<Sounds>();
    }

    void Update()
    {
	foreach (Transform enemy in enemies)
	{
            enemyHealth = enemy.GetComponent<HealthController>();

            // Verifica si el enemigo está derrotado y si aún no se ha activado "win" para él
            if (enemyHealth.Health <= 0 && health > 0 && live && !defeatedEnemies.Contains(enemy))
            {  
                animator.SetTrigger("win");
                defeatedEnemies.Add(enemy); // Marca el enemigo como derrotado
            }	    
    }
	//Debug.Log(defeatedEnemies.Count+ "/" + enemies.Count+"/"+health);
	if ((defeatedEnemies.Count == enemies.Count))
	{
	    animator.SetBool("WIN",true);
	}

	if(health<0 && iddle)
	{
	    animator.SetTrigger("death");
	}

	iddle=false;

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
	float distancePlus=0;

        foreach (Transform enemy in enemies)
        {
            // Verifica si el enemigo está dentro del ángulo especificado
            if (IsFacingEnemy(enemy, angleRange))
            {
                // Calcula la distancia al enemigo
		float enemyPower=enemy.GetComponent<HealthController>().power;
		if(enemyPower>power) distancePlus=(enemyPower-power);
                float distance = Vector3.Distance(transform.position, enemy.position)- distancePlus;

                // Verifica si el enemigo está dentro del rango de distancia y ángulo
                if (distance <= maxDistance)
                {
                    dangersEnemies.Add(enemy);
                }
            }
	    distancePlus=0;
        }

        return dangersEnemies;
    }

    private bool IsFacingEnemy(Transform enemy, float angleRange)
    {
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
	
	animator.SetTrigger("shieldReaction");
	sounds.ShieldSound();
	
            return;
        }

        // Si no tiene escudo, aplicar daño
        health -= damage;
        animator.SetFloat("health", health);
        Debug.Log(this.gameObject.name + ": Vida actual: " + health);

	LifeOfBar(); //Actualizar Barra de vida
	sounds.HurtSound();
        if (health <= 0)
        {
            Die();
	    return;
        }

	animator.SetTrigger("coupReaction");
    }


    private void Die()
    {
        Debug.Log(this.gameObject.name + " ha muerto.");
	animator.ResetTrigger("win");
	animator.SetTrigger("death");
	live=false;
	y=0;
    }

    private void finish()
    {
	animator.enabled = false;
	animator.SetTrigger("death");
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
	animator.ResetTrigger("win");
    }


    public void LifeOfBar()
    {
        lifeOfBar.fillAmount = health /  maxHealth;
    }


    public void ActivateShield()
    {
	StartCoroutine(TemporarilySetTrue(duration)); // Inicia la corutina al inicio
    }

    private IEnumerator TemporarilySetTrue(float seconds)
    {
	shield = true;
        yield return new WaitForSeconds(seconds); // Espera el tiempo especificado
        shield = false; // Cambia la variable a false
    }
}
