using System;
using System.Reflection;
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
    private static readonly MethodInfo intersectRayMeshMethod = typeof(HandleUtility).GetMethod("IntersectRayMesh",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    private static GameObject lastGameObjectUnderCursor;

    public static bool RaycastAgainstScene(out ExtendedRaycastHit extendedHit)
    {
        if (Event.current == null)
        {
            extendedHit = new ExtendedRaycastHit();
            return false;
        }
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        GameObject gameObjectUnderCursor;
        switch (Event.current.type)
        {
            case EventType.Layout:
            case EventType.Repaint:
            case EventType.ExecuteCommand:
                gameObjectUnderCursor = lastGameObjectUnderCursor;
                break;
            default:
                gameObjectUnderCursor = HandleUtility.PickGameObject(Event.current.mousePosition, false);
                break;
        }

        if (gameObjectUnderCursor)
        {
            Mesh meshUnderCursor = null;
            if (gameObjectUnderCursor.TryGetComponent(out MeshFilter meshFilter))
                meshUnderCursor = meshFilter.sharedMesh;
            if (!meshUnderCursor && 
                gameObjectUnderCursor.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                Mesh bakedMesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(bakedMesh);
                meshUnderCursor = bakedMesh;
            }
            
            /*
            if (meshUnderCursor)
            {
                lastGameObjectUnderCursor = gameObjectUnderCursor;

                object[] rayMeshParameters = new object[]
                    { ray, meshUnderCursor, gameObjectUnderCursor.transform.localToWorldMatrix, null };
                if ((bool) intersectRayMeshMethod.Invoke(null, rayMeshParameters))
                {
                    RaycastHit tempHit = (RaycastHit) rayMeshParameters[3];
                    extendedHit = new ExtendedRaycastHit(tempHit, gameObjectUnderCursor.transform, tempHit.triangleIndex);
                    Debug.Log("bb");
                    return true;
                }
            }
            else
                lastGameObjectUnderCursor = null;
            */
        }

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