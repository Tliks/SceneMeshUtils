using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class LocalizationEditor
{
    
    private static Dictionary<string, string[]> _LocalizedText = new Dictionary<string, string[]>
    {
        { "advancedoptions", new string[] {"Advanced Options", "高度なオプション"}},
        { "UndoButton", new string[] { "Undo (Ctrl+Z)", "元に戻す (Ctrl+Z)" } },
        { "RedoButton", new string[] { "Redo (Ctrl+Y)", "やり直し (Ctrl+Y)" } },
        { "PreviewModeLabel", new string[] { "Preview Mode: ", "プレビューモード: " } },
        { "SelectedMesh", new string[] { "Selected Mesh", "選択されたメッシュ" } },
        { "UnselectedMesh", new string[] { "Unselected Mesh", "選択されていないメッシュ" } },
        { "SwitchPreviewModeButton", new string[] { "Switch Preview Mode", "プレビューモード切替" } },
        { "SkinnedMeshRendererField", new string[] { "Skinned Mesh Renderer", "スキンメッシュレンダラー" } },
        { "PreviewMeshButton", new string[] { "Preview Mesh", "メッシュプレビュー" } },
        { "CalculateIslandsButton", new string[] { "Calculate Islands", "アイランド計算" } },
        { "EnableSelectionButton", new string[] { "Enable Selection", "選択を有効化" } },
        { "DisableSelectionButton", new string[] { "Disable Selection", "選択を無効化" } },
        { "SelectedTotalPolygonsLabel", new string[] { "Selected/Total Polygons", "選択中/全ポリゴン" } },
        { "SelectAllButton", new string[] { "Select All", "すべてを選択" } },
        { "UnselectAllButton", new string[] { "Unselect All", "すべての選択を解除" } },
        { "ReverseAllButton", new string[] { "Reverse All", "選択を反転" } },
        { "CreateModuleButton", new string[] { "Create Module", "モジュール作成" } },
        { "CreateBothModulesButton", new string[] { "Create both selected and unselected modules", "選択・非選択両方のモジュールを作成" } },
        { "SplitMeshMoreToggle", new string[] { "Split mesh more", "メッシュをさらに分割" } },
        { "SelectAllInRangeToggle", new string[] { "Select all in range", "範囲内をすべて選択" } },
        { "PhysBoneToggle", new string[] { "PhysBone", "PhysBone" } },
        { "PhysBoneColiderToggle", new string[] { "PhysBoneColider", "PhysBoneColider" } },
        { "AdditionalTransformsToggle", new string[] { "Additional Transforms", "追加のTransform" } },
        { "IncludeIgnoreTransformsToggle", new string[] { "Include IgnoreTransforms", "IgnoreTransformsを含める" } },
        { "RenameRootTransformToggle", new string[] { "Rename RootTransform", "RootTransformの名前を変更" } },
        { "SpecifyRootObjectLabel", new string[] { "Specify Root Object", "ルートオブジェクトを指定" } },
        { "EncodedIslandsLabel", new string[] { "Encoded Selection:", "エンコードされた選択範囲:" } },
        { "tooltip.AdditionalTransformsToggle", new string[] { "Output Additional PhysBones Affected Transforms for exact PhysBone movement", "正確なPhysBoneの動きのために追加のPhysBones Affected Transformsを出力" } },
        { "tooltip.IncludeIgnoreTransformsToggle", new string[] { "Output PhysBone's IgnoreTransforms", "PhysBoneのIgnoreTransformsを出力する" } },
        { "tooltip.RenameRootTransformToggle", new string[] { "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.", "推奨されません。モジュラーアバターの仕様により、場合によっては衣装側の物理ボーンが削除されることがあります。そのため、PhysBoneのRootTransformの名前を変更することで、衣装側の物理ボーンが統合されることを保証します。これにより重複が発生する可能性があります。" } },
        { "tooltip.SpecifyRootObjectLabel", new string[] { "The default root object is the parent object of the specified skinned mesh renderer object", "デフォルトのルートオブジェクトは、指定されたSkinned Mesh Rendererオブジェクトの親オブジェクトです" } },
        { "tooltip.SplitMeshMoreToggle", new string[] {"Splits the mesh into smaller pieces. This is off by default as it can create unnecessarily small separations.", "メッシュをさらに細かく分離します。無意味に小さな分離をすることがあるのでデフォルトではオフになっています。"}},
        { "tooltip.SelectAllInRangeToggle", new string[] {"Range selection options. By default, only islands that are completely within the dragged range are selected. Change this so that islands are selected even if only a part of them is within the range.", "範囲選択に関するオプションです。デフォルトでは、ドラッグされた範囲内に完全に含まれているアイランドのみが選択されます。これを、アイランドの一部でも範囲内にある場合に選択されるように変更します。"}}
    };
    
    private const string PreferenceKey = "com.aoyon.module-creator.lang";
    public static string[] availableLanguages = new[] { "English", "日本語" };
    public static int selectedLanguage = Array.IndexOf(availableLanguages, EditorPrefs.GetString(PreferenceKey, availableLanguages[0]));

    public static string GetLocalizedText(string key)
    {
        int languageIndex = selectedLanguage;
        if (_LocalizedText.ContainsKey(key))
        {
            return _LocalizedText[key][languageIndex];
        }

        return $"[Missing: {key}]";
    }

    public static void SetLanguage(int languageIndex)
    {
        selectedLanguage = languageIndex;
        EditorPrefs.SetString(PreferenceKey, availableLanguages[languageIndex]);
    }

    public static void RenderLocalize()
    {
        selectedLanguage = EditorGUILayout.Popup("Language", selectedLanguage, availableLanguages);
        SetLanguage(selectedLanguage);
    }

}