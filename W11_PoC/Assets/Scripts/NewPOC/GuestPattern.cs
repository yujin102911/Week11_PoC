using UnityEngine;

[CreateAssetMenu(fileName = "Guest Pattern", menuName = "TestSO", order = 0)]
public class GuestPattern : ScriptableObject
{
    [Header("크기 설정")]
    [SerializeField]
    private int width = 6;
    [SerializeField]
    private int height = 6;

    [Space(10)]
    [Header("손님 패턴")]
    [SerializeField] private Bool2D _patternGrid;

    public Bool2D PatternGrid => _patternGrid;

    private void OnValidate()
    {
        ResizeGrid();
    }

    private void ResizeGrid()
    {

        if (_patternGrid == null || _patternGrid.data == null || _patternGrid.data.Length != width * height)
        {
            _patternGrid = new Bool2D();
            _patternGrid.width = width;
            _patternGrid.height = height;
            _patternGrid.data = new bool[width * height];
        }
    }
}
