using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool isHeld = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
        //Debug.Log($"UIButtonHold.OnPointerDown on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
        //Debug.Log($"UIButtonHold.OnPointerUp on {gameObject.name}");
    }
}
