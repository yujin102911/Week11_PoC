#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockData))]
public class ImagePreviewExampleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "BlockSprite")
            {
                EditorGUILayout.PropertyField(prop);

                // 이미지가 있으면 그 아래에 미리보기 표시
                if (prop.objectReferenceValue != null)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("미리보기:", EditorStyles.boldLabel);

                    var obj = prop.objectReferenceValue;

                    if (obj is Sprite sprite)
                    {
                        //Sprite 타입이면 이렇게
                        Texture2D texture = sprite.texture;
                        Rect rect = sprite.rect;
                        Rect uv = new Rect(
                            rect.x / texture.width,
                            rect.y / texture.height,
                            rect.width / texture.width,
                            rect.height / texture.height
                        );

                        Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));

                        GUI.DrawTextureWithTexCoords(previewRect, texture, uv);

                    }
                    else if (obj is Texture2D tex)
                    {
                        //Texture2D 타입이면 이렇게
                        Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
                        GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
                    }

                    GUILayout.Space(10);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(prop);
            }
        }
        

        serializedObject.ApplyModifiedProperties();

    }
}
#endif
