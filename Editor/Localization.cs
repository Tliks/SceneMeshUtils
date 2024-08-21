using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace com.aoyon.scenemeshutils
{
    public class LocalizationEditor
    {
        
        private static Dictionary<string, string[]> _LocalizedText = new Dictionary<string, string[]>
        {
            { "TriangleSelector.SelectedTotalPolygonsLabel", new string[] { "Selected/Total Polygons", "選択中/全ポリゴン" } },
            { "TriangleSelector.commondescription", new string[] {"You can select parts of the mesh by clicking on the scene. You can also select a range by dragging.", "シーン上でクリックすることでメッシュの一部を選択できます。ドラッグすることで範囲選択も可能です。"}},
            { "TriangleSelector.SelectAllButton", new string[] { "Select All", "すべて選択" } },
            { "TriangleSelector.UnselectAllButton", new string[] { "Unselect All", "すべての選択を解除" } },
            { "TriangleSelector.ReverseAllButton", new string[] { "Reverse All", "選択を反転" } },
            { "TriangleSelector.UndoButton", new string[] { "Undo (Ctrl+Z)", "元に戻す (Ctrl+Z)" } },
            { "TriangleSelector.RedoButton", new string[] { "Redo (Ctrl+Y)", "やり直す (Ctrl+Y)" } },
            { "TriangleSelector.EnableSelectionButton", new string[] { "Resume Selection", "選択を再開" } },
            { "TriangleSelector.DisableSelectionButton", new string[] { "Stop Selection", "選択を停止" } },
            { "TriangleSelector.SelectionMode", new string[] { "SelectionMode: ", "選択モード: " }},

            { "TriangleSelector.island", new string[] { "Island", "アイランド" }},
            { "TriangleSelector.island.description", new string[] {"In Island Mode, you can select structurally separated parts of the mesh (islands). For range selection, drag to ensure the desired  island is completely within the selection range.", "アイランドモードでは構造的に分離されたメッシュの一部(アイランド)が選択できます。範囲選択では選択したいアイランドが範囲内に完全に入るようにドラックしてください。"}},
            { "TriangleSelector.island.SplitMeshMoreToggle", new string[] { "Split mesh more", "メッシュをさらに分割" } },
            { "TriangleSelector.island.SplitMeshMoreToggle.description", new string[] {" Split the mesh into more detailed islands. This is off by default as it can create unnecessarily small separations.", "メッシュをさらに多くのアイランドに細かく分離します。無意味に小さな分離をすることがあるのでデフォルトではオフになっています。"}},
            { "TriangleSelector.island.SelectAllInRangeToggle", new string[] { "Select all in range", "範囲内をすべて選択" } },
            { "TriangleSelector.island.SelectAllInRangeToggle.description", new string[] {"Range selection options. By default, only islands that are completely within the dragged range are selected. Change this to select island that are even partially within the range.", "範囲選択に関するオプションです。デフォルトでは、ドラッグされた範囲内に完全に含まれているアイランドのみが選択されます。これを、一部でも範囲内にあるアイランドも選択されるように変更します。"}},
            
            { "TriangleSelector.polygon", new string[] { "Polygon", "ポリゴン" }},
            { "TriangleSelector.polygon.description", new string[] {"In Polygon Mode, you can directly select polygons.", "ポリゴンモードではポリゴンを直接選択できます。"}},
            { "TriangleSelector.polygon.scale", new string[] { "Scale", "スケール" }},
            { "TriangleSelector.polygon.scale.description", new string[] { "The value that determines how far polygons from the clicked point are included in the selection.", "クリックした箇所からどれだけ離れた位置にあるポリゴンまで選択対象に含めるかの値です。" }},

            { "TriangleSelector.SelectionName", new string[] { "SelectionName(Optional)", "選択箇所の名前(オプション)"}},
            { "TriangleSelector.Apply", new string[] { "Apply", "適用"}},

            { "advancedoptions", new string[] {"Advanced Options", "高度なオプション"}},
            { "SelectedMesh", new string[] { "Selected Mesh", "選択済みのメッシュ" } },
            { "UnselectedMesh", new string[] { "Unselected Mesh", "未選択のメッシュ" } },
            { "SwitchPreviewModeButton", new string[] { "Switch Preview Mode", "プレビューモード切替" } },
            { "SkinnedMeshRendererField", new string[] { "Skinned Mesh Renderer", "Skinned Mesh Renderer" } },
            { "PreviewMeshButton", new string[] { "Preview Mesh", "メッシュプレビュー" } },
            { "CalculateIslandsButton", new string[] { "Calculate Islands", "アイランド計算" } },
            { "CreateModuleButton", new string[] { "Create Module", "モジュールを作成" } },
            { "CreateBothModulesButton", new string[] { "Create both selected and unselected modules", "選択・未選択両方のモジュールを作成" } },
            { "PhysBoneToggle", new string[] { "PhysBone", "PhysBone" } },
            { "PhysBoneColiderToggle", new string[] { "PhysBoneColider", "PhysBoneColider" } },
            { "AdditionalTransformsToggle", new string[] { "Additional Transforms", "追加のTransform" } },
            { "IncludeIgnoreTransformsToggle", new string[] { "Include IgnoreTransforms", "IgnoreTransformsを含める" } },
            { "RenameRootTransformToggle", new string[] { "Rename RootTransform", "RootTransformの名前を変更" } },
            { "SpecifyRootObjectLabel", new string[] { "Specify Root Object", "ルートオブジェクトを指定" } },
            { "EncodedIslandsLabel", new string[] { "Encoded Selection:", "エンコードされた選択範囲:" } },
            { "tooltip.AdditionalTransformsToggle", new string[] { "Output Additional PhysBones Affected Transforms for exact PhysBone movement", "正確なPhysBoneの動きのために追加のPhysBones Affected Transformsを出力" } },
            { "tooltip.IncludeIgnoreTransformsToggle", new string[] { "Output PhysBone's IgnoreTransforms", "PhysBoneのIgnoreTransformsを出力する" } },
            { "tooltip.RenameRootTransformToggle", new string[] { "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.", "推奨されません。モジュラーアバターの仕様により、場合によっては衣装側の物理ボーンが削除されることがあります。そのため、PhysBoneのRootTransformの名前を変更することで、衣装側のPhysBoneが確実に統合されるようにします。これにより重複が発生する可能性があります。" } },
            { "tooltip.SpecifyRootObjectLabel", new string[] { "The default root object is the parent object of the specified skinned mesh renderer object", "デフォルトのルートオブジェクトは、指定されたSkinned Mesh Rendererがついたオブジェクトの親オブジェクトです" } },
            { "GenerateMaskTexture", new string[] {"GenerateMaskTexture", "マスク画像を生成"}},
            { "experimentalOptions", new string[] {"Experimental Options", "実験的な機能"}},
            { "mask.description", new string[] {"Generate a mask image of the selected area", "選択した箇所のマスク画像を生成"}},
            { "mask.resolution", new string[] {"Resolution", "解像度"}},
            { "mask.areacolor", new string[] {"SelectedAreaColor", "選択箇所の色"}},
            { "mask.backcolor", new string[] {"BackgroundColor", "背景の色"}},
            { "mask.color.white", new string[] {"white", "白"}},
            { "mask.color.black", new string[] {"black", "黒"}},
            { "mask.color.original", new string[] {"original", "オリジナル"}},
            { "mask.color.alpha", new string[] {"alpha", "アルファ"}},
            { "mask.color.grayscale", new string[] {"grayscale", "グレースケール"}},
            { "mask.expansion", new string[] {"Padding", "パディング"}},
            { "Utility.description", new string[] { "Action: ", "実行内容: " }},
            { "Utility.None", new string[] { "Please Select", "選択してください" }},
            { "Utility.ModuleCreator", new string[] { "Create Module", "モジュールを生成" }},
            { "Utility.GenerateMask", new string[] { "Generate Mask Texture", "マスク画像を生成" }},
            { "Utility.DeleteMesh", new string[] { "Delete Mesh", "メッシュを削除" }},
            { "Utility.BlendShape", new string[] { "Generate Shrink BlendShape", "縮小ブレンドシェイプを生成" }},
            { "Utility.TransformPolygon", new string[] { "Transform Polygon", "ポリゴンを移動" }}
        };
        
        private const string PreferenceKey = "com.aoyon.module-creator.lang";
        public static string[] availableLanguages = new[] { "English", "日本語" };
        public static int selectedLanguageIndex = Array.IndexOf(availableLanguages, EditorPrefs.GetString(PreferenceKey, availableLanguages[0]));

        public static string GetLocalizedText(string key)
        {
            if (_LocalizedText.ContainsKey(key))
            {
                return _LocalizedText[key][selectedLanguageIndex];
            }

            return $"[Missing: {key}]";
        }

        public static void SetLanguage(int languageIndex)
        {
            selectedLanguageIndex = languageIndex;
            EditorPrefs.SetString(PreferenceKey, availableLanguages[selectedLanguageIndex]);
        }

        public static void RenderLocalize()
        {
            int LanguageIndex = EditorGUILayout.Popup("Language", selectedLanguageIndex, availableLanguages);
            SetLanguage(LanguageIndex);
        }

    }
}