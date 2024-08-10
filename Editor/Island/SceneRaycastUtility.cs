using System;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{

    public static class SceneRaycastUtility
    {
        public static GameObject selectedColiderObject; 
        public static GameObject unselectedColiderObject; 
        private static MeshCollider selectedMeshCollider;
        private static MeshCollider ubselectedMeshCollider;

        private static RaycastHit[] hits = new RaycastHit[20];

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
            
            int hitCount = Physics.RaycastNonAlloc(ray, hits, Mathf.Infinity);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.gameObject == selectedColiderObject || hit.collider.gameObject == unselectedColiderObject)
                {
                    hitInfo = hit;
                    return true;
                }
            }

            return false;
        }

        public static bool IsSelected(RaycastHit hitInfo)
        {
            GameObject hitGameobject = hitInfo.transform.gameObject;
            if (hitGameobject == selectedColiderObject)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public static void AddCollider(Transform selectedtransform, Transform unselectedtransform)
        {   
            AddcoliderObject(ref selectedColiderObject, selectedtransform);
            AddcoliderObject(ref unselectedColiderObject, unselectedtransform);

            selectedMeshCollider = AddMeshCollider(selectedColiderObject);
            ubselectedMeshCollider = AddMeshCollider(unselectedColiderObject);

            return;

            void AddcoliderObject(ref GameObject obj, Transform transform)
            {
                if (obj == null)
                {
                    obj = new GameObject();
                    obj.name = "AAU preview";
                    obj.transform.position = transform.position;
                    obj.transform.rotation = transform.rotation;
                }
            }
            
            MeshCollider AddMeshCollider(GameObject obj)
            {
                MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = obj.AddComponent<MeshCollider>();
                    meshCollider.convex = false;
                }
                return meshCollider;
            }
            
        }

        public static void DeleteCollider()
        {
            UnityEngine.Object.DestroyImmediate(selectedColiderObject);
            UnityEngine.Object.DestroyImmediate(unselectedColiderObject);
        }

        public static void UpdateColider(Mesh mesh, bool IsSelected)
        {
            if (IsSelected)
            {
                selectedMeshCollider.sharedMesh = mesh;
            }
            else
            {
                ubselectedMeshCollider.sharedMesh = mesh;
            }
        }

    }
}