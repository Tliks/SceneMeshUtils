using System.Collections.Generic;
using com.aoyon.triangleselector.utils;

namespace com.aoyon.scenemeshutils
{
    public class LocalizationEditor
    {
        
        private static Dictionary<string, string[]> _LocalizedText = new Dictionary<string, string[]>
        {
            { "Utility.mask.description", new string[] {"Generate a mask image of the selection", "選択した箇所のマスク画像を生成します"}},
            { "Utility.mask.createtexture", new string[] {"Create Texture", "テクスチャを生成"}},
            { "Utility.mask.resolution", new string[] {"Resolution", "解像度"}},
            { "Utility.mask.areacolor", new string[] {"SelectedAreaColor", "選択箇所の色"}},
            { "Utility.mask.backcolor", new string[] {"BackgroundColor", "背景の色"}},
            { "Utility.mask.color.white", new string[] {"white", "白"}},
            { "Utility.mask.color.black", new string[] {"black", "黒"}},
            { "Utility.mask.color.original", new string[] {"original", "オリジナル"}},
            { "Utility.mask.color.alpha", new string[] {"alpha", "アルファ"}},
            { "Utility.mask.color.grayscale", new string[] {"grayscale", "グレースケール"}},
            { "Utility.mask.expansion", new string[] {"Padding", "パディング"}},
            
            { "Utility.TransformPolygon", new string[] { "Transform Polygon", "ポリゴンを移動" }}
        };
        
        private const string PreferenceKey = "com.aoyon.scenemeshutils.lang";
        private static int selectedLanguageIndex = LocalizationManager.GetSelectedLanguageIndex(PreferenceKey);

        public static string GetLocalizedText(string key)
        {
            return LocalizationManager.GetLocalizedText(_LocalizedText, key, selectedLanguageIndex);
        }

        public static void RenderLocalize()
        {
            LocalizationManager.RenderLocalize(ref selectedLanguageIndex, PreferenceKey);
        }

    }
}