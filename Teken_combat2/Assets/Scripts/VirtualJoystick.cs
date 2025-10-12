using UnityEngine;
using UnityEngine.EventSystems;

// Simple Virtual Joystick for UI. Attach to a GameObject with a background RectTransform
// and set the "handle" child RectTransform. Exposes Horizontal and Vertical (-1..1)
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    public RectTransform background; // full joystick area
    public RectTransform handle;     // the movable handle

    [Header("Settings")]
    public float handleRange = 50f; // pixels from center
    public float deadZone = 0.1f;   // normalized deadzone

    private Vector2 input = Vector2.zero;

    public float Horizontal => input.x;
    public float Vertical => input.y;

    void Reset()
    {
        // try to auto-find handle as first child
        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0) as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;

        Vector2 pos;
        // convert screen point to local point in background rect
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos);

        // clamp to handleRange
        Vector2 clamped = Vector2.ClampMagnitude(pos, handleRange);

        // set handle anchored position
        if (handle != null)
            handle.anchoredPosition = clamped;

        // normalized input
        input = clamped / handleRange;

        if (input.magnitude < deadZone) input = Vector2.zero;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    // Optional editor helper
    void OnDisable()
    {
        input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
