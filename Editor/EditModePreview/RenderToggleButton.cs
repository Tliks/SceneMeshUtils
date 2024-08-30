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
                    StopNDMFPreview(toggleNode);
                }
            }
            else
            {
                if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelection.EnablePreview")))
                {
                    StartNDMFPreview(toggleNode);
                }
            }
        }

        public static void StartNDMFPreview(TogglablePreviewNode toggleNode)
        {
            toggleNode.IsEnabled.Value = true;
        }

        public static void StopNDMFPreview(TogglablePreviewNode toggleNode)
        {
            toggleNode.IsEnabled.Value = false;
        }
    }
}

