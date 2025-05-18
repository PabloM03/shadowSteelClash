using UnityEngine;

public class light : MonoBehaviour
{
    // Velocidad de rotación en grados por segundo
    public float rotationSpeed = 50f;

    void Update()
    {
        // Rotar el objeto sobre el eje x
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }
}
