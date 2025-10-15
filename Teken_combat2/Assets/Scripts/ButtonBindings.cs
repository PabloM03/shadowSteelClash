using UnityEngine;
using UnityEngine.UI;

// Asigna los botones desde el Inspector en una única GameObject de la escena.
// KnightController buscará esta instancia y tomará los botones si le faltan.
public class ButtonBindings : MonoBehaviour
{
    public static ButtonBindings Instance { get; private set; }

    public Button attack1Button;
    public Button attack2Button;
    public Button jumpButton;
    public Button dodgeButton;
    public Button dodgeButton2;
    public Button kickButton;
    public Button crouchButton;
    public Button turnRightButton;
    public Button turnLeftButton;
    public Button shieldButton;
    public Button LanchWarrokButton;
    public VirtualJoystick joystick;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ButtonBindings in scene. Using first one: " + Instance.gameObject.name);
            return;
        }
        Instance = this;

        // If joystick not assigned in inspector, try to find one in the scene
        if (joystick == null)
        {
            joystick = GameObject.FindObjectOfType<VirtualJoystick>(true);
            if (joystick != null)
            {
                Debug.Log("ButtonBindings: auto-assigned VirtualJoystick from scene: " + joystick.gameObject.name);
            }
            else
            {
                Debug.Log("ButtonBindings: no VirtualJoystick found in scene. Assign in Inspector if needed.");
            }
        }
    }
}