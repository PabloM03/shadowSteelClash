using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Animations;
using UnityEngine.UI;

/// <summary>
/// KnightController (clean merge + retro-compat cameraTransform)
/// - Mantiene la lógica original de la V1 (teclado) y añade la UI/joystick de la V2.
/// - Unifica entradas: teclado y canvas provocan exactamente los mismos flags/animaciones.
/// - Conserva giros alrededor del centro con A/D (y botones TurnLeft/Right) y el movimiento/orientación con flechas (y joystick).
/// - Añadido: public Transform cameraTransform para no romper referencias externas (online.cs).
/// </summary>
public class KnightController : NetworkBehaviour
{
    [Header("Core")]
    private Animator animator;
    private Rigidbody rb;

    // ⚠️ Retro-compatibilidad: otros scripts acceden a este campo.
    [Tooltip("Referencia a la cámara usada para orientar al personaje. Campo expuesto por compatibilidad.")]
    public Transform cameraTransform;

    // Interno (alias de cameraTransform)
    private Transform cam;

    private Vector3 arenaCenter = Vector3.zero;

    [Header("Rotation")]
    [Tooltip("Velocidad de rotación en grados/segundo (yaw only).")]
    private float rotationSpeed = 150f;

    // --------- Estado de acciones ----------
    private bool anyButton;
    [SerializeField] private bool hasShield;
    private bool turnBack;
    private bool isRunning;
    private bool isWalkingBackward;
    private bool isJoystickRight;
    private bool isJoystickLeft;

    // Memoria de inputs para detectar cambio de sentido (como V1)
    private float prevH, prevV;

    // Corrutina para pulsos de bool->false
    private Coroutine triggerRoutine;

    [Header("UI (opcional)")]
    public Button attack1Button;
    public Button attack2Button;
    public Button jumpButton;
    public Button dodgeButton;
    public Button dodgeButton2;
    public Button kickButton;
    public Button crouchButton;     // requiere UIButtonHold para hold
    public Button turnRightButton;  // requiere UIButtonHold para hold
    public Button turnLeftButton;   // requiere UIButtonHold para hold
    public Button shieldButton;     // requiere UIButtonHold para hold
    public Button lanchWarrokButton;// placeholder
    public VirtualJoystick joystick; // opcional

    // Componentes hold (si existen)
    private UIButtonHold crouchHold;
    private UIButtonHold turnRightHold;
    private UIButtonHold turnLeftHold;
    private UIButtonHold shieldHold;

    [Header("Debug")]
    public bool showButtons = false;

    // ⚙️ Zona muerta (dead zone)
    [SerializeField] private float deadZone = 0.25f; // valor mínimo antes de empezar a moverse
    [SerializeField] private float sensitivity = 0.6f; // reduce la sensibilidad general del movimiento

    // --------- Estructura de entradas unificadas ----------
    private struct InputState
    {
        // Pulsos/Triggers
        public bool attack1;
        public bool attack2;
        public bool jump;
        public bool dodge;
        public bool dodge2;
        public bool kick;

        // Holds / Bools
        public bool defend;   // escudo
        public bool crouch;
        public bool turnRightAroundCenter;
        public bool turnLeftAroundCenter;

        // Movimiento/orientación (analog)
        public float axisH; // Horizontal (flechas / joystick)
        public float axisV; // Vertical   (flechas / joystick)
    }

    // --------- Ciclo de vida ----------
    private void Start()
    {
        // Si no está asignada por Inspector, tomar la principal
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Mantener alias interno sincronizado
        cam = cameraTransform;

        var netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isClient && netIdentity.isClientOnly && netIdentity.isLocalPlayer == false && netIdentity.isServer == false)
            return;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Seguir al jugador en modo offline (mismo comportamiento que V1)
        if (!GetComponent<online>() && Camera.main)
        {
            var cf = Camera.main.GetComponent<CameraFollow>();
            if (cf) cf.target = transform;
        }

        // LookAtConstraint como en V1
        var lookAt = GetComponentInChildren<LookAtConstraint>();
        if (lookAt && cameraTransform)
        {
            var source = new ConstraintSource { sourceTransform = cameraTransform, weight = 1f };
            lookAt.SetSource(0, source);
        }

        // Intentar tomar bindings desde ButtonBindings si no fueron asignados
        TryWireUIFromSingleton();

        // Suscribir triggers de click (no-hold)
        if (attack1Button) attack1Button.onClick.AddListener(() => { TriggerBool("AttackBool"); anyButton = true; });
        if (attack2Button) attack2Button.onClick.AddListener(() => { TriggerBool("Attack2Bool"); anyButton = true; });
        if (jumpButton)    jumpButton.onClick.AddListener(() => { TriggerBool("JumpBool");   anyButton = true; });
        if (dodgeButton)   dodgeButton.onClick.AddListener(() => { TriggerBool("dodgeBool"); anyButton = true; });
        if (dodgeButton2)  dodgeButton2.onClick.AddListener(() => { TriggerBool("dodge2Bool"); anyButton = true; });
        if (kickButton)    kickButton.onClick.AddListener(() => { TriggerBool("kickBool");   anyButton = true; });
        if (lanchWarrokButton) lanchWarrokButton.onClick.AddListener(() => { anyButton = true; /* placeholder */ });

        // Capturar holds (si existen)
        crouchHold    = crouchButton    ? crouchButton.GetComponent<UIButtonHold>()    : null;
        turnRightHold = turnRightButton ? turnRightButton.GetComponent<UIButtonHold>() : null;
        turnLeftHold  = turnLeftButton  ? turnLeftButton.GetComponent<UIButtonHold>()  : null;
        shieldHold    = shieldButton    ? shieldButton.GetComponent<UIButtonHold>()    : null;
        if (crouchButton && !crouchHold)       Debug.LogWarning("crouchButton requiere UIButtonHold para funcionar como hold.");
        if (turnRightButton && !turnRightHold) Debug.LogWarning("turnRightButton requiere UIButtonHold para funcionar como hold.");
        if (turnLeftButton && !turnLeftHold)   Debug.LogWarning("turnLeftButton requiere UIButtonHold para funcionar como hold.");
        if (shieldButton && !shieldHold) Debug.LogWarning("shieldButton requiere UIButtonHold para funcionar como hold.");
    
    }

    private void Update()
    {
        // Mantener alias por si se re-asigna cameraTransform en runtime
        if (cam != cameraTransform) cam = cameraTransform;

        if (!IsControllable())
            return;

        if (!animator || !animator.enabled)
            return;

        anyButton = false;
        animator.ResetTrigger("kick"); // compat legado

        // 1) Leer entradas unificadas (teclado + UI)
        var input = ReadUnifiedInput();

        // 2) Animaciones de triggers como en V1
        ApplyTriggers(input);

        // 3) Bools de animación (defensa, crouch, laterales que giran alrededor del centro)
        ApplyBools(input);

        // 4) Movimiento/orientación (flechas / joystick) con misma semántica de V1
        HandleMovementLikeV1(input);

        // 5) TurnBack (como V1)
        DetectTurnBack(input.axisH, input.axisV);

        // 6) AnyButton
        animator.SetBool("AnyButton", anyButton);

        if (showButtons && (input.attack1 || input.attack2))
            Debug.Log("Botón de ataque activado");
    }

    // --------- Helpers principales ----------
    private bool IsControllable()
    {
        // Igualar comportamiento original: si no hay NetworkIdentity -> se controla
        // Si hay NetworkIdentity, sólo el local player controla
        var ni = GetComponent<NetworkIdentity>();
        if (!ni) return true;
        return isLocalPlayer || (isServer && isLocalPlayer);
    }

    private InputState ReadUnifiedInput()
    {
        InputState s = default;

        // --- Teclado (igual que V1) ---
        s.attack1 = Input.GetKeyDown(KeyCode.Alpha1);
        s.attack2 = Input.GetKeyDown(KeyCode.Alpha2);
        s.dodge = Input.GetKeyDown(KeyCode.Q);
        s.dodge2 = Input.GetKeyDown(KeyCode.R);

        s.jump    = Input.GetKeyDown(KeyCode.Space);
        s.kick    = Input.GetKey(KeyCode.E);

        // Defensa (escudo) y crouch por teclado
        bool kbDefend = Input.GetKey(KeyCode.W);
        bool kbCrouch = Input.GetKey(KeyCode.S);

        // Giros alrededor del centro (A/D)
        bool kbTurnRight = Input.GetKey(KeyCode.D);
        bool kbTurnLeft  = Input.GetKey(KeyCode.A);

        // Ejes de movimiento/orientación con flechas (como V1)
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) h =  1f;
        else if (Input.GetKey(KeyCode.LeftArrow))  h = -1f;

        if (Input.GetKey(KeyCode.UpArrow))    v =  1f;
        else if (Input.GetKey(KeyCode.DownArrow))  v = -1f;

        // --- UI (Canvas) ---
        // Holds desde botones (si existen)
        bool uiCrouch = crouchHold && crouchHold.isHeld;
        bool uiTurnR  = turnRightHold && turnRightHold.isHeld;
        bool uiTurnL  = turnLeftHold  && turnLeftHold.isHeld;
        bool uiDefend = shieldHold && shieldHold.isHeld;

        // Joystick virtual (si está y hay entrada significativa)
        if (joystick)
        {
            float jh = joystick.Horizontal;
            float jv = joystick.Vertical;


            // Aplica zona muerta: ignora valores pequeños
            if (Mathf.Abs(jh) < deadZone) jh = 0f;
            if (Mathf.Abs(jv) < deadZone) jv = 0f;

            // Reduce la sensibilidad multiplicando por un factor (<1)
            jh *= sensitivity;
            jv *= sensitivity;

            // Solo sustituimos si hay entrada significativa
            if (Mathf.Abs(jh) > 0.001f || Mathf.Abs(jv) > 0.001f)
            {
                h = jh;
                v = jv;
            }
        }


        // Fusión teclado + UI: los holds se combinan por OR
        s.defend   = kbDefend || uiDefend;
        s.crouch   = kbCrouch || uiCrouch;
        s.turnRightAroundCenter = kbTurnRight || uiTurnR;
        s.turnLeftAroundCenter  = kbTurnLeft  || uiTurnL;

        s.axisH = h;
        s.axisV = v;

        return s;
    }

    private void ApplyTriggers(InputState s)
    {
        if (s.attack1) { TriggerBool("AttackBool"); anyButton = true; }
        if (s.attack2) { TriggerBool("Attack2Bool"); anyButton = true; }
        if (s.jump)    { TriggerBool("JumpBool");    anyButton = true; }
        if (s.dodge) { TriggerBool("dodgeBool"); anyButton = true; }
        if (s.dodge2) { TriggerBool("dodge2Bool"); anyButton = true; }
        if (s.kick)    { TriggerBool("kickBool");    anyButton = true; }
    }

    private void ApplyBools(InputState s)
    {
        // Escudo / crouch / laterales (A/D o botones) -> exacto a V1
        animator.SetBool("isDefending", s.defend);
        animator.SetBool("Crouch", s.crouch);
        animator.SetBool("MoveRight", s.turnRightAroundCenter);
        animator.SetBool("MoveLeft", s.turnLeftAroundCenter);

        // Si se mantienen A/D (o sus botones), se orienta al centro
        if (s.turnRightAroundCenter || s.turnLeftAroundCenter)
            OrientTowardsCenter();

        // ✅ Activar o desactivar el escudo lógicamente (teclado o canvas)
        if (s.defend)
        {
            if (!hasShield)
                shieldActivate(); // activa la lógica del escudo
        }
        else
        {
            hasShield = false; // desactiva la lógica del escudo
        }

        // AnyButton si cualquier cosa está activa (como V1)
        if (s.defend || s.crouch || s.turnRightAroundCenter || s.turnLeftAroundCenter ||
            Mathf.Abs(s.axisH) + Mathf.Abs(s.axisV) > 0.1f)
            anyButton = true;
    }


    private void HandleMovementLikeV1(InputState s)
    {
        // Flags de “joystick lateral” (para anim) con umbral
        isJoystickRight = s.axisH > 0.1f;
        isJoystickLeft  = s.axisH < -0.1f;

        animator.SetBool("isJoystickRight", isJoystickRight);
        animator.SetBool("isJoystickLeft",  isJoystickLeft);

        if (Mathf.Abs(s.axisH) > 0f || Mathf.Abs(s.axisV) > 0f)
        {
            // Dirección base en plano XZ como en V1
            Vector3 dir = new Vector3(s.axisH, 0f, s.axisV).normalized;

            // En V1 se transformaba por la cámara y se anulaba Y
            if (cameraTransform)
            {
                dir = cameraTransform.TransformDirection(dir);
                dir.y = 0f;
            }

            // Lógica V1: vertical > 0 -> correr; vertical < 0 -> retroceder
            if (s.axisV > 0.1f)
            {
                isRunning = true;
                isWalkingBackward = false;
            }
            else if (s.axisV < -0.1f)
            {
                isRunning = false;
                isWalkingBackward = true;

                // En V1, si no había joystick lateral (derecha/izquierda) se invertía la dirección
                if (!(isJoystickRight || isJoystickLeft))
                    dir = -dir;
            }
            else
            {
                isRunning = false;
                isWalkingBackward = false;
            }

            // Rotación yaw-only hacia la dirección objetivo
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            isRunning = false;
            isWalkingBackward = false;
        }

        animator.SetBool("isRunning",         isRunning);
        animator.SetBool("isWalkingBackward", isWalkingBackward);
    }

    private void DetectTurnBack(float h, float v)
    {
        bool turnedH = (prevH * h) < 0f;
        bool turnedV = (prevV * v) < 0f;

        turnBack = turnedH || turnedV;
        animator.SetBool("TurnBack", turnBack);

        if (Mathf.Abs(h) > 0.0001f) prevH = h;
        if (Mathf.Abs(v) > 0.0001f) prevV = v;

        if (turnBack) anyButton = true;
    }

    private void OrientTowardsCenter()
    {
        Vector3 toCenter = arenaCenter - transform.position;
        toCenter.y = 0f;
        if (toCenter.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(toCenter, Vector3.up);
        transform.rotation = target;
    }

    // --------- Utilidades de triggers (bool que se apaga) ----------
    private void TriggerBool(string boolName, float duration = 0.1f)
    {
        if (triggerRoutine != null && false)
            StopCoroutine(triggerRoutine);

        triggerRoutine = StartCoroutine(TriggerBoolCoroutine(boolName, duration));
    }

    private IEnumerator TriggerBoolCoroutine(string boolName, float duration)
    {
        animator.SetBool(boolName, true);
        yield return new WaitForSeconds(duration);
        animator.SetBool(boolName, false);
    }

    // (Se mantiene para compatibilidad si lo llamas en otro sitio)
    private void FireAllTriggersFromBools()
    {
        for (int i = 0; i < animator.parameterCount; i++)
        {
            var p = animator.GetParameter(i);
            if (p.type == AnimatorControllerParameterType.Bool && p.name.EndsWith("Bool"))
            {
                bool val = animator.GetBool(p.name);
                if (val)
                {
                    string trigger = p.name.Substring(0, p.name.Length - 4);
                    animator.SetTrigger(trigger);
                    Debug.Log("Triggering: " + p.name);
                }
            }
        }
    }

    // --------- API pública conservada ----------
    public void shieldActivate() => hasShield = true;

    public bool HasShield(bool orientation) => hasShield && orientation;

    // --------- Wiring de UI desde singleton (opcional) ----------
    private void TryWireUIFromSingleton()
    {
        if (ButtonBindings.Instance == null) return;

        attack1Button     = attack1Button     ? attack1Button     : ButtonBindings.Instance.attack1Button;
        attack2Button     = attack2Button     ? attack2Button     : ButtonBindings.Instance.attack2Button;
        jumpButton        = jumpButton        ? jumpButton        : ButtonBindings.Instance.jumpButton;
        dodgeButton       = dodgeButton       ? dodgeButton       : ButtonBindings.Instance.dodgeButton;
        dodgeButton2      = dodgeButton2      ? dodgeButton2      : ButtonBindings.Instance.dodgeButton2;
        kickButton        = kickButton        ? kickButton        : ButtonBindings.Instance.kickButton;
        crouchButton      = crouchButton      ? crouchButton      : ButtonBindings.Instance.crouchButton;
        turnRightButton   = turnRightButton   ? turnRightButton   : ButtonBindings.Instance.turnRightButton;
        turnLeftButton    = turnLeftButton    ? turnLeftButton    : ButtonBindings.Instance.turnLeftButton;
        shieldButton      = shieldButton      ? shieldButton      : ButtonBindings.Instance.shieldButton;
        lanchWarrokButton = lanchWarrokButton ? lanchWarrokButton : ButtonBindings.Instance.LanchWarrokButton;
        joystick          = joystick          ? joystick          : ButtonBindings.Instance.joystick;

        // Log de faltantes (opcionales)
        string miss = "";
        if (!attack1Button)     miss += "attack1Button ";
        if (!attack2Button)     miss += "attack2Button ";
        if (!jumpButton)        miss += "jumpButton ";
        if (!dodgeButton)       miss += "dodgeButton ";
        if (!dodgeButton2)      miss += "dodgeButton2 ";
        if (!kickButton)        miss += "kickButton ";
        if (!crouchButton)      miss += "crouchButton ";
        if (!turnRightButton)   miss += "turnRightButton ";
        if (!turnLeftButton)    miss += "turnLeftButton ";
        if (!shieldButton)      miss += "shieldButton ";
        if (!lanchWarrokButton) miss += "LanchWarrokButton ";
        if (!string.IsNullOrEmpty(miss))
            Debug.LogWarning("Botones/joystick no asignados (opcionales): " + miss);
    }
}
