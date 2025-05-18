using Mirror;
using UnityEngine;

public class online : NetworkBehaviour
{
    // Variables de acción
    /*private bool isAttacking;
    public void SetIsAttacking(bool value) { isAttacking = value; }
    private bool isJumping;
    public void SetIsJumping(bool value) { isJumping = value; }
    private bool isDefending;
    public void SetIsDefending(bool value) { isDefending = value; }
    private bool isRunning;
    public void SetIsRunning(bool value) { isRunning = value; }
    private bool isCrouching;
    public void SetIsCrouching(bool value) { isCrouching = value; }
    private bool isAttacking2;
    public void SetIsAttacking2(bool value) { isAttacking2 = value; }
    private bool kick;
    public void SetKick(bool value) { kick = value; }
    private bool dodge;
    public void SetDodge(bool value) { dodge = value; }
    private bool isWalkingBackward;
    public void SetIsWalkingBackward(bool value) { isWalkingBackward = value; }
    private bool isJoystickRight;
    public void SetIsJoystickRight(bool value) { isJoystickRight = value; }
    private bool isJoystickLeft;
    public void SetIsJoystickLeft(bool value) { isJoystickLeft = value; }
    private bool moveRight;
    public void SetMoveRight(bool value) { moveRight = value; }
    private bool moveLeft;
    public void SetMoveLeft(bool value) { moveLeft = value; }
    private bool anyButton = false;
    public void SetAnyButton(bool value) { anyButton = value; }
    private bool hasShield = false;
    public void SetHasShield(bool value) { hasShield = value; }
    private bool TurnBack;
    public void SetTurnBack(bool value) { TurnBack = value; }
    private float health;
    public void SetHealth(float value) { health = value; }
    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Quaternion syncedRotation;
    [SyncVar] private bool syncedIsAttacking;
    [SyncVar] private bool syncedIsJumping;
    [SyncVar] private bool syncedIsDefending;
    [SyncVar] private bool syncedIsRunning;
    [SyncVar] private bool syncedIsCrouching;
    [SyncVar] private bool syncedIsAttacking2;
    [SyncVar] private bool syncedKick;
    [SyncVar] private bool syncedDodge;
    [SyncVar] private bool syncedIsWalkingBackward;
    [SyncVar] private bool syncedIsJoystickRight;
    [SyncVar] private bool syncedIsJoystickLeft;
    [SyncVar] private bool syncedMoveRight;
    [SyncVar] private bool syncedMoveLeft;
    [SyncVar] private bool syncedAnyButton;
    [SyncVar] private bool syncedHasShield;*/

    private Animator animator;
    private void Start()
    {
        if (!isLocalPlayer)
        {
            GetComponent<KnightController>().enabled = false;
        }
        
        if (isLocalPlayer)
        {
            GetComponent<KnightController>().cameraTransform = Camera.main.transform;
            Camera.main.GetComponent<CameraFollow>().target = transform;
        }

        if (!isLocalPlayer && !isServer)
        {
            GetComponent<Animator>().applyRootMotion = false;
        }

        animator = GetComponent<Animator>();
    }

    /*private void Update()
    {
        if (isLocalPlayer) // Solo el cliente local envía su posición y rotación al servidor
        {
            //CmdUpdatePosition(transform.position, transform.rotation);
            CmdUpdateAnimations(isAttacking, isJumping, isDefending, isRunning, isCrouching, isAttacking2, kick, dodge, isWalkingBackward, isJoystickRight, isJoystickLeft, moveRight, moveLeft, anyButton, hasShield);
        }
        else // Los demás clientes reciben y aplican la posición, rotación y animaciones sincronizadas
        {
            //transform.position = syncedPosition;
            //transform.rotation = syncedRotation;
            isAttacking = syncedIsAttacking;
            isJumping = syncedIsJumping;
            isDefending = syncedIsDefending;
            isRunning = syncedIsRunning;
            isCrouching = syncedIsCrouching;
            isAttacking2 = syncedIsAttacking2;
            kick = syncedKick;
            dodge = syncedDodge;
            isWalkingBackward = syncedIsWalkingBackward;
            isJoystickRight = syncedIsJoystickRight;
            isJoystickLeft = syncedIsJoystickLeft;
            moveRight = syncedMoveRight;
            moveLeft = syncedMoveLeft;
            anyButton = syncedAnyButton;
            hasShield = syncedHasShield;
        }
    }

    /*[Command] // Enviar posición y rotación al servidor
    private void CmdUpdatePosition(Vector3 position, Quaternion rotation)
    {
        syncedPosition = position;
        syncedRotation = rotation;
    }*/
    /*
    [Command] // Enviar animaciones al servidor
    private void CmdUpdateAnimations(bool isAttacking, bool isJumping, bool isDefending, bool isRunning, bool isCrouching, bool isAttacking2, bool kick, bool dodge, bool isWalkingBackward, bool isJoystickRight, bool isJoystickLeft, bool moveRight, bool moveLeft, bool anyButton, bool hasShield)
    {
        syncedIsAttacking = isAttacking;
        syncedIsJumping = isJumping;
        syncedIsDefending = isDefending;
        syncedIsRunning = isRunning;
        syncedIsCrouching = isCrouching;
        syncedIsAttacking2 = isAttacking2;
        syncedKick = kick;
        syncedDodge = dodge;
        syncedIsWalkingBackward = isWalkingBackward;
        syncedIsJoystickRight = isJoystickRight;
        syncedIsJoystickLeft = isJoystickLeft;
        syncedMoveRight = moveRight;
        syncedMoveLeft = moveLeft;
        syncedAnyButton = anyButton;
        syncedHasShield = hasShield;

        UpdateAnimations();
        RpcUpdateAnimations(isAttacking, isJumping, isDefending, isRunning, isCrouching, isAttacking2, kick, dodge, isWalkingBackward, isJoystickRight, isJoystickLeft, moveRight, moveLeft, anyButton, hasShield);
    }
    

    //[ClientRpc] // Aplicar animaciones a todos los clientes
    private void RpcUpdateAnimations(bool isAttacking, bool isJumping, bool isDefending, bool isRunning, bool isCrouching, bool isAttacking2, bool kick, bool dodge, bool isWalkingBackward, bool isJoystickRight, bool isJoystickLeft, bool moveRight, bool moveLeft, bool anyButton, bool hasShield)
    {
        //if (isLocalPlayer) return;

        this.isAttacking = isAttacking;
        this.isJumping = isJumping;
        this.isDefending = isDefending;
        this.isRunning = isRunning;
        this.isCrouching = isCrouching;
        this.isAttacking2 = isAttacking2;
        this.kick = kick;
        this.dodge = dodge;
        this.isWalkingBackward = isWalkingBackward;
        this.isJoystickRight = isJoystickRight;
        this.isJoystickLeft = isJoystickLeft;
        this.moveRight = moveRight;
        this.moveLeft = moveLeft;
        this.anyButton = anyButton;
        this.hasShield = hasShield;

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
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
        animator.SetBool("MoveRight", moveRight);
        animator.SetBool("MoveLeft", moveLeft);     
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalkingBackward", isWalkingBackward);
        animator.SetBool("isJoystickRight", isJoystickRight);
        animator.SetBool("isJoystickLeft", isJoystickLeft);
        animator.SetBool("AnyButton", anyButton);
    }*/
}
