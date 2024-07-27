/*
MIT License

Copyright (c) 2023 suzuryg

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class CustomSceneViewWindow : SceneView
{
    private bool isMouseOver = false;
    private static SceneView _defaultSceneView;

    public static CustomSceneViewWindow ShowWindow(SceneView defaultSceneView)
    {
        var window = CreateWindow<CustomSceneViewWindow>();
        window.titleContent = new GUIContent("Selected Mesh Preview");
        window.Show();
        _defaultSceneView = defaultSceneView;
        SetLastActiveSceneView(_defaultSceneView);
        return window;
    }

    private void OnGUI()
    {
        if (EditorWindow.mouseOverWindow == this)
        {
            if (!isMouseOver)
            {
                isMouseOver = true;
                Debug.Log("OnFocus");
                SetLastActiveSceneView(this);
            }
        }
        else
        {
            if (isMouseOver)
            {
                isMouseOver = false;
                Debug.Log("OnUnFocus");
                SetLastActiveSceneView(_defaultSceneView);
            }
        }
    }
    
    /*
    ref
    https://github.com/suzuryg/face-emo/blob/8ea4a835b0024437643a218086ea348d7c16a851/Packages/jp.suzuryg.face-emo/Editor/Detail/View/ExpressionEditor/ExpressionPreviewWindow.cs#L162-L180
    */
    private static void SetLastActiveSceneView(SceneView sceneView)
    {
        Type sceneViewType = typeof(SceneView);
        PropertyInfo lastActiveSceneViewInfo = sceneViewType.GetProperty("lastActiveSceneView", BindingFlags.Public | BindingFlags.Static);
        if (lastActiveSceneViewInfo != null)
        {
            lastActiveSceneViewInfo.SetValue(null, sceneView, null);
        }
        else
        {
            Debug.LogError("lastActiveSceneView property not found");
        }
    }
}
