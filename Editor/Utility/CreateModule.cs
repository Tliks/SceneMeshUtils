using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace com.aoyon.scenemeshutils
{   

    public class ModuleCreator : EditorWindow
    {
        private static bool _showAdvancedOptions = false;
        private static ModuleCreatorSettings _Settings = new ModuleCreatorSettings
        {
            IncludePhysBone = true,
            IncludePhysBoneColider = true
        };

        private SkinnedMeshRenderer[] _skinnedMeshRenderers;
        [SerializeField]
        private List<List<int>> _targetselections = new();
        private RenderSelector[] _renderSelectors;
        private bool _outputunselected = false;
        private SerializedObject serializedObject;

        public static void ShowWindow(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            ModuleCreator window = GetWindow<ModuleCreator>();
            window.Initialize(skinnedMeshRenderers);
            window.Show();
        }

        private void Initialize(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            _skinnedMeshRenderers = skinnedMeshRenderers;

            serializedObject = new SerializedObject(this);

            _renderSelectors = new RenderSelector[_skinnedMeshRenderers.Length];
            for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
            {
                var renderSelector = CreateInstance<RenderSelector>();                
                renderSelector.Initialize(_skinnedMeshRenderers[i], serializedObject.FindProperty("_targetselections").GetArrayElementAtIndex(i));
                _renderSelectors[i] = renderSelector;
            }
        }

        void OnDestroy()
        {
            foreach(var renderSelector in _renderSelectors)
            {
                renderSelector.Dispose();
            }
        }
        
        void OnGUI()
        {
            serializedObject.Update();

            for (int i = 0; i < numberOfRenderers; i++)
            {
                _skinnedMeshRenderers[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(_skinnedMeshRenderers[i], typeof(SkinnedMeshRenderer), true);
            }

            _renderSelector.RenderGUI();

            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.description"), MessageType.Info);

            EditorGUILayout.Space();
            RenderPhysBoneOptions();

            EditorGUILayout.Space();

            RenderCreateModuleButtons();
            EditorGUILayout.Space();
            
            process_advanced_options();

            if (serializedObject != null && serializedObject.targetObject != null)
            {
                serializedObject.ApplyModifiedProperties();
            }
            
        }

        private static void RenderPhysBoneOptions()
        {
            EditorGUILayout.Space();

            _Settings.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneToggle"), _Settings.IncludePhysBone);

            GUI.enabled = _Settings.IncludePhysBone;
            _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneColiderToggle"), _Settings.IncludePhysBoneColider);
            GUI.enabled = true;
        }

        private void CreateModule(HashSet<int> Triangles, bool iskeep)
        {
            if (Triangles.Count > 0)
            {   
                Mesh newMesh;
                if (iskeep)
                {
                    newMesh = MeshUtility.KeepMesh(_originskinnedMeshRenderer.sharedMesh, Triangles);
                }
                else
                {
                    newMesh = MeshUtility.DeleteMesh(_originskinnedMeshRenderer.sharedMesh, Triangles);
                }
                
                string rootname = CheckUtility.CheckRoot(_originskinnedMeshRenderer.gameObject).name;
                string path = AssetPathUtility.GenerateMeshPath(rootname, "PartialMesh");
                AssetDatabase.CreateAsset(newMesh, path);
                AssetDatabase.SaveAssets();

                _Settings.newmesh = newMesh;
                ModuleCreatorProcessor.CheckAndCopyBones(new List<GameObject> {_originskinnedMeshRenderer.gameObject}, _Settings);
            }
        }
        
        private void RenderCreateModuleButtons()
        {
            GUI.enabled = _originskinnedMeshRenderer != null && _targetselection.Count > 0;

            _outputunselected = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.OutputUnselcted"), _outputunselected);
            
            // Create Selected Islands Module
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.CreateModuleButton")))
            {
                CustomAnimationMode.StopAnimationMode();
                CreateModule(_targetselection.ToHashSet(), true);
                if (_outputunselected) CreateModule(_targetselection.ToHashSet(), false);
                Close();
            }

            GUI.enabled = true;
        }

        private static void process_advanced_options()
        {   

            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.advancedoptions"));
            if (_showAdvancedOptions)
            {
                
                GUI.enabled = _Settings.IncludePhysBone;
                GUIContent content_at = new GUIContent(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.AdditionalTransformsToggle"), LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.AdditionalTransformsToggle"));
                _Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _Settings.RemainAllPBTransforms);

                GUIContent content_ii = new GUIContent(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.IncludeIgnoreTransformsToggle"), LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle"));
                _Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _Settings.IncludeIgnoreTransforms);

                GUIContent content_rr = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.RenameRootTransformToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.RenameRootTransformToggle"));
                _Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _Settings.RenameRootTransform);

                GUI.enabled = true;

                GUIContent content_sr = new GUIContent(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.SpecifyRootObjectLabel"), LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel"));
                _Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _Settings.RootObject, typeof(GameObject), true);
                        
            }

            EditorGUILayout.Space();
        }

    }
}