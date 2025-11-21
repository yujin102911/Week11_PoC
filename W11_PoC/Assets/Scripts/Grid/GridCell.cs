using UnityEngine;
using UnityEngine.UI;

public class GridCell : MonoBehaviour
{
    public Vector2Int Coordinate { get; private set; }
    public bool IsOccupied { get; private set; }

    [SerializeField] private Image cellImage;
    private Color defaultColor;

    /// <summary>
    /// 뭐하는 함수임? 나도 잘 모르겠음 추후에 알아낸 후에 뭐하는 함수인지 작성하도록 하겠음
    /// </summary>
    public void Init(int x, int y)
    {
        Coordinate = new Vector2Int(x, y);
        IsOccupied = false;
        defaultColor = cellImage.color;
    }

    /// <summary>
    /// 이 스크립트가 붙은 격자 셀이 채워지면 색 및 현재 상태를 설정하는 함수
    /// </summary>
    public void SetOccupied(Color color)
    {
        IsOccupied = true;
        cellImage.color = color;
    }

    /// <summary>
    /// 이 스크립트가 붙은 격자 셀에서 제거하면 셀 Clear하는 함수
    /// </summary>
    public void Clear()
    {
        IsOccupied = false;
        cellImage.color = defaultColor;
    }


}
