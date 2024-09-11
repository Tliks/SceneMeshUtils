using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace com.aoyon.scenemeshutils
{   

    public class ModuleCreator : EditorWindow
    {

        private List<SkinnedMeshRenderer> _skinnedMeshRenderers;

        private GameObject _preview;
        private IEnumerable<SkinnedMeshRenderer> _previewRenderers;

        [System.Serializable]
        public class ListWrapper
        {
            public List<int> List = new();
        }
        [SerializeField]
        private List<ListWrapper> _targetselections;
        private RenderSelector[] _renderSelectors;

        private SerializedObject serializedObject;
        private Vector2 scrollPosition;


        private static ModuleCreatorSettings _Settings = new ModuleCreatorSettings
        {
            IncludePhysBone = true,
            IncludePhysBoneColider = true
        };

        private bool _mergePrefab = false;
        private bool _outputunselected = false;
        private static bool _showAdvancedOptions = false;


        public static void ShowWindow(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            ModuleCreator window = GetWindow<ModuleCreator>();
            window.Initialize(skinnedMeshRenderers);
            window.Show();
        }

        private void Initialize(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            _skinnedMeshRenderers = skinnedMeshRenderers.ToList();

            (_preview, _previewRenderers) = ModuleCreatorProcessor.CopyRenderers(_skinnedMeshRenderers);
            _preview.transform.position += new Vector3(-100, 0, -100);

            serializedObject = new SerializedObject(this);
            serializedObject.Update();

            SerializedProperty listProperty = serializedObject.FindProperty("_targetselections");
            listProperty.arraySize = _skinnedMeshRenderers.Count();

            _renderSelectors = new RenderSelector[_skinnedMeshRenderers.Count()];
            for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
            {
                var renderSelector = CreateInstance<RenderSelector>();
                renderSelector.Initialize(_skinnedMeshRenderers[i], listProperty.GetArrayElementAtIndex(i).FindPropertyRelative("List"));
                _renderSelectors[i] = renderSelector;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnDestroy()
        {
            DestroyImmediate(_preview);
            foreach(var renderSelector in _renderSelectors)
            {
                renderSelector.Dispose();
            }
        }
        
        void OnGUI()
        {
            serializedObject.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 冒頭
            LocalizationEditor.RenderLocalize();
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.description"), MessageType.Info);
            
            // 各Rendererに対するUI
            EditorGUILayout.Space();
            for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    float width3 = 80f;
                    float margin = 14f;
                    float remainingWidth = position.width - width3 - margin;

                    float width1 = remainingWidth * 0.65f;
                    float width2 = remainingWidth * 0.35f;

                    _skinnedMeshRenderers[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(_skinnedMeshRenderers[i], typeof(SkinnedMeshRenderer), true, GUILayout.Width(width1));

                    _renderSelectors[i].RenderTriangleSelection("(100%)", new GUILayoutOption[]{ GUILayout.Width(width2)});

                    string[] labels = new string[]{ "Edit", "Edit", "Close" };
                    GUILayoutOption[] options = new GUILayoutOption[]{ GUILayout.Width(80f), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
                    _renderSelectors[i].RenderEditSelection(labels, options);
                }
            }


            // オプション
            EditorGUILayout.Space();
        
            _Settings.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneToggle"), _Settings.IncludePhysBone);

            GUI.enabled = _Settings.IncludePhysBone;
            _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneColiderToggle"), _Settings.IncludePhysBoneColider);
            GUI.enabled = true;

            EditorGUILayout.Space();

            _mergePrefab = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.MergePrefab"), _mergePrefab);
            _outputunselected = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.OutputUnselcted"), _outputunselected);


            // 実行ボタン
            EditorGUILayout.Space();
            GUI.enabled = _skinnedMeshRenderers != null & _skinnedMeshRenderers.Count() > 0;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.CreateModuleButton")))
            {
                CreateModule();
                Close();
            }
            GUI.enabled = true;


            // 高度なオプション
            EditorGUILayout.Space();
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.advancedoptions"));
            if (_showAdvancedOptions)
            { 
                GUI.enabled = _Settings.IncludePhysBone;

                GUIContent content_at = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.AdditionalTransformsToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.AdditionalTransformsToggle"));
                _Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _Settings.RemainAllPBTransforms);

                GUIContent content_ii = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.IncludeIgnoreTransformsToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle"));
                _Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _Settings.IncludeIgnoreTransforms);

                GUIContent content_rr = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.RenameRootTransformToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.RenameRootTransformToggle"));
                _Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _Settings.RenameRootTransform);

                GUI.enabled = true;

                GUIContent content_sr = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.SpecifyRootObjectLabel"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel"));
                _Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _Settings.RootObject, typeof(GameObject), true);    
            }


            EditorGUILayout.EndScrollView();

            if (serializedObject != null && serializedObject.targetObject != null)
            {
                serializedObject.ApplyModifiedProperties();
            }
            
        }

        private void CreateModule()
        {
            IEnumerable<GameObject> objs = _skinnedMeshRenderers.Select(r => r.gameObject);
            GameObject root = CheckUtility.CheckObjects(objs);

            if (_mergePrefab)
            {
                string mesh_name = $"{root.name} Parts";
                (GameObject new_root, string variantPath) = ModuleCreatorProcessor.SaveRootObject(root, mesh_name);
                new_root.transform.position = Vector3.zero;

                List<SkinnedMeshRenderer> newskinnedMeshRenderers = ModuleCreatorProcessor.GetRenderers(root, new_root, objs).ToList();

                for (int i = 0; i < newskinnedMeshRenderers.Count(); i++)
                {
                    var triangleindies = _targetselections[i].List;
                    if (triangleindies.Count() > 0)
                    {
                        Mesh newMesh = MeshUtility.KeepMesh(newskinnedMeshRenderers[i].sharedMesh, triangleindies.ToHashSet());
                        string path = AssetPathUtility.GenerateMeshPath(root.name, "PartialMesh");
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        newskinnedMeshRenderers[i].sharedMesh = newMesh;
                    }
                }

                ModuleCreatorProcessor.CreateModule(new_root, newskinnedMeshRenderers, _Settings, root.scene);
                Debug.Log("Saved prefab to " + variantPath);
            }
            else
            {
                for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
                {
                    string mesh_name = _skinnedMeshRenderers[i].name;
                    (GameObject new_root, string variantPath) = ModuleCreatorProcessor.SaveRootObject(root, mesh_name);
                    new_root.transform.position = Vector3.zero;

                    var target = new List<GameObject> { _skinnedMeshRenderers[i].gameObject };
                    List<SkinnedMeshRenderer> newskinnedMeshRenderers = ModuleCreatorProcessor.GetRenderers(root, new_root, target).ToList();
                    var triangleindies = _targetselections[i].List;
                    if (triangleindies.Count() > 0)
                    {
                        Mesh newMesh = MeshUtility.KeepMesh(newskinnedMeshRenderers[i].sharedMesh, triangleindies.ToHashSet());
                        string path = AssetPathUtility.GenerateMeshPath(root.name, "PartialMesh");
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        newskinnedMeshRenderers[i].sharedMesh = newMesh;
                    }
                    ModuleCreatorProcessor.CreateModule(new_root, newskinnedMeshRenderers, _Settings, root.scene);
                    Debug.Log("Saved prefab to " + variantPath);

                }

            }
                
        }


    }
}