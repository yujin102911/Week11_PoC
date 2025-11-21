using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    [Header("그리드 정보")]
    [SerializeField] private string gridId = "Grid_01";
    [SerializeField] private GridType gridType = GridType.Inventory;

    [Header("격자 설정")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 100f;
    [SerializeField] private float spacing = 10f;

    [Header("참조")]
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private RectTransform gridRectTransform;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    private GridCell[,] gridArray;
    private List<GridCell> currentHighlightedCells = new List<GridCell>();

    // 프로퍼티
    public string GridId => gridId;
    public GridType Type => gridType;
    public int Width => width;
    public int Height => height;
    public RectTransform RectTransform => gridRectTransform;

    private void Awake()
    {
        if (gridRectTransform == null)
            gridRectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        SetupGridLayout();
        GenerateGrid();

        // GridManager에 등록
        GridManager.Instance?.RegisterGrid(this);
    }

    private void OnDestroy()
    {
        GridManager.Instance?.UnregisterGrid(this);
    }

    private void SetupGridLayout()
    {
        if (gridLayoutGroup == null)
            gridLayoutGroup = gridContainer.GetComponent<GridLayoutGroup>();

        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
            gridLayoutGroup.spacing = new Vector2(spacing, spacing);
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = width;
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.LowerLeft;
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        }
    }

    public void GenerateGrid()
    {
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
        gridArray = new GridCell[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridCell newCell = Instantiate(cellPrefab, gridContainer);
                newCell.name = $"{this.name}_Cell_{x}_{y}";
                newCell.Init(x, y, this); // Grid 참조 전달
                gridArray[x, y] = newCell;
            }
        }
    }

    public void ResizeGrid(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        SetupGridLayout();
        GenerateGrid();
    }

    #region Position Helpers

    /// <summary>
    /// 스크린 좌표가 이 그리드 영역 안에 있는지 확인
    /// </summary>
    public bool ContainsScreenPoint(Vector2 screenPos, Camera cam = null)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(gridRectTransform, screenPos, cam);
    }

    /// <summary>
    /// 월드 좌표에서 그리드 인덱스 찾기
    /// </summary>
    public Vector2Int GetGridIndexFromWorldPos(Vector3 worldPos)
    {
        float minDist = float.MaxValue;
        Vector2Int closest = new Vector2Int(-1, -1);
        float threshold = (cellSize + spacing) * 0.75f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridArray[x, y] == null) continue;

                float dist = Vector3.Distance(worldPos, gridArray[x, y].transform.position);
                if (dist < minDist && dist < threshold)
                {
                    minDist = dist;
                    closest = new Vector2Int(x, y);
                }
            }
        }
        return closest;
    }

    #endregion

    #region Preview

    public void ShowPreviewWithShape(Vector2Int startCoords, List<Vector2Int> shape, Color color)
    {
        ClearPreview();
        if (shape == null || shape.Count == 0) return;

        bool isValid = CheckValidityWithShape(startCoords, shape);

        foreach (Vector2Int offset in shape)
        {
            int targetX = startCoords.x + offset.x;
            int targetY = startCoords.y + offset.y;

            if (!IsInBounds(targetX, targetY)) continue;

            GridCell cell = gridArray[targetX, targetY];
            cell.SetHighlight(true, isValid);
            currentHighlightedCells.Add(cell);
        }
    }

    public void ClearPreview()
    {
        foreach (var cell in currentHighlightedCells)
        {
            if (cell != null) cell.SetHighlight(false);
        }
        currentHighlightedCells.Clear();
    }

    #endregion

    #region Validation

    public bool CheckValidityWithShape(Vector2Int startCoords, List<Vector2Int> shape)
    {
        if (startCoords.x == -1 || shape == null) return false;

        foreach (Vector2Int offset in shape)
        {
            int targetX = startCoords.x + offset.x;
            int targetY = startCoords.y + offset.y;

            if (!IsInBounds(targetX, targetY)) return false;
            if (gridArray[targetX, targetY].IsOccupied) return false;
        }
        return true;
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    #endregion

    #region Place / Remove Block

    public bool TryPlaceBlockWithShape(Vector2Int startCoords, List<Vector2Int> shape, BlockData blockData)
    {
        if (!CheckValidityWithShape(startCoords, shape)) return false;

        PlacedBlockInfo blockInfo = new PlacedBlockInfo(
            startCoords,
            shape,
            blockData.blockColor,
            blockData,
            this // 어느 그리드에 배치됐는지 저장
        );

        foreach (Vector2Int offset in shape)
        {
            int targetX = startCoords.x + offset.x;
            int targetY = startCoords.y + offset.y;
            gridArray[targetX, targetY].SetOccupied(blockData.blockColor, blockInfo);
        }

        ClearPreview();
        return true;
    }

    public bool TryPlaceWithInfo(Vector2Int startCoords, PlacedBlockInfo blockInfo)
    {
        if (!CheckValidityWithShape(startCoords, blockInfo.Shape)) return false;

        blockInfo.Origin = startCoords;
        blockInfo.OwnerGrid = this;

        foreach (Vector2Int offset in blockInfo.Shape)
        {
            int targetX = startCoords.x + offset.x;
            int targetY = startCoords.y + offset.y;
            gridArray[targetX, targetY].SetOccupied(blockInfo.Color, blockInfo);
        }

        return true;
    }

    public void RemovePlacedBlock(PlacedBlockInfo blockInfo)
    {
        if (blockInfo == null) return;

        foreach (Vector2Int offset in blockInfo.Shape)
        {
            int targetX = blockInfo.Origin.x + offset.x;
            int targetY = blockInfo.Origin.y + offset.y;

            if (IsInBounds(targetX, targetY))
            {
                gridArray[targetX, targetY].Clear();
            }
        }
    }

    public void ClearCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return;
        gridArray[x, y].Clear();
    }

    public void ClearAllCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridArray[x, y].Clear();
            }
        }
    }

    #endregion

    public GridCell GetCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return gridArray[x, y];
    }
}

public enum GridType
{
    Inventory,     //인벤토리
    Storage,       //창고
    Refrigerator,  //냉장고
    Serving,       //판매제출용
}