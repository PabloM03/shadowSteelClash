using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Animations;

public class KnightController : NetworkBehaviour
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
    private bool anyButton = false;
    public bool showButtons = false;
    private bool hasShield = false;
    private bool TurnBack = false;

    public Transform cameraTransform; // La cámara para obtener su dirección
    private float rotationSpeed = 1f; // Velocidad de rotación

    private float previousHorizontal = 0;
    private float previousVertical = 0;

    private Coroutine triggerRoutine;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (!GetComponent<online>())
        {
            Camera.main.GetComponent<CameraFollow>().target = transform;
        }

        LookAtConstraint lookAt = GetComponentInChildren<LookAtConstraint>();
        ConstraintSource source = new ConstraintSource();
        source.sourceTransform = cameraTransform;
        source.weight = 1;
        lookAt.SetSource(0, source);  
        
    }

    void Update()
    {
        if (!(GetComponent<NetworkIdentity>() && (!(isLocalPlayer || (isServer && isLocalPlayer)))))
        {
            anyButton = false;
            animator.ResetTrigger("kick");
            if (!animator.enabled) return;
            HandleButtons();
            HandleMovement();
            DetectTurnBack();
            if (!isDefending) hasShield = false;
        }
        else
        {
            return;
            FireAllTriggersFromBools();
        }
    }

    private void HandleButtons()
    {
        isAttacking = Input.GetKeyDown(KeyCode.Alpha1);
        isAttacking2 = Input.GetKeyDown(KeyCode.Alpha2);
        dodge = Input.GetKeyDown(KeyCode.Q);
        isJumping = Input.GetKeyDown(KeyCode.Space);
        isDefending = Input.GetKey(KeyCode.W);
        kick = Input.GetKey(KeyCode.E);
        moveRight = Input.GetKey(KeyCode.D);
        moveLeft = Input.GetKey(KeyCode.A);
        isCrouching = Input.GetKey(KeyCode.S);

        if (isAttacking)
        {
            TriggerBool("AttackBool");
            anyButton = true;
        }

        if (isAttacking2)
        {
            TriggerBool("Attack2Bool");
            anyButton = true;
        }

        if (isJumping)
        {
            TriggerBool("JumpBool");
            anyButton = true;
        }

        if (dodge)
        {
            TriggerBool("dodgeBool");
            anyButton = true;
        }

        if (kick)
        {
            TriggerBool("kickBool");
            anyButton = true;
        }

        animator.SetBool("isDefending", isDefending);
        animator.SetBool("Crouch", isCrouching);
        animator.SetBool("MoveRight", moveRight);
        animator.SetBool("MoveLeft", moveLeft);

        if (isDefending || isCrouching || moveRight || moveLeft || ((Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"))) > 0.1) || isAttacking || isAttacking2 || isJumping || dodge || kick || TurnBack)
            anyButton = true;

        animator.SetBool("AnyButton", anyButton);

        if (moveRight || moveLeft)
        {
            OrientTowardsCenter();
        }
    }

    private void TriggerBool(string boolName, float duration = 0.1f)
    {
        if (triggerRoutine != null)
            StopCoroutine(triggerRoutine);

        triggerRoutine = StartCoroutine(TriggerBoolCoroutine(boolName, duration));
    }

    private IEnumerator TriggerBoolCoroutine(string boolName, float duration)
    {
        animator.SetBool(boolName, true);
        yield return new WaitForSeconds(duration);
        animator.SetBool(boolName, false);
    }

    private void FireAllTriggersFromBools()
    {
        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter param = animator.GetParameter(i);

            if (param.type == AnimatorControllerParameterType.Bool && param.name.EndsWith("Bool"))
            {
                bool value = animator.GetBool(param.name);

                if (value)
                {
                    string triggerName = param.name.Substring(0, param.name.Length - 4);
                    animator.SetTrigger(triggerName);
                    Debug.Log("Triggering: " + param.name);
                    Debug.Log("Triggering: " + param.name);
                }
            }
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetKey(KeyCode.RightArrow) ? 1 : Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
        float vertical = Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        isJoystickRight = horizontal > 0.9f;
        isJoystickLeft = horizontal < -0.9f;

        animator.SetBool("isJoystickRight", isJoystickRight);
        animator.SetBool("isJoystickLeft", isJoystickLeft);

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
            direction = cameraTransform.TransformDirection(direction);
            direction.y = 0;

            if (vertical > 0)
            {
                isRunning = true;
                isWalkingBackward = false;
            }
            else if (vertical < 0)
            {
                isRunning = false;
                isWalkingBackward = true;
                if (!(isJoystickRight || isJoystickLeft)) direction = -direction;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            isRunning = false;
            isWalkingBackward = false;
        }

        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalkingBackward", isWalkingBackward);
    }

    private void DetectTurnBack()
    {
        float horizontal = Input.GetKey(KeyCode.RightArrow) ? 1 : Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
        float vertical = Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        bool hasTurnedBackHorizontally = (previousHorizontal * horizontal) < 0;
        bool hasTurnedBackVertically = (previousVertical * vertical) < 0;

        TurnBack = hasTurnedBackHorizontally || hasTurnedBackVertically;
        animator.SetBool("TurnBack", TurnBack);

        previousHorizontal = horizontal;
        previousVertical = vertical;
    }

    private void OrientTowardsCenter()
    {
        Vector3 directionToCenter = center - transform.position;
        directionToCenter.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        transform.rotation = targetRotation;
    }

    public void shieldActivate()
    {
        hasShield = true;
    }

    public bool HasShield(bool orientation)
    {
        return hasShield && orientation;
    }
}
