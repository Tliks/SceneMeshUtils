
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    public class MaskTextureGenerator : EditorWindow
    {
        private SkinnedMeshRenderer _originskinnedMeshRenderer;
        private string _rootname;

        private List<int> _targetselection = new();
        private RenderSelector _renderSelector;

        private int[] optionValues = { 128, 256, 512, 1024, 2048 };
        private int selectedValue = 512;
        private int _areacolorindex = 0;
        private int _backcolorindex = 1;
        private int _expansion = 2;

        public static void ShowWindow(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            MaskTextureGenerator window = GetWindow<MaskTextureGenerator>();
            window.Initialize(skinnedMeshRenderer);
            window.Show();
        }

        private void Initialize(SkinnedMeshRenderer originskinnedMeshRenderer)
        {
            _originskinnedMeshRenderer = originskinnedMeshRenderer;
            _renderSelector = CreateInstance<RenderSelector>();
            _renderSelector.Initialize(_originskinnedMeshRenderer, _targetselection);
            _renderSelector.RegisterApplyCallback(newselection => _targetselection = newselection);
            _rootname = CheckUtility.CheckRoot(originskinnedMeshRenderer.gameObject).name;
        }

        void OnDisable()
        {
            _renderSelector.Dispose();
        }

        void OnGUI()
        {
            _renderSelector.RenderGUI();
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.mask.description"), MessageType.Info);

            EditorGUILayout.Space();
            RenderGenerateMask();            
        }

        public void RenderGenerateMask()
        {
            //EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("mask.description"), MessageType.Info);
            string[] options = { 
                LocalizationEditor.GetLocalizedText("Utility.mask.color.white"), 
                LocalizationEditor.GetLocalizedText("Utility.mask.color.black"), 
                LocalizationEditor.GetLocalizedText("Utility.mask.color.alpha"),
                LocalizationEditor.GetLocalizedText("Utility.mask.color.original"),
                LocalizationEditor.GetLocalizedText("Utility.mask.color.grayscale")
                };

            _areacolorindex = EditorGUILayout.Popup(LocalizationEditor.GetLocalizedText("Utility.mask.areacolor"), _areacolorindex, options);
            _backcolorindex = EditorGUILayout.Popup(LocalizationEditor.GetLocalizedText("Utility.mask.backcolor"), _backcolorindex, options);

            selectedValue = EditorGUILayoutIntPopup(LocalizationEditor.GetLocalizedText("Utility.mask.resolution"), selectedValue, optionValues);
            _expansion = EditorGUILayout.IntField(LocalizationEditor.GetLocalizedText("Utility.mask.expansion"), _expansion);
            
            // Create Selected Islands Module
            GUI.enabled = _originskinnedMeshRenderer != null && _targetselection.Count > 0;
            EditorGUILayout.Space();
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.mask.createtexture")))
            {   
                CustomAnimationMode.StopAnimationMode();
                GenerateMask(_targetselection.ToHashSet());
                Close();
            }
            GUI.enabled = true;

        }

        private static Color[] CreateColorArray(int indexValue, Texture2D originalTexture, int textureSize)
        {
            Color[] colors = new Color[textureSize * textureSize];

            switch (indexValue)
            {
                case 0:
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = Color.white;
                    break;
                case 1:
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = Color.black;
                    break;
                case 2:
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = new Color(0, 0, 0, 0);
                    break;
                case 3:
                    for (int y = 0; y < textureSize; y++)
                    {
                        for (int x = 0; x < textureSize; x++)
                        {
                            colors[y * textureSize + x] = originalTexture.GetPixelBilinear((float)x / textureSize, (float)y / textureSize);
                        }
                    }
                    break;
                case 4:
                    for (int y = 0; y < textureSize; y++)
                    {
                        for (int x = 0; x < textureSize; x++)
                        {
                            Color origColor = originalTexture.GetPixelBilinear((float)x / textureSize, (float)y / textureSize);
                            float gray = origColor.grayscale;
                            colors[y * textureSize + x] = new Color(gray, gray, gray, origColor.a);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return colors;
        }


        private void GenerateMask(HashSet<int> Triangles)
        {
            MeshMaskGenerator generator = new MeshMaskGenerator(selectedValue, _expansion);
            Texture2D originalTexture = GetReadableTexture(_originskinnedMeshRenderer.sharedMaterial.mainTexture as Texture2D);

            Color[] targetColors = CreateColorArray(_areacolorindex, originalTexture, selectedValue);
            Color[] baseColors = CreateColorArray(_backcolorindex, originalTexture, selectedValue);

            Dictionary<string, Texture2D> maskTextures = generator.GenerateMaskTextures(_originskinnedMeshRenderer, Triangles, baseColors, targetColors, _originskinnedMeshRenderer.sharedMesh);
            
            List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>();
            if (maskTextures.Count == 0) Debug.Log("????");
            foreach (KeyValuePair<string, Texture2D> kvp in maskTextures)
            {
                string timeStamp = DateTime.Now.ToString("yyMMdd_HHmmss");
                string path = AssetPathUtility.GenerateTexturePath(_rootname, $"{timeStamp}_{_originskinnedMeshRenderer.name}_{kvp.Key}");
                byte[] bytes = kvp.Value.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();

                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null)
                {
                    selectedObjects.Add(obj);
                    EditorGUIUtility.PingObject(obj);
                    Debug.Log("Saved MaskTexture to " + path);
                }
            }
            //Selection.activeGameObject = null;
            Selection.objects = selectedObjects.ToArray();
        }

        private Texture2D GetReadableTexture(Texture2D originalTexture)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                originalTexture.width,
                originalTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(originalTexture, renderTexture);

            RenderTexture previousRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(renderTexture);

            return readableTexture;
        }

        private static int EditorGUILayoutIntPopup(string label, int selectedValue, int[] optionValues)
        {
            return EditorGUILayout.IntPopup(label, selectedValue, optionValues.Select(i => i.ToString()).ToArray(), optionValues);
        }

    }
}