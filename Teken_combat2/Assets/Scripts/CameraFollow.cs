using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Objeto al que la cámara seguirá
    public Transform target;

    // Offset relativo al objetivo (ajustado para la vista inicial deseada)
    public Vector3 offset = new Vector3(0.23f, 2f, -2.82f);

    // Velocidad de rotación de la cámara
    public float rotationSpeed = 2f;
    private float returnSpeed = 2f; // Velocidad de retorno a la posición original

    // Umbral para detectar si el joystick está casi en el centro
    private float joystickThreshold = 0.1f;

    // Ángulos acumulados para la rotación horizontal y vertical
    private float currentRotationX = 8.201f;  // Ángulo inicial en el eje X
    private float currentRotationY;
    public float height = 1;

    // Estado de bloqueo de la orientación
    private bool isOrientationLocked = false;

    // Límite inferior para la rotación vertical (en este caso, 0 grados)
    public float minRotationX = -22;

    private float radius = 7.4f;
    private float nearInsideCircle = 0.3f;
    private float nearOutsideCircle = 1.5f;

    void Start()
    {
        if (target != null)
        {
            // Configura la rotación y posición inicial de la cámara
            currentRotationY = target.eulerAngles.y;
            Vector3 targetPosition = target.position + target.rotation * offset;
            transform.position = targetPosition;
            transform.rotation = Quaternion.Euler(8.201f, target.eulerAngles.y, 0f);
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Entrada del joystick derecho
            float horizontalInput = Input.GetAxis("RightStickHorizontal");
            float verticalInput = Input.GetAxis("RightStickVertical");

            // Calcular la magnitud del movimiento del joystick
            float joystickMagnitude = new Vector2(horizontalInput, verticalInput).magnitude;

            // Verificar si el botón 11 está siendo presionado
            bool isButton11Pressed = Input.GetKey(KeyCode.JoystickButton11);

            // Verificar si el botón 8 ha sido presionado para alternar el estado de bloqueo
            if (Input.GetKeyDown(KeyCode.JoystickButton8))
            {
                isOrientationLocked = !isOrientationLocked;
            }

            if (!isOrientationLocked)
            {
                currentRotationX -= verticalInput * rotationSpeed;

                // Si el botón 11 no está presionado y la magnitud del joystick es cercana a cero,
                // la cámara vuelve suavemente a la posición original
                if (!isButton11Pressed && joystickMagnitude < joystickThreshold)
                {
                    // Suavemente ajusta la rotación para alinearse con el personaje
                    currentRotationY = Mathf.LerpAngle(currentRotationY, target.eulerAngles.y, Time.deltaTime * returnSpeed);
                    currentRotationX = Mathf.Lerp(currentRotationX, 8.201f, Time.deltaTime * returnSpeed);
                }
                else if (!isButton11Pressed)
                {
                    // Ajustar la rotación cuando el joystick está en uso
                    currentRotationY -= horizontalInput * rotationSpeed;
                }
            }
	
	    currentRotationX = Mathf.Max(currentRotationX, minRotationX);

            // Aplicar la rotación acumulada al offset inicial
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
            Vector3 targetPosition = target.position + rotation * offset;
            transform.position = targetPosition;

            // Rotar la cámara para que mire al objetivo con una altura ajustada
            Vector3 lookAtPosition = target.position;
            lookAtPosition.y += height; // Ajusta la altura del punto de mira
            transform.LookAt(lookAtPosition);
        }
	
	

	// Calcula la distancia de la cámara al origen en el plano XZ
	float distanceToOriginXZ = new Vector2(transform.position.x,transform.position.z).magnitude;

	// Asigna el nearClipPlane en función de si está dentro o fuera del círculo
	GetComponent<Camera>().nearClipPlane = (distanceToOriginXZ > radius) ? 	nearOutsideCircle : nearInsideCircle;


    }
}
