using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Diagnostics;

public class ModuleCreatorSettings
{
    public bool IncludePhysBone = true;
    public bool IncludePhysBoneColider = true;

    public bool RenameRootTransform = false;
    public bool RemainAllPBTransforms = false;
    public bool IncludeIgnoreTransforms = false;
    public GameObject RootObject = null;
    public Mesh newmesh=null;
}

public class ModuleCreator
{
    private readonly ModuleCreatorSettings _Settings;

    public ModuleCreator(ModuleCreatorSettings settings)
    {
        _Settings = settings;
    }
    
    public void CheckAndCopyBones(GameObject sourceObject)
    {   
        try
        {
            Stopwatch stopwatch = new Stopwatch();
            
            //stopwatch.Start();
            (GameObject root, int skin_index) = CheckObjects(sourceObject);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("CheckObjects: " + stopwatch.ElapsedMilliseconds + " ms");

            stopwatch.Start();
            (GameObject new_root, string variantPath) = SaveRootObject(root, sourceObject.name);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("SaveAsPrefabAsset: " + stopwatch.ElapsedMilliseconds + " ms");

            stopwatch.Start();
            CleanUpHierarchy(new_root, skin_index);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("CleanUpHierarchy: " + stopwatch.ElapsedMilliseconds + " ms");

            stopwatch.Start();
            PrefabUtility.SavePrefabAsset(new_root);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("SavePrefabAsset: " + stopwatch.ElapsedMilliseconds + " ms");

            float BaseX = root.transform.position.x;
            float randomX = UnityEngine.Random.Range(BaseX + 1, BaseX + 2);
            GameObject instance = PrefabUtility.InstantiatePrefab(new_root) as GameObject;
            SceneManager.MoveGameObjectToScene(instance, sourceObject.scene);
            Vector3 newPosition = instance.transform.position;
            newPosition.x = randomX;
            //instance.transform.position = newPosition;

            //Selection.objects.Append(instance);

            EditorGUIUtility.PingObject(instance);
            //Selection.activeGameObject = instance;
            Selection.objects = Selection.gameObjects.Append(instance).ToArray();

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
    }

    public (GameObject, SkinnedMeshRenderer) PreviewMesh(GameObject sourceObject)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = null;
        GameObject new_root = null;
        try
        {
            Stopwatch stopwatch = new Stopwatch();
            
            //stopwatch.Start();
            (GameObject root, int skin_index) = CheckObjects(sourceObject);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("CheckObjects: " + stopwatch.ElapsedMilliseconds + " ms");

            stopwatch.Start();
            new_root = CopyObjects(root, sourceObject.name);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("CopyObjects: " + stopwatch.ElapsedMilliseconds + " ms");

            stopwatch.Start();
            skinnedMeshRenderer = CleanUpHierarchy(new_root, skin_index);
            stopwatch.Stop();
            //UnityEngine.Debug.Log("CleanUpHierarchy: " + stopwatch.ElapsedMilliseconds + " ms");

            EditorGUIUtility.PingObject(new_root);
            Selection.objects = Selection.gameObjects.Append(new_root).ToArray();

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

        return (new_root, skinnedMeshRenderer);

    }

    private (GameObject, int) CheckObjects(GameObject targetObject)
    {
        Checktarget(targetObject);
        CheckPrefabAsset(targetObject);
        GameObject root = CheckRoot(targetObject);
        CheckSkin(targetObject);
        CheckHips(root);

        //skin_index: 複製先でSkinnedMeshRendererがついたオブジェクトを追跡するためのインデックス
        Transform[] AllChildren = GetAllChildren(root);
        int skin_index = Array.IndexOf(AllChildren, targetObject.transform);

        return (root, skin_index);


        void Checktarget(GameObject targetObject)
        {
            if (targetObject == null)
            {
                throw new InvalidOperationException("Target object is not set.");
            }
        }

        void CheckPrefabAsset(GameObject targetObject)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(targetObject))
            {
                throw new InvalidOperationException("Please run it on the prefab instance in the hierarchy, not on the prefabasset.");
            }
        }

        GameObject CheckRoot(GameObject targetObject)
        {
            if (_Settings.RootObject) return _Settings.RootObject;
            //親オブジェクトが存在するか確認
            Transform parent = targetObject.transform.parent;
            if (parent == null)
            {
                throw new InvalidOperationException("Please select the object with SkinnedMeshRenderer directly under the avatar/costume");
            }

            GameObject root;
            if (PrefabUtility.IsPartOfPrefabInstance(targetObject))
            {
                root = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject);
            }
            else
            {
                root = parent.gameObject;
            }
            return root;
        }

        void CheckSkin(GameObject targetObject)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                
                throw new InvalidOperationException($"'{targetObject.name}' does not have a SkinnedMeshRenderer.");
            }
        }

        void CheckHips(GameObject root)
        {
            GameObject hips = null;
            foreach (Transform child in root.transform)
            {
                foreach (Transform grandChild in child.transform)
                {
                    if (grandChild.name.ToLower().StartsWith("hip"))
                    {
                        hips = grandChild.gameObject;
                        break;
                    }
                }
            }
            if (hips == null)
            {
                //throw new InvalidOperationException("Hips not found under the root object.");
                UnityEngine.Debug.LogWarning("Hips could not be found. Merge Armature/Setup Outfit may not work properly.");
            }
        }

    }

    private (GameObject, string) SaveRootObject(GameObject root_object, string source_name)
    {
        string variantPath = AssetPathUtility.GeneratePrefabPath(root_object.name, source_name);

        GameObject new_root = PrefabUtility.SaveAsPrefabAsset(root_object, variantPath);        
        if (new_root == null)
        {
            throw new InvalidOperationException("Prefab creation failed.");
        }
        return (new_root, variantPath);

    }

    private GameObject CopyObjects(GameObject root_object, string source_name)
    {
        GameObject duplicatedParent = UnityEngine.Object.Instantiate(root_object);
        return duplicatedParent;
    }

    private SkinnedMeshRenderer CleanUpHierarchy(GameObject new_root, int skin_index)
    {   
        HashSet<GameObject> objectsToSave = new HashSet<GameObject>();

        // 複製先のSkinnedMeshRendererがついたオブジェクトを追加
        Transform[] AllChildren = GetAllChildren(new_root);
        GameObject skin = AllChildren[skin_index].gameObject;
        objectsToSave.Add(skin);

        SkinnedMeshRenderer skinnedMeshRenderer = skin.GetComponent<SkinnedMeshRenderer>();
        if (_Settings.newmesh) 
        {
            skinnedMeshRenderer.sharedMesh = _Settings.newmesh;
            MeshDeletionUtility.RemoveUnusedMaterials(skinnedMeshRenderer);
        }

        // SkinnedMeshRendererのrootBoneとanchor overrideに設定されているオブジェクトを追加
        Transform rootBone = skinnedMeshRenderer.rootBone;
        Transform anchor = skinnedMeshRenderer.probeAnchor;
        if (rootBone) objectsToSave.Add(rootBone.gameObject);
        if (anchor) objectsToSave.Add(anchor.gameObject);

        // ウェイトをつけているオブジェクトを追加
        HashSet<GameObject> weightedBones = GetWeightedBones(skinnedMeshRenderer);
        UnityEngine.Debug.Log($"Bones weighting {skin.name}: {weightedBones.Count}/{skinnedMeshRenderer.bones.Length}");
        objectsToSave.UnionWith(weightedBones);

        // PhysBoneに関連するオブジェクトを追加
        if (_Settings.IncludePhysBone == true) 
        {
            HashSet<GameObject> PhysBoneObjects = FindPhysBoneObjects(new_root, weightedBones);
            objectsToSave.UnionWith(PhysBoneObjects);
        }

        CheckAndDeleteRecursive(new_root, objectsToSave);

        return skinnedMeshRenderer;
    }

    private HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;
        Transform[] bones = skinnedMeshRenderer.bones;
        HashSet<GameObject> weightedBones = new HashSet<GameObject>();
        HashSet<int> missingBoneIndices = new HashSet<int>();

        foreach (BoneWeight boneWeight in boneWeights)
        {
            if (boneWeight.weight0 > 0)
            {
                Transform boneTransform = bones[boneWeight.boneIndex0];
                if (boneTransform == null) 
                    missingBoneIndices.Add(boneWeight.boneIndex0);
                else
                    weightedBones.Add(boneTransform.gameObject);
            }

            if (boneWeight.weight1 > 0)
            {
                Transform boneTransform = bones[boneWeight.boneIndex1];
                if (boneTransform == null) 
                    missingBoneIndices.Add(boneWeight.boneIndex1);
                else
                    weightedBones.Add(boneTransform.gameObject);
            }

            if (boneWeight.weight2 > 0)
            {
                Transform boneTransform = bones[boneWeight.boneIndex2];
                if (boneTransform == null) 
                    missingBoneIndices.Add(boneWeight.boneIndex2);
                else
                    weightedBones.Add(boneTransform.gameObject);
            }

            if (boneWeight.weight3 > 0)
            {
                Transform boneTransform = bones[boneWeight.boneIndex3];
                if (boneTransform == null) 
                    missingBoneIndices.Add(boneWeight.boneIndex3);
                else
                    weightedBones.Add(boneTransform.gameObject);
            }
        }

        if (missingBoneIndices.Count > 0)
        {
            throw new InvalidOperationException($"Some bones weighting mesh could not be found. Total missing bones: {missingBoneIndices.Count}");
        }

        return weightedBones;
    }

    private void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> objectsToSave)
    {   
        List<GameObject> children = GetChildren(obj);

        // 子オブジェクトに対して再帰的に処理を適用
        foreach (GameObject child in children)
        {   
            CheckAndDeleteRecursive(child, objectsToSave);
        }

        // 削除しない条件
        if (objectsToSave.Contains(obj) || obj.transform.childCount != 0)
        {
            ActivateObject(obj);
            RemoveComponents(obj);
            return;
        }
        
        UnityEngine.Object.DestroyImmediate(obj, true);
    }

    private List<GameObject> GetChildren(GameObject parent)
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in parent.transform)
        {
            children.Add(child.gameObject);
        }
        return children;
    }

    private static Transform[] GetAllChildren(GameObject parent)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        return children;
    }

    private void ActivateObject(GameObject obj)
    {
        obj.SetActive(true);
        obj.tag = "Untagged"; 
    }

    private void RemoveComponents(GameObject targetGameObject)
    {
        // 削除対象のomponentを列挙
        List<Component> componentsToRemove;
        if (_Settings.IncludePhysBone == true)
        {
            componentsToRemove = targetGameObject.GetComponents<Component>()
                .Where(c => !(c is Transform) && !(c is SkinnedMeshRenderer)&& !(c is VRCPhysBone) && !(c is VRCPhysBoneCollider))
                .ToList();
        }
        else
        {
            componentsToRemove = targetGameObject.GetComponents<Component>()
                .Where(c => !(c is Transform) && !(c is SkinnedMeshRenderer))
                .ToList();
        }

        foreach (var component in componentsToRemove)
        {
            UnityEngine.Object.DestroyImmediate(component, true);
        }
    }

    private HashSet<GameObject> FindPhysBoneObjects(GameObject root, HashSet<GameObject> weightedBones)
    {
        var physBoneObjects = new HashSet<GameObject>();

        // PhysBoneに対する処理
        foreach (VRCPhysBone physBone in root.GetComponentsInChildren<VRCPhysBone>(true))
        {
            if (physBone.rootTransform == null) physBone.rootTransform = physBone.transform;
            var weightedPBObjects = GetWeightedPhysBoneObjects(physBone, weightedBones);

            // 有効なPhysBoneだった場合
            if (weightedPBObjects.Count > 0)
            {
                //MAの仕様に反し衣装側のPBを強制
                if (_Settings.RenameRootTransform == true)
                {
                    physBone.rootTransform.name += ".1";
                }

                physBoneObjects.Add(physBone.gameObject);
                physBoneObjects.UnionWith(weightedPBObjects);

                if (_Settings.IncludePhysBoneColider == true)
                {
                    foreach (VRCPhysBoneCollider collider in physBone.colliders)
                    {
                        if (collider == null) continue;
                        if (collider.rootTransform == null) collider.rootTransform = collider.transform;
                        physBoneObjects.Add(collider.gameObject);
                        physBoneObjects.Add(collider.rootTransform.gameObject);
                    }
                }
            }
            
            // 無効なPhysBoneはここで削除
            else UnityEngine.Object.DestroyImmediate(physBone, true);
        }

        // PhysBoneColiderに対する処理
        ProcessPhysBoneColliders(root, physBoneObjects);

        return physBoneObjects;
    }

    private void AddSingleChildRecursive(Transform transform, HashSet<GameObject> result, HashSet<Transform> ignoreTransforms)
    {   
        if (_Settings.IncludeIgnoreTransforms == false && ignoreTransforms.Contains(transform)) return;
        result.Add(transform.gameObject);   
        if (transform.childCount == 1)
        {
            Transform child = transform.GetChild(0);
            AddSingleChildRecursive(child, result, ignoreTransforms);
        }
    }

    private HashSet<Transform> GetIgnoreTransforms(VRCPhysBone physBone)
    {
        HashSet<Transform> AffectedIgnoreTransforms = new HashSet<Transform>();

        foreach (Transform ignoreTransform in physBone.ignoreTransforms)
        {   
            if (ignoreTransform == null) continue;
            Transform[] AffectedIgnoreTransform = GetAllChildren(ignoreTransform.gameObject);
            AffectedIgnoreTransforms.UnionWith(AffectedIgnoreTransform);
        }

        return AffectedIgnoreTransforms;
    }


    private HashSet<GameObject> GetWeightedPhysBoneObjects(VRCPhysBone physBone, HashSet<GameObject> weightedBones)
    {
        var WeightedPhysBoneObjects = new HashSet<GameObject>();
        HashSet<Transform> ignoreTransforms = GetIgnoreTransforms(physBone);

        Transform[] allchildren = GetAllChildren(physBone.rootTransform.gameObject);

        foreach (Transform child in allchildren)
        {
            if (weightedBones.Contains(child.gameObject))
            {
                if (_Settings.RemainAllPBTransforms == true)
                {
                    WeightedPhysBoneObjects.UnionWith(allchildren.Select(t => t.gameObject));
                    break;

                }
                HashSet<GameObject> result = new HashSet<GameObject>();
                AddSingleChildRecursive(child, result, ignoreTransforms);
                WeightedPhysBoneObjects.UnionWith(result);
            }
        }

        return WeightedPhysBoneObjects;
    }

    private void ProcessPhysBoneColliders(GameObject root, HashSet<GameObject> physBoneObjects)
    {
        foreach (VRCPhysBoneCollider collider in root.GetComponentsInChildren<VRCPhysBoneCollider>(true))
        {   
            // 必要なPhysBoneColliderに対する処理
            if (physBoneObjects.Contains(collider.gameObject))
            {
                //MAの仕様に反し衣装側のPBCを強制
                if (_Settings.RenameRootTransform == true)
                {
                    collider.rootTransform.name += ".1";
                    //Debug.Log(collider.rootTransform.name);
                }
            }

            // 不要なPhysBoneColliderはここで削除
            else
            {
                UnityEngine.Object.DestroyImmediate(collider, true);
            }
        }
    }

}