using UnityEngine;

public class KnightController2 : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private Vector3 center = Vector3.zero; // Centro del círculo en (0,0)

    // Variables de acción
    private bool isAttacking;
    private bool isJumping;
    private bool isDefending; 
    private bool isRunning;
    private bool isCrouching;
    private bool isAttacking2;
    private bool kick;
    private bool dodge;
    private bool isWalkingBackward;
    private bool isJoystickRight;
    private bool isJoystickLeft;
    private bool moveRight;
    private bool moveLeft;
    private bool anyButton;
    public bool showButtons=false;
    private bool hasShield=false;



    public Transform cameraTransform; // La cámara para obtener su dirección
    //private float movementSpeed = 1f; // Velocidad de movimiento
    private float rotationSpeed = 1f; // Velocidad de rotación

    // Variables para detectar el giro de 180 grados
    //private bool isTurningBack;
    private float previousHorizontal = 0;
    private float previousVertical = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }



    void Update()
    {
	if(!animator.enabled) return;
        HandleButtons();
        HandleMovement();
        DetectTurnBack();
	anyButton = true;
	if (isDefending==false) hasShield=false;


        // Iterar sobre los posibles botones de joystick (del 0 al 19)
        for (int i = 0; i <= 19; i++)
        {
            KeyCode button = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
            if (Input.GetKey(button))
            {
                if(showButtons) Debug.Log("Estás presionando: " + button);
		anyButton=false;
            }
        }
	anyButton=anyButton && !((Mathf.Abs(Input.GetAxis("Horizontal"))+Mathf.Abs(Input.GetAxis("Vertical")))>0.1);
	//Debug.Log("anyButton" + anyButton);
    }

    private void HandleButtons()
    {
        isAttacking = Input.GetKeyDown(KeyCode.JoystickButton3);
        isAttacking2 = Input.GetKeyDown(KeyCode.JoystickButton7);
        dodge = Input.GetKeyDown(KeyCode.JoystickButton6);
        isJumping = Input.GetKeyDown(KeyCode.JoystickButton1);
        isDefending = Input.GetKey(KeyCode.JoystickButton0);
        kick = Input.GetKey(KeyCode.JoystickButton2);
	moveRight = Input.GetKey(KeyCode.JoystickButton5); // Moverse lateralmente hacia la derecha
        moveLeft = Input.GetKey(KeyCode.JoystickButton4); // Moverse lateralmente hacia la izquierda

        isCrouching = Input.GetKey(KeyCode.JoystickButton10);

        if (isAttacking)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        if (isAttacking2)
        {
            animator.ResetTrigger("Attack2");
            animator.SetTrigger("Attack2");
        }

        if (dodge)
        {
            animator.ResetTrigger("dodge");
            animator.SetTrigger("dodge");
        }

        if (isJumping)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Jump");
        }

        if (kick)
        {
            animator.ResetTrigger("kick");
            animator.SetTrigger("kick");
        }

        animator.SetBool("isDefending", isDefending);
        animator.SetBool("Crouch", isCrouching);
	animator.SetBool("AnyButton", anyButton);

        // Activar animaciones laterales y orientar hacia el centro (0,0)

        animator.SetBool("MoveRight", moveRight);
        animator.SetBool("MoveLeft", moveLeft);

        // Orientar al personaje hacia el centro solo cuando alguna animación lateral esté activa
        if (moveRight || moveLeft)
        {
            OrientTowardsCenter();
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Actualizar los bools para verificar si el joystick está totalmente a la derecha o a la izquierda
        isJoystickRight = horizontal > 0.9f;
        isJoystickLeft = horizontal < -0.9f;

        animator.SetBool("isJoystickRight", isJoystickRight);
        animator.SetBool("isJoystickLeft", isJoystickLeft);

        // Solo moverse y rotar si hay entrada de movimiento
        if (horizontal != 0 || vertical != 0)
        {
            // Dirección del movimiento basada en la cámara
            Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
            direction = cameraTransform.TransformDirection(direction);
            direction.y = 0; // Mantener en el plano XZ

            // Determinar si el personaje se mueve hacia adelante o hacia atrás
            if (vertical > 0)
            {
                animator.SetBool("isRunning", true);  // Activar animación de correr hacia adelante
                animator.SetBool("isWalkingBackward", false);  // Desactivar animación de caminar hacia atrás
            }
            else if (vertical < 0)
            {
                animator.SetBool("isRunning", false);  // Desactivar animación de correr hacia adelante
                animator.SetBool("isWalkingBackward", true);  // Activar animación de caminar hacia atrás

                // Invertir la dirección al moverse hacia atrás
		if(!(isJoystickRight || isJoystickLeft)) direction = -direction;
            }

            // Rotar suavemente hacia la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Desactivar todas las animaciones de movimiento si no hay entrada de movimiento
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalkingBackward", false);
        }
    }

    private void DetectTurnBack()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        bool hasTurnedBackHorizontally = (previousHorizontal * horizontal) < 0;
        bool hasTurnedBackVertically = (previousVertical * vertical) < 0;

        if (hasTurnedBackHorizontally || hasTurnedBackVertically)
        {
            //isTurningBack = true;
            animator.SetTrigger("TurnBack"); // Activar el trigger en el Animator
        }
        else
        {
            //isTurningBack = false;
        }

        // Actualizar los valores anteriores
        previousHorizontal = horizontal;
        previousVertical = vertical;
    }

    private void OrientTowardsCenter()
    {
        // Calcular la dirección hacia el centro (0,0)
        Vector3 directionToCenter = center - transform.position;
        directionToCenter.y = 0; // Asegurarse de que solo se rote en el plano XZ

        // Rotar el personaje hacia el centro instantáneamente
        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        transform.rotation = targetRotation;
    }

    public void shieldActivate()
    {
	hasShield=true;
    }

    public bool HasShield(bool orientation)
    {
	return hasShield && orientation;
    }
}



