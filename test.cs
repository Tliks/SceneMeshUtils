using UnityEngine;
using UnityEditor;

public class TexturePreviewWindow : EditorWindow
{
    private Texture2D texture;
    private Rect textureRect;
    private Vector2 textureSize = new Vector2(512, 512);

    [MenuItem("Window/Texture Preview")]
    public static void ShowWindow()
    {
        GetWindow<TexturePreviewWindow>("Texture Preview");
    }

    private void OnGUI()
    {
        // テクスチャのロードなどが必要な場合ここで行う
        texture = (Texture2D)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), false);

        if (texture == null)
            return;
        
        // 保持しているテクスチャを512x512のサイズで表示
        textureRect = GUILayoutUtility.GetRect(textureSize.x, textureSize.y, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(textureRect, texture, null, ScaleMode.ScaleToFit);

        // 現在のイベントを取得
        Event currentEvent = Event.current;

        // マウスクリックイベントが発生した場合の処理
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            Vector2 mousePos = currentEvent.mousePosition;

            if (textureRect.Contains(mousePos))
            {
                // Rect内の相対座標を計算
                Vector2 relativePos = mousePos - textureRect.position;

                // テクスチャのスケールを考慮して座標をマッピング
                float scaleX = texture.width / textureSize.x;
                float scaleY = texture.height / textureSize.y;

                Vector2 texturePos = new Vector2(relativePos.x * scaleX, relativePos.y * scaleY);

                Debug.Log($"Texture Coordinates: {texturePos}");
            }
        }

        // その他のGUI要素
        GUILayout.Label("Other UI Elements and Controls Here");
    }
}