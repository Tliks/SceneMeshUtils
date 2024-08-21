using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.aoyon.scenemeshutils
{
    public class CheckUtility
    {
        public static void Checktarget(GameObject targetObject)
        {
            if (targetObject == null)
            {
                throw new InvalidOperationException("Target object is not set.");
            }
        }

        public static void CheckPrefabAsset(GameObject targetObject)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(targetObject))
            {
                throw new InvalidOperationException("Please run it on the prefab instance in the hierarchy, not on the prefabasset.");
            }
        }

        public static GameObject CheckRoot(GameObject targetObject)
        {
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

        public static void CheckSkin(GameObject targetObject)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                
                throw new InvalidOperationException($"'{targetObject.name}' does not have a SkinnedMeshRenderer.");
            }
        }

        public static void CheckHips(GameObject root)
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
                Debug.LogWarning("Hips could not be found. Merge Armature/Setup Outfit may not work properly.");
            }
        }

        public static SkinnedMeshRenderer CleanUpHierarchy(GameObject new_root, int skin_index)
        {
            return CleanUpHierarchy(new_root, skin_index, null);
        }

        public static SkinnedMeshRenderer CleanUpHierarchy(GameObject new_root, int skin_index, ModuleCreatorSettings settings)
        {
            HashSet<GameObject> objectsToSave = new HashSet<GameObject>();
            HashSet<object> componentsToSave = new HashSet<object>();

            // 複製先のSkinnedMeshRendererがついたオブジェクトを追加
            GameObject skin = GetSkin(new_root, skin_index);
            objectsToSave.Add(skin);

            SkinnedMeshRenderer skinnedMeshRenderer = skin.GetComponent<SkinnedMeshRenderer>();
            componentsToSave.Add(skinnedMeshRenderer);
            if (settings != null && settings.newmesh != null) 
            {
                skinnedMeshRenderer.sharedMesh = settings.newmesh;
                MeshUtility.RemoveUnusedMaterials(skinnedMeshRenderer);
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
            if (settings != null && settings.newmesh != null) 
            {
                (HashSet<GameObject> PhysBoneObjects, HashSet<object> PBComponents)  = FindPhysBoneObjects(new_root, weightedBones, settings);
                objectsToSave.UnionWith(PhysBoneObjects);
                componentsToSave.UnionWith(PBComponents);
            }

            CheckAndDeleteRecursive(new_root, objectsToSave, componentsToSave);

            return skinnedMeshRenderer;
        }

        private static GameObject GetSkin(GameObject new_root, int skin_index)
        {
            Transform[] AllChildren = GetAllChildren(new_root);
            GameObject skin = AllChildren[skin_index].gameObject;
            return skin;
        }

        private static HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
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

        private static void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> objectsToSave, HashSet<object> componentsToSave)
        {   
            List<GameObject> children = GetChildren(obj);

            // 子オブジェクトに対して再帰的に処理を適用
            foreach (GameObject child in children)
            {   
                CheckAndDeleteRecursive(child, objectsToSave, componentsToSave);
            }

            // 削除しない条件
            if (objectsToSave.Contains(obj) || obj.transform.childCount != 0)
            {
                ActivateObject(obj);
                RemoveComponents(obj, componentsToSave);
                return;
            }
            
            UnityEngine.Object.DestroyImmediate(obj, true);
        }

        private static List<GameObject> GetChildren(GameObject parent)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
            }
            return children;
        }

        public static Transform[] GetAllChildren(GameObject parent)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            return children;
        }

        private static void ActivateObject(GameObject obj)
        {
            obj.SetActive(true);
            obj.tag = "Untagged"; 
        }

        private static void RemoveComponents(GameObject targetGameObject, HashSet<object> componentsToSave)
        {
            var components = targetGameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                if (!(component is Transform) && !componentsToSave.Contains(component))
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                }
            }
        }

        private static (HashSet<GameObject>, HashSet<object>) FindPhysBoneObjects(GameObject root, HashSet<GameObject> weightedBones, ModuleCreatorSettings settings)
        {
            var physBoneObjects = new HashSet<GameObject>();
            HashSet<object> componentsToSave = new HashSet<object>();

            // PhysBoneに対する処理
            foreach (VRCPhysBone physBone in root.GetComponentsInChildren<VRCPhysBone>(true))
            {
                if (physBone.rootTransform == null) physBone.rootTransform = physBone.transform;
                var weightedPBObjects = GetWeightedPhysBoneObjects(physBone, weightedBones, settings);

                // 有効なPhysBoneだった場合
                if (weightedPBObjects.Count > 0)
                {
                    componentsToSave.Add(physBone);

                    //MAの仕様に反し衣装側のPBを強制
                    if (settings.RenameRootTransform == true)
                    {
                        physBone.rootTransform.name += ".1";
                    }

                    physBoneObjects.Add(physBone.gameObject);
                    physBoneObjects.UnionWith(weightedPBObjects);

                    if (settings.IncludeIgnoreTransforms == true)
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
                
            }

            // PhysBoneColiderに対する処理
            ProcessPhysBoneColliders(root, physBoneObjects, componentsToSave, settings);

            return (physBoneObjects, componentsToSave);
        }

        private static HashSet<GameObject> GetWeightedPhysBoneObjects(VRCPhysBone physBone, HashSet<GameObject> weightedBones, ModuleCreatorSettings settings)
        {
            var WeightedPhysBoneObjects = new HashSet<GameObject>();
            HashSet<Transform> ignoreTransforms = GetIgnoreTransforms(physBone);

            Transform[] allchildren = GetAllChildren(physBone.rootTransform.gameObject);

            foreach (Transform child in allchildren)
            {
                if (weightedBones.Contains(child.gameObject))
                {
                    if (settings.RemainAllPBTransforms == true)
                    {
                        WeightedPhysBoneObjects.UnionWith(allchildren.Select(t => t.gameObject));
                        break;

                    }
                    HashSet<GameObject> result = new HashSet<GameObject>();
                    AddSingleChildRecursive(child, result, ignoreTransforms, settings);
                    WeightedPhysBoneObjects.UnionWith(result);
                }
            }

            return WeightedPhysBoneObjects;
        }

        private static void AddSingleChildRecursive(Transform transform, HashSet<GameObject> result, HashSet<Transform> ignoreTransforms, ModuleCreatorSettings settings)
        {   
            if (settings.IncludeIgnoreTransforms == false && ignoreTransforms.Contains(transform)) return;
            result.Add(transform.gameObject);   
            if (transform.childCount == 1)
            {
                Transform child = transform.GetChild(0);
                AddSingleChildRecursive(child, result, ignoreTransforms, settings);
            }
        }

        private static HashSet<Transform> GetIgnoreTransforms(VRCPhysBone physBone)
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



        private static void ProcessPhysBoneColliders(GameObject root, HashSet<GameObject> physBoneObjects, HashSet<object> componentsToSave, ModuleCreatorSettings settings)
        {
            foreach (VRCPhysBoneCollider collider in root.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {   
                // 必要なPhysBoneColliderに対する処理
                if (physBoneObjects.Contains(collider.gameObject))
                {
                    componentsToSave.Add(collider);

                    //MAの仕様に反し衣装側のPBCを強制
                    if (settings.RenameRootTransform == true)
                    {
                        collider.rootTransform.name += ".1";
                        //Debug.Log(collider.rootTransform.name);
                    }
                }

            }
        }

    }


}