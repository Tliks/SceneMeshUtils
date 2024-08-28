using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using nadena.dev.ndmf.preview;

namespace com.aoyon.scenemeshutils
{
    public class NDMFToggleButton
    {
        public static void RenderNDMFToggle(TogglablePreviewNode toggleNode)
        {
            if (toggleNode.IsEnabled.Value)
            {
                if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelection.DisablePreview")))
                {
                    toggleNode.IsEnabled.Value = !toggleNode.IsEnabled.Value;
                }
            }
            else
            {
                if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelection.EnablePreview")))
                {
                    toggleNode.IsEnabled.Value = !toggleNode.IsEnabled.Value;
                }
            }
        }
    }
}

