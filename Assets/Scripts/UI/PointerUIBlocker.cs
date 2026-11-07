using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class PointerUIBlocker
{
    private static readonly List<RaycastResult> Hits = new List<RaycastResult>();
    private static EventSystem cachedEventSystem;
    private static PointerEventData pointer;

    public static bool IsPointerOverUI()
    {
        return IsPointerOverUI(Input.mousePosition);
    }

    public static bool IsPointerOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        // Raycast now so world input does not depend on EventSystem.Update running first.
        if (pointer == null || cachedEventSystem != eventSystem)
        {
            cachedEventSystem = eventSystem;
            pointer = new PointerEventData(eventSystem);
        }
        pointer.Reset();
        pointer.position = screenPosition;
        Hits.Clear();
        eventSystem.RaycastAll(pointer, Hits);
        bool blocked = false;
        foreach (RaycastResult hit in Hits)
        {
            if (hit.module is UnityEngine.UI.GraphicRaycaster)
            {
                blocked = true;
                break;
            }
        }
        Hits.Clear();
        return blocked;
    }
}
