using UnityEngine;
using UnityEngine.EventSystems;

// Attach this temporarily to a Button GameObject and/or its children to see which object receives pointer events.
public class UIPointerLogger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"UIPointerLogger.OnPointerDown on {gameObject.name} (raycast target: {GetRaycastTargetName(eventData)})");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"UIPointerLogger.OnPointerUp on {gameObject.name} (raycast target: {GetRaycastTargetName(eventData)})");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"UIPointerLogger.OnPointerClick on {gameObject.name} (raycast target: {GetRaycastTargetName(eventData)})");
    }

    private string GetRaycastTargetName(PointerEventData eventData)
    {
        try
        {
            if (eventData == null || eventData.pointerCurrentRaycast.gameObject == null) return "<none>";
            return eventData.pointerCurrentRaycast.gameObject.name;
        }
        catch { return "<error>"; }
    }
}
