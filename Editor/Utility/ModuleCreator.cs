using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.aoyon.scenemeshutils
{
    public class ModuleCreatorSettings
    {
        public bool IncludePhysBone = true;
        public bool IncludePhysBoneColider = true;

        public bool RenameRootTransform = true;
        public bool RemainAllPBTransforms = false;
        public bool IncludeIgnoreTransforms = false;
        public GameObject RootObject = null;
        public Mesh newmesh=null;
    }

    public class ModuleCreatorProcessor
    {
        public static GameObject CheckAndCopyBones(GameObject sourceObject, ModuleCreatorSettings settings)
        {   
            GameObject instance = null;
            try
            {
                (GameObject root, int skin_index) = CheckObjects(sourceObject);

                (GameObject new_root, string variantPath) = SaveRootObject(root, sourceObject.name);

                CheckUtility.CleanUpHierarchy(new_root, skin_index, settings);

                PrefabUtility.SavePrefabAsset(new_root);

                //float BaseX = root.transform.position.x;
                //float randomX = UnityEngine.Random.Range(BaseX + 1, BaseX + 2);
                instance = PrefabUtility.InstantiatePrefab(new_root) as GameObject;
                SceneManager.MoveGameObjectToScene(instance, sourceObject.scene);
                //Vector3 newPosition = instance.transform.position;
                //newPosition.x = randomX;
                //instance.transform.position = newPosition;

                //Selection.objects.Append(instance);

                EditorGUIUtility.PingObject(instance);
                Selection.activeGameObject = instance;
                //Selection.objects = Selection.gameObjects.Append(instance).ToArray();

                Selection.activeGameObject = new_root;
                EditorUtility.FocusProjectWindow();
                UnityEngine.Debug.Log("Saved prefab to " + variantPath);

            }

            catch (InvalidOperationException ex)
            {
                UnityEngine.Debug.LogError("[Module Creator] " + ex.Message);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.StackTrace);
                UnityEngine.Debug.LogError(ex);
            }

            return instance;
        }

        public static (GameObject, SkinnedMeshRenderer) PreviewMesh(SkinnedMeshRenderer sourceRenderer)
        {
            (GameObject root, int skin_index) = CheckObjects(sourceRenderer.gameObject);
            GameObject new_root = CopyObjects(root, "AAU Preview");
            SkinnedMeshRenderer skinnedMeshRenderer = CheckUtility.CleanUpHierarchy(new_root, skin_index);
            return (new_root, skinnedMeshRenderer);
        }


        private static (GameObject, int) CheckObjects(GameObject targetObject)
        {
            CheckUtility.Checktarget(targetObject);
            CheckUtility.CheckPrefabAsset(targetObject);
            GameObject root = CheckUtility.CheckRoot(targetObject);
            CheckUtility.CheckSkin(targetObject);
            CheckUtility.CheckHips(root);

            //skin_index: 複製先でSkinnedMeshRendererがついたオブジェクトを追跡するためのインデックス
            Transform[] AllChildren = CheckUtility.GetAllChildren(root);
            int skin_index = Array.IndexOf(AllChildren, targetObject.transform);

            return (root, skin_index);
        }
        

        private static (GameObject, string) SaveRootObject(GameObject root_object, string source_name)
        {
            string variantPath = AssetPathUtility.GeneratePrefabPath(root_object.name, source_name);

            GameObject new_root = PrefabUtility.SaveAsPrefabAsset(root_object, variantPath);        
            if (new_root == null)
            {
                throw new InvalidOperationException("Prefab creation failed.");
            }
            return (new_root, variantPath);

        }

        private static GameObject CopyObjects(GameObject root_object, string source_name)
        {
            GameObject duplicatedParent = UnityEngine.Object.Instantiate(root_object);
            duplicatedParent.name = source_name;
            return duplicatedParent;
        }


    }
}