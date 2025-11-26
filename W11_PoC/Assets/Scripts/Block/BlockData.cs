using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewBlock", menuName = "Game/Block Data")]
public class BlockData : ScriptableObject
{
    [Header("기본 정보")]
    public string blockName;
    public Color blockColor = Color.white;

    [Header("이미지")]
    public Sprite BlockSprite;

    [Header("모양(0,0 기준 상대 좌표)")]
    public List<Vector2Int> shape = new List<Vector2Int>();


    public Vector2Int GetBounds()
    {
        if (shape == null || shape.Count == 0)
        {
            return Vector2Int.zero;
        }
        int maxX = 0,  maxY = 0;
        foreach (var pos in shape)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public bool ContainsOffset(Vector2Int offset)
    {
        return shape.Contains(offset);
    }

}
