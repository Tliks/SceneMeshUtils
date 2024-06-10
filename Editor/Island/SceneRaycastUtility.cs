using UnityEditor;
using UnityEngine;

public static class SceneRaycastUtility
{
    public static bool TryRaycast(out RaycastHit hitInfo)
    {
        hitInfo = new RaycastHit();

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
            hitInfo = hit;
            return true;
        }

        return false;
    }
    
    public static bool IsHitObject(GameObject targetObject, RaycastHit hitInfo)
    {
        return hitInfo.transform != null && hitInfo.transform.gameObject == targetObject;
    }

    public static MeshCollider AddCollider(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        MeshCollider meshCollider;
        meshCollider = skinnedMeshRenderer.gameObject.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = skinnedMeshRenderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;  
        }
        return meshCollider;
    }

    public static void DeleteCollider(MeshCollider meshCollider)
    {
        Object.DestroyImmediate(meshCollider);
    }

}