using UnityEditor;
using UnityEngine;

public struct SceneRaycastHitInfo
{
    public RaycastHit hit;
    public Transform hitTransform;
    public int hitTriangleIndex;

    public SceneRaycastHitInfo(RaycastHit hit, Transform hitTransform, int hitTriangleIndex)
    {
        this.hit = hit;
        this.hitTransform = hitTransform;
        this.hitTriangleIndex = hitTriangleIndex;
    }
}

public static class SceneRaycastUtility
{
    public static bool TryRaycast(out SceneRaycastHitInfo hitInfo)
    {
        hitInfo = new SceneRaycastHitInfo();
        
        if (Event.current == null)
        {
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
        if (HandleUtility.RaySnap(ray) is RaycastHit hit)
        {
            hitInfo = new SceneRaycastHitInfo(hit, hit.transform, hit.triangleIndex);
            return true;
        }

        return false;
    }

    public static bool IsHitObject(GameObject targetObject, SceneRaycastHitInfo hitInfo)
    {
        return hitInfo.hitTransform != null && hitInfo.hitTransform.gameObject == targetObject;
    }
}