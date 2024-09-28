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
            {"TriangleSelection.TriangleSelection", new string[] {"Selection", "選択箇所"} },
            {"TriangleSelection.Remove", new string[] {"Remove", "削除"} },
            {"TriangleSelection.Add", new string[] {"Add New Selection", "新しい選択箇所を追加"} },
            {"TriangleSelection.Edit", new string[] {"Edit Current Selection", "現在の選択箇所を編集"} },
            {"TriangleSelection.CloseSelector", new string[] {"Close Triangle Selector", "Triangle Selectorを閉じる"} },
            {"TriangleSelection.EnablePreview", new string[] {"Enable NDMF Preview", "NDMFプレビューを有効化"} },
            {"TriangleSelection.DisablePreview", new string[] {"Disable NDMF Preview", "NDMFプレビューを無効化"} },

            { "TriangleSelector.SelectedTotalPolygonsLabel", new string[] { "Selected/Total Polygons", "選択中/全ポリゴン" } },
            { "TriangleSelector.commondescription", new string[] {"You can select parts of the mesh by clicking on the scene. You can also select a range by dragging.", "シーン上でクリックすることでメッシュの一部を選択できます。ドラッグすることで範囲選択も可能です。"}},
            { "TriangleSelector.SelectAllButton", new string[] { "Select All", "すべて選択" } },
            { "TriangleSelector.UnselectAllButton", new string[] { "Unselect All", "すべて解除" } },
            { "TriangleSelector.ReverseAllButton", new string[] { "Reverse All", "すべて反転" } },
            { "TriangleSelector.UndoButton", new string[] { "Undo (Ctrl+Z)", "元に戻す (Ctrl+Z)" } },
            { "TriangleSelector.RedoButton", new string[] { "Redo (Ctrl+Y)", "やり直す (Ctrl+Y)" } },
            { "TriangleSelector.EnableSelectionButton", new string[] { "Resume Selection", "選択を再開" } },
            { "TriangleSelector.DisableSelectionButton", new string[] { "Stop Selection", "選択を停止" } },
            { "TriangleSelector.SelectionMode", new string[] { "SelectMode", "選択モード" }},

            { "TriangleSelector.islandMode", new string[] { "Island", "アイランド" }},
            { "TriangleSelector.island.description", new string[] {"In Island Mode, you can select structurally separated parts of the mesh (islands). For range selection, drag to ensure the desired  island is completely within the selection range.", "アイランドモードでは構造的に分離されたメッシュの一部(アイランド)が選択できます。範囲選択では選択したいアイランドが範囲内に完全に入るようにドラックしてください。"}},
            { "TriangleSelector.island.SplitMeshMoreToggle", new string[] { "Split mesh more", "メッシュをさらに分割" } },
            { "TriangleSelector.island.SplitMeshMoreToggle.description", new string[] {" Split the mesh into more detailed islands.", "メッシュをさらに多くのアイランドに細かく分離します。"}},
            { "TriangleSelector.island.SelectAllInRangeToggle", new string[] { "Select all in range", "範囲内をすべて選択" } },
            { "TriangleSelector.island.SelectAllInRangeToggle.description", new string[] {"Range selection options. Change this to select island that are even partially within the range.", "範囲選択に関するオプションです。一部でも範囲内にあるアイランドも選択されるように変更します。"}},
            
            { "TriangleSelector.polygonMode", new string[] { "Polygon", "ポリゴン" }},
            { "TriangleSelector.polygon.description", new string[] {"In Polygon Mode, you can directly select polygons.", "ポリゴンモードではポリゴンを直接選択できます。"}},
            { "TriangleSelector.polygon.scale", new string[] { "Scale", "スケール" }},
            { "TriangleSelector.polygon.scale.description", new string[] { "The value that determines how far polygons from the clicked point are included in the selection.", "クリックした箇所からどれだけ離れた位置にあるポリゴンまで選択対象に含めるかの値です。" }},

            { "TriangleSelector.SelectionName", new string[] { "SaveName(Optional)", "保存名(オプション)"}},
            { "TriangleSelector.SaveMode", new string[] { "Save Method", "保存方法"}},
            { "TriangleSelector.SaveMode.overwrite", new string[] { "Overwrite", "上書き"}},
            { "TriangleSelector.SaveMode.EditNew", new string[] { "New", "新規"}},
            { "TriangleSelector.Apply", new string[] { "Apply", "適用"}},
        

            { "Utility.ModuleCreator.description", new string[] { "Generate a Prefab containing the mesh of the selection.", "選択した箇所のメッシュを含むPrefabを生成します。" }},
            { "Utility.ModuleCreator.advancedoptions", new string[] {"Advanced Options", "高度なオプション"}},
            { "Utility.ModuleCreator.CreateModuleButton", new string[] { "Create Module", "モジュールを作成" } },
            { "Utility.ModuleCreator.OutputUnselcted", new string[] { "Include Unselected Module", "未選択のモジュールも含める" } },
            { "Utility.ModuleCreator.MergePrefab", new string[] { "Combine into a Single Prefab", "一つのPrefabにまとめる" } },
            { "Utility.ModuleCreator.PhysBoneToggle", new string[] { "PhysBone", "PhysBone" } },
            { "Utility.ModuleCreator.PhysBoneColiderToggle", new string[] { "PhysBoneColider", "PhysBoneColider" } },
            { "Utility.ModuleCreator.AdditionalTransformsToggle", new string[] { "Additional Transforms", "追加のTransform" } },
            { "Utility.ModuleCreator.IncludeIgnoreTransformsToggle", new string[] { "Include IgnoreTransforms", "IgnoreTransformsを含める" } },
            { "Utility.ModuleCreator.RenameRootTransformToggle", new string[] { "Rename RootTransform", "RootTransformの名前を変更" } },
            { "Utility.ModuleCreator.SpecifyRootObjectLabel", new string[] { "Specify Root Object", "ルートオブジェクトを指定" } },
            { "Utility.ModuleCreator.tooltip.AdditionalTransformsToggle", new string[] { "Output Additional PhysBones Affected Transforms for exact PhysBone movement", "正確なPhysBoneの動きのために追加のPhysBones Affected Transformsを出力" } },
            { "Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle", new string[] { "Output PhysBone's IgnoreTransforms", "PhysBoneのIgnoreTransformsを出力する" } },
            { "Utility.ModuleCreator.tooltip.RenameRootTransformToggle", new string[] { "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.", "推奨されません。モジュラーアバターの仕様により、場合によっては衣装側の物理ボーンが削除されることがあります。そのため、PhysBoneのRootTransformの名前を変更することで、衣装側のPhysBoneが確実に統合されるようにします。これにより重複が発生する可能性があります。" } },
            { "Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel", new string[] { "The default root object is the parent object of the specified skinned mesh renderer object", "デフォルトのルートオブジェクトは、指定されたSkinned Mesh Rendererがついたオブジェクトの親オブジェクトです" } },

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
            
            { "Utility.DeleteMesh", new string[] { "Delete Mesh", "メッシュを削除" }},
            { "Utility.DeleteMesh.description", new string[] { "Delete the mesh of the selection non-destructively.", "選択した箇所のメッシュを非破壊で削除します" }},

            { "Utility.BlendShape", new string[] { "Generate Shrink BlendShape", "縮小ブレンドシェイプを生成" }},
            { "Utility.BlendShape.description", new string[] { "Add a blendShape to shrink the selection non-destructively.", "選択した箇所を縮小させるBlendShapeを非破壊で追加します" }},

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