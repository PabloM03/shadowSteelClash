using UnityEngine;

public class RestrictToCircleMovement : MonoBehaviour
{
    private Vector3 center = Vector3.zero; // Centro del círculo en (0,0)
    public float radius = 7.4f; // Radio del círculo


    // Update is called once per frame
    void Update()
    {
        RestrictToCircle();
    }

    private void RestrictToCircle()
    {
        Vector3 offset = transform.position - center;
        if (offset.magnitude > radius)
        {
            offset = offset.normalized * radius;
            transform.position = center + offset;
        }
    }
}