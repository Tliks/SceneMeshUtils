using UnityEditor;
using UnityEngine;

public struct ExtendedRaycastHit
{
    public RaycastHit raycastHit;
    public Transform transform;
    public int triangleIndex;

    public ExtendedRaycastHit(RaycastHit raycastHit, Transform transform, int triangleIndex)
    {
        this.raycastHit = raycastHit;
        this.transform = transform;
        this.triangleIndex = triangleIndex;
    }
}

public static class EditorRaycastHelper
{
    public static bool RaycastAgainstScene(out ExtendedRaycastHit extendedHit)
    {
        if (Event.current == null)
        {
            extendedHit = new ExtendedRaycastHit();
            return false;
        }

        switch (Event.current.type)
        {
            case EventType.Layout:
            case EventType.Repaint:
            case EventType.ExecuteCommand:
                break;
            default:
                HandleUtility.PickGameObject(Event.current.mousePosition, false);
                break;
        }
        
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        object raycastResult = HandleUtility.RaySnap(ray);
        if (raycastResult != null && raycastResult is RaycastHit tempHit2)
        {
            extendedHit = new ExtendedRaycastHit(tempHit2, tempHit2.transform, tempHit2.triangleIndex);
            return true;
        }

        extendedHit = new ExtendedRaycastHit();
        return false;
    }
    public static bool IsHitObjectSpecified(ExtendedRaycastHit extendedHit, GameObject specifiedObject)
    {
        return extendedHit.transform != null && extendedHit.transform.gameObject == specifiedObject;
    }
}