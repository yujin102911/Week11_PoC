using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Bool2D))]
public class Bool2DDrawer : PropertyDrawer
{
    private const float cellSize = 20f;
    private const float padding = 5f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty heightProp = property.FindPropertyRelative("height");
        int height = heightProp.intValue;

        return (height * cellSize) + padding * 4;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty widthProp = property.FindPropertyRelative("width");
        SerializedProperty heightProp = property.FindPropertyRelative("height");
        SerializedProperty dataProp = property.FindPropertyRelative("data");

        int width = widthProp.intValue;
        int height = heightProp.intValue;

        EditorGUI.LabelField(new Rect(position.x, position.y, 200, 20), label);

        position.y += 20 + padding;

        // y 루프를 height-1에서 0으로 내려가게 함 → Bottom-Left 기준
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                SerializedProperty element = dataProp.GetArrayElementAtIndex(idx);

                // 그릴 때 y를 뒤집지 않고, 루프 자체를 뒤집으면 됨
                int visualY = (height - 1 - y);

                Rect r = new Rect(
                    position.x + x * cellSize,
                    position.y + visualY * cellSize,
                    cellSize,
                    cellSize
                );

                element.boolValue = GUI.Toggle(r, element.boolValue, GUIContent.none);
            }
        }

        //for (int y = 0; y < height; y++)
        //{
        //    for (int x = 0; x < width; x++)
        //    {
        //        int idx = y * width + x;
        //        SerializedProperty element = dataProp.GetArrayElementAtIndex(idx);

        //        Rect r = new Rect(
        //            position.x + x * cellSize,
        //            position.y + y * cellSize,
        //            cellSize,
        //            cellSize
        //        );

        //        element.boolValue = GUI.Toggle(r, element.boolValue, GUIContent.none);
        //    }
        //}
    }
}
