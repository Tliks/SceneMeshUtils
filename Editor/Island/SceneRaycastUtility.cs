using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{

    public static class SceneRaycastUtility
    {
        private static GameObject ColiderObject; 
        private static MeshCollider MeshCollider;
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
            if (HandleUtility.RaySnap(ray) is RaycastHit hit && hit.transform.gameObject == ColiderObject)
            {
                hitInfo = hit;
                return true;
            }

            return false;
        }
        
        public static MeshCollider AddCollider(Transform transform)
        {
            if (ColiderObject == null)
            {
                ColiderObject = new GameObject();
                ColiderObject.name = "AO preview";
                ColiderObject.transform.position = transform.position;
                ColiderObject.transform.rotation = transform.rotation;
                ColiderObject.transform.localScale = transform.lossyScale;
            }
            
            MeshCollider = ColiderObject.GetComponent<MeshCollider>();
            if (MeshCollider == null)
            {
                MeshCollider = ColiderObject.AddComponent<MeshCollider>();
                MeshCollider.convex = false;  
            }
            return MeshCollider;
        }

        public static void DeleteCollider()
        {
            Object.DestroyImmediate(ColiderObject);
        }

        public static void UpdateColider(Mesh mesh)
        {
            MeshCollider.sharedMesh = mesh;
        }

    }
}