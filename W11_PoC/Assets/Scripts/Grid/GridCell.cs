using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GridCell : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2Int Coordinate { get; private set; }
    public bool IsOccupied { get; private set; }
    public PlacedBlockInfo PlacedBlock { get; private set; }
    public Grid OwnerGrid { get; private set; }

    [SerializeField] private Image cellImage;

    private Color defaultColor = Color.white;
    private Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.5f);
    private Color invalidHighlightColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    // 드래그 관련
    private static bool isAnyDragging = false; // 전역 드래그 상태
    private bool isDraggingBlock = false;
    private PlacedBlockInfo draggingBlockInfo;
    private Grid originalGrid;
    private CanvasGroup canvasGroup;

    // 드래그 시각적 피드백용
    private GameObject dragVisual;
    private RectTransform dragVisualRect;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (isDraggingBlock && draggingBlockInfo != null)
        {
            HandleRotationInput();

            // 드래그 비주얼 위치 업데이트
            if (dragVisual != null)
            {
                dragVisual.transform.position = Input.mousePosition;
            }
        }
    }

    private void HandleRotationInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
            RotateDraggingBlock(true);
        else if (scroll < 0f)
            RotateDraggingBlock(false);
    }

    private void RotateDraggingBlock(bool clockwise)
    {
        if (draggingBlockInfo == null || draggingBlockInfo.Shape == null) return;

        List<Vector2Int> currentShape = draggingBlockInfo.Shape;
        List<Vector2Int> rotatedShape = new List<Vector2Int>();

        foreach (Vector2Int pos in currentShape)
        {
            Vector2Int newPos = clockwise
                ? new Vector2Int(pos.y, -pos.x)
                : new Vector2Int(-pos.y, pos.x);
            rotatedShape.Add(newPos);
        }

        NormalizeShape(rotatedShape);
        draggingBlockInfo.Shape = rotatedShape;

        UpdateDragVisual();
        UpdatePreviewAtPointer();
    }

    private void NormalizeShape(List<Vector2Int> shape)
    {
        if (shape.Count == 0) return;

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var pos in shape)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
        }

        for (int i = 0; i < shape.Count; i++)
        {
            shape[i] = new Vector2Int(shape[i].x - minX, shape[i].y - minY);
        }
    }

    private void UpdatePreviewAtPointer()
    {
        Vector2 mousePos = Input.mousePosition;
        Grid targetGrid = GridManager.Instance.GetGridAtScreenPos(mousePos);

        if (targetGrid != null)
        {
            Vector2Int gridPos = targetGrid.GetGridIndexFromWorldPos(mousePos);
            GridManager.Instance.ShowPreview(targetGrid, gridPos, draggingBlockInfo.Shape, draggingBlockInfo.Color);
        }
        else
        {
            GridManager.Instance.ClearAllPreviews();
        }
    }

    public void Init(int x, int y, Grid owner)
    {
        Coordinate = new Vector2Int(x, y);
        IsOccupied = false;
        PlacedBlock = null;
        OwnerGrid = owner;
        defaultColor = cellImage.color;
    }

    public void SetOccupied(Color color, PlacedBlockInfo blockInfo = null)
    {
        IsOccupied = true;
        PlacedBlock = blockInfo;
        cellImage.color = color;
    }

    public void Clear()
    {
        IsOccupied = false;
        PlacedBlock = null;
        cellImage.color = defaultColor;
    }

    public void SetHighlight(bool isOn, bool isValid = true)
    {
        if (IsOccupied && isOn) return;

        if (isOn)
            cellImage.color = isValid ? highlightColor : invalidHighlightColor;
        else if (!IsOccupied)
            cellImage.color = defaultColor;
    }

    #region 배치된 블록 드래그 (그리드 간 이동)

    // 클릭 즉시 드래그 준비 상태 확인
    public void OnPointerDown(PointerEventData eventData)
    {
        // 다른 드래그가 진행 중이면 무시
        if (isAnyDragging) return;

        // 이 셀이 비어있으면 무시
        if (!IsOccupied || PlacedBlock == null) return;

        // 드래그 가능 상태임을 표시 (시각적 피드백 등 추가 가능)
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 다른 드래그가 진행 중이면 무시
        if (isAnyDragging) return;

        if (!IsOccupied || PlacedBlock == null) return;

        // 전역 드래그 상태 설정
        isAnyDragging = true;
        isDraggingBlock = true;

        draggingBlockInfo = PlacedBlock;
        originalGrid = OwnerGrid;

        // 해당 블록의 모든 셀 제거
        OwnerGrid.RemovePlacedBlock(draggingBlockInfo);

        // 드래그 비주얼 생성
        CreateDragVisual();

        // 이 셀의 레이캐스트 차단 (다른 셀이 이벤트 가로채지 않도록)
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingBlock || draggingBlockInfo == null) return;

        UpdatePreviewAtPointer();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingBlock || draggingBlockInfo == null)
        {
            ResetDragState();
            return;
        }

        // 드래그 비주얼 제거
        DestroyDragVisual();

        // SpawnPanel 위에 드롭했는지 체크
        if (BlockSpawner.Instance != null
            && BlockSpawner.Instance.gameObject.activeInHierarchy
            && BlockSpawner.Instance.IsPointerOverSpawnArea(eventData.position))
        {
            BlockItem createdBlock = BlockSpawner.Instance.CreateBlockItemFromPlaced(draggingBlockInfo, eventData.position);

            if (createdBlock == null)
            {
                originalGrid.TryPlaceWithInfo(draggingBlockInfo.Origin, draggingBlockInfo);
            }

            GridManager.Instance.ClearAllPreviews();
            ResetDragState();
            return;
        }

        // 현재 마우스 위치에서 그리드 찾기
        Grid targetGrid = GridManager.Instance.GetGridAtScreenPos(eventData.position);
        bool placed = false;

        if (targetGrid != null)
        {
            Vector2Int gridPos = targetGrid.GetGridIndexFromWorldPos(eventData.position);
            placed = targetGrid.TryPlaceWithInfo(gridPos, draggingBlockInfo);
        }

        // 배치 실패 시 원래 그리드, 원래 위치로 복귀
        if (!placed)
        {
            originalGrid.TryPlaceWithInfo(draggingBlockInfo.Origin, draggingBlockInfo);
        }

        GridManager.Instance.ClearAllPreviews();
        ResetDragState();
    }

    private void ResetDragState()
    {
        isDraggingBlock = false;
        isAnyDragging = false;
        draggingBlockInfo = null;
        originalGrid = null;
        canvasGroup.blocksRaycasts = true;

        DestroyDragVisual();
    }

    #endregion

    #region 드래그 비주얼

    private void CreateDragVisual()
    {
        if (draggingBlockInfo == null) return;

        // Canvas 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        dragVisual = new GameObject("DragVisual");
        dragVisualRect = dragVisual.AddComponent<RectTransform>();
        dragVisual.transform.SetParent(canvas.transform, false);
        dragVisual.transform.SetAsLastSibling();

        CanvasGroup visualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        visualCanvasGroup.blocksRaycasts = false;
        visualCanvasGroup.alpha = 0.7f;

        // 셀 크기 (대략적인 값, 필요시 Grid에서 가져오기)
        float cellSize = 40f;
        float spacing = 4f;

        foreach (Vector2Int offset in draggingBlockInfo.Shape)
        {
            GameObject cellObj = new GameObject($"Cell_{offset.x}_{offset.y}");
            RectTransform cellRect = cellObj.AddComponent<RectTransform>();
            Image cellImg = cellObj.AddComponent<Image>();

            cellRect.SetParent(dragVisualRect, false);
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);
            cellRect.anchoredPosition = new Vector2(
                offset.x * (cellSize + spacing),
                offset.y * (cellSize + spacing)
            );

            cellImg.color = draggingBlockInfo.Color;
        }

        dragVisual.transform.position = Input.mousePosition;
    }

    private void UpdateDragVisual()
    {
        DestroyDragVisual();
        CreateDragVisual();
    }

    private void DestroyDragVisual()
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
            dragVisualRect = null;
        }
    }

    #endregion
}

/// <summary>
/// 그리드에 배치된 블록 정보
/// </summary>
[System.Serializable]
public class PlacedBlockInfo
{
    public Vector2Int Origin;
    public List<Vector2Int> Shape;
    public Color Color;
    public BlockData SourceData;
    public Grid OwnerGrid; // 현재 속한 그리드

    public PlacedBlockInfo(Vector2Int origin, List<Vector2Int> shape, Color color, BlockData source = null, Grid owner = null)
    {
        Origin = origin;
        Shape = new List<Vector2Int>(shape);
        Color = color;
        SourceData = source;
        OwnerGrid = owner;
    }
}