using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

// Runtime helper to auto-fix common UI configuration problems that block pointer events.
// Attach to any GameObject in the scene (or leave it as a prefab) - it runs once in Start().
public class UIRuntimeFixer : MonoBehaviour
{
    [Tooltip("If true, the script will add missing UIButtonHold components to Buttons automatically.")]
    public bool addUIButtonHoldIfMissing = true;

    void Start()
    {
        EnsureEventSystem();
        EnsureCanvasesHaveGraphicRaycaster();
        FixButtons();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("UIRuntimeFixer: EventSystem created at runtime.");
        }
    }

    void EnsureCanvasesHaveGraphicRaycaster()
    {
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.GetComponent<GraphicRaycaster>() == null)
            {
                c.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("UIRuntimeFixer: Added GraphicRaycaster to Canvas: " + c.gameObject.name);
            }
        }
    }

    void FixButtons()
    {
        var buttons = FindObjectsOfType<Button>(includeInactive: true);
        Debug.Log($"UIRuntimeFixer: Found {buttons.Length} Buttons (including inactive). Listing details...");
        foreach (var b in buttons)
        {
            bool active = b.gameObject.activeInHierarchy;
            var img = b.GetComponent<Image>();
            bool hasImg = img != null;
            bool raycast = hasImg ? img.raycastTarget : false;
            bool hasHold = b.GetComponent<UIButtonHold>() != null;

            Debug.Log($"UIRuntimeFixer: Button '{b.gameObject.name}' | Active: {active} | Interactable: {b.interactable} | HasImage: {hasImg} | Image.raycastTarget: {raycast} | HasUIButtonHold: {hasHold}");

            // Ensure the Button is interactable (even if inactive, we can set the field)
            if (!b.interactable)
            {
                b.interactable = true;
                Debug.Log("UIRuntimeFixer: Set Button.interactable = true on " + b.gameObject.name);
            }

            // Ensure the graphic (Image) is raycast target
            if (hasImg && !img.raycastTarget)
            {
                img.raycastTarget = true;
                Debug.Log("UIRuntimeFixer: Enabled Image.raycastTarget on " + b.gameObject.name);
            }

            // Optionally add UIButtonHold if missing (even on inactive buttons)
            if (addUIButtonHoldIfMissing && !hasHold)
            {
                b.gameObject.AddComponent<UIButtonHold>();
                Debug.Log("UIRuntimeFixer: Added UIButtonHold to " + b.gameObject.name);
            }
        }

        // Extra: quick check specifically for common control names
        string[] expected = new[] { "Crouch", "Turn left", "Turn right", "Shield" };
        foreach (var name in expected)
        {
            var found = buttons.FirstOrDefault(x => x.gameObject.name == name);
            if (found == null) Debug.LogWarning($"UIRuntimeFixer: No Button found with name '{name}' (check hierarchy and naming).\n");
            else Debug.Log($"UIRuntimeFixer: Confirmed presence of expected button '{name}' (active: {found.gameObject.activeInHierarchy}).");
        }
    }
}
