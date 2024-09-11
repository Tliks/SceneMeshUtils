using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Collections.Immutable;

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
        
        public static GameObject CheckAndCopyBones(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, ModuleCreatorSettings settings)
        {   
            GameObject instance = null;
            try
            {

                IEnumerable<GameObject> objs = skinnedMeshRenderers.Select(r => r.gameObject);
                GameObject root = CheckUtility.CheckObjects(objs);

                string mesh_name = objs.Count() == 1 ? objs.First().name : $"{root.name} Parts";
                (GameObject new_root, string variantPath) = SaveRootObject(root, mesh_name);
                new_root.transform.position = Vector3.zero;

                IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = GetRenderers(root, new_root, objs);

                CreateModule(new_root, newskinnedMeshRenderers, settings, root.scene);
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

        public static void CreateModule(GameObject new_root, IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers, ModuleCreatorSettings settings, Scene scene)
        {
            GameObject instance = null;
            try
            {
                CheckUtility.CleanUpHierarchy(new_root, newskinnedMeshRenderers, settings);

                PrefabUtility.SavePrefabAsset(new_root);

                instance = PrefabUtility.InstantiatePrefab(new_root) as GameObject;
                SceneManager.MoveGameObjectToScene(instance, scene);

                EditorGUIUtility.PingObject(instance);
                Selection.activeGameObject = instance;

                Selection.activeGameObject = new_root;
                EditorUtility.FocusProjectWindow();

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

        }

        public static (GameObject, string) SaveRootObject(GameObject root_object, string source_name)
        {
            string variantPath = AssetPathUtility.GeneratePrefabPath(root_object.name, source_name);

            GameObject new_root = PrefabUtility.SaveAsPrefabAsset(root_object, variantPath);        
            if (new_root == null)
            {
                throw new InvalidOperationException("Prefab creation failed.");
            }
            return (new_root, variantPath);

        }

        public static (GameObject, IEnumerable<SkinnedMeshRenderer>, string) SaveRenderes(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, string source_name)
        {
            IEnumerable<GameObject> objs = skinnedMeshRenderers.Select(r => r.gameObject);
            GameObject root = CheckUtility.CheckObjects(objs);

            (GameObject new_root, string variantPath) = SaveRootObject(root, source_name);

            IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = GetRenderers(root, new_root, objs);

            return (new_root, newskinnedMeshRenderers, variantPath);
        }

        public static (GameObject, IEnumerable<SkinnedMeshRenderer>) CopyRenderers(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers)
        {
            IEnumerable<GameObject> objs = skinnedMeshRenderers.Select(r => r.gameObject);
            GameObject root = CheckUtility.CheckObjects(objs);

            GameObject new_root = UnityEngine.Object.Instantiate(root);

            IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = GetRenderers(root, new_root, objs);

            return (new_root, newskinnedMeshRenderers);

        }

        public static (GameObject, SkinnedMeshRenderer) PreviewMesh(SkinnedMeshRenderer sourceRenderer)
        {
            List<GameObject> objs = new List<GameObject> {sourceRenderer.gameObject};
            GameObject root = CheckUtility.CheckObjects(objs);

            GameObject new_root = UnityEngine.Object.Instantiate(root);
            new_root.name = "SMU Preview";

            IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = GetRenderers(root, new_root, objs);

            CheckUtility.CleanUpHierarchy(new_root, newskinnedMeshRenderers);
            return (new_root, newskinnedMeshRenderers.First());
        }

        public static IEnumerable<SkinnedMeshRenderer> GetRenderers(GameObject root, GameObject new_root, IEnumerable<GameObject> objs)
        {
            IEnumerable<int> skinIndices = CheckUtility.GetIndices(root, objs);
            IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = CheckUtility.GetSkin(new_root, skinIndices);
            return newskinnedMeshRenderers;
        }

    }
}