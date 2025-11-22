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
    private static bool isAnyDragging = false;
    private bool isDraggingBlock = false;
    private PlacedBlockInfo draggingBlockInfo;
    private Grid originalGrid;
    private Vector2Int originalOrigin;           // 원래 Origin 저장
    private List<Vector2Int> originalShape;      // 원래 Shape 저장
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

        // 정규화 (0,0 기준으로) - 내부 저장용
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
            // 중심 기준 shape 사용
            List<Vector2Int> centeredShape = GetCenteredShape(draggingBlockInfo.Shape);
            GridManager.Instance.ShowPreview(targetGrid, gridPos, centeredShape, draggingBlockInfo.Color);
        }
        else
        {
            GridManager.Instance.ClearAllPreviews();
        }
    }

    // Shape를 중심 기준으로 변환
    private List<Vector2Int> GetCenteredShape(List<Vector2Int> shape)
    {
        if (shape == null || shape.Count == 0)
            return shape;

        // 바운드 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var pos in shape)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        // 중심 오프셋 계산
        int centerX = (minX + maxX) / 2;
        int centerY = (minY + maxY) / 2;

        // 중심 기준으로 변환
        List<Vector2Int> centered = new List<Vector2Int>();
        foreach (var pos in shape)
        {
            centered.Add(new Vector2Int(pos.x - centerX, pos.y - centerY));
        }

        return centered;
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
        if (isAnyDragging) return;
        if (!IsOccupied || PlacedBlock == null) return;

        isAnyDragging = true;
        isDraggingBlock = true;

        draggingBlockInfo = PlacedBlock;
        originalGrid = OwnerGrid;

        // 원래 상태 저장 (복귀용)
        originalOrigin = draggingBlockInfo.Origin;
        originalShape = new List<Vector2Int>(draggingBlockInfo.Shape);

        OwnerGrid.RemovePlacedBlock(draggingBlockInfo);
        CreateDragVisual();
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

        DestroyDragVisual();

        // SpawnPanel 위에 드롭했는지 체크
        if (BlockSpawner.Instance != null
            && BlockSpawner.Instance.gameObject.activeInHierarchy
            && BlockSpawner.Instance.IsPointerOverSpawnArea(eventData.position))
        {
            BlockItem createdBlock = BlockSpawner.Instance.CreateBlockItemFromPlaced(draggingBlockInfo, eventData.position);

            if (createdBlock == null)
            {
                ReturnBlockToOriginalPosition();
            }

            GridManager.Instance.ClearAllPreviews();
            ResetDragState();
            return;
        }

        // 그리드 찾기
        Grid targetGrid = GridManager.Instance.GetGridAtScreenPos(eventData.position);
        bool placed = false;

        if (targetGrid != null)
        {
            Vector2Int gridPos = targetGrid.GetGridIndexFromWorldPos(eventData.position);
            // 중심 기준 shape로 배치 시도
            List<Vector2Int> centeredShape = GetCenteredShape(draggingBlockInfo.Shape);

            // 원본 shape 백업
            var originalShape = new List<Vector2Int>(draggingBlockInfo.Shape);
            draggingBlockInfo.Shape = centeredShape;

            placed = targetGrid.TryPlaceWithInfo(gridPos, draggingBlockInfo);

            // 배치 실패시 원래 shape 복원
            if (!placed)
            {
                draggingBlockInfo.Shape = originalShape;
            }
        }

        // 배치 실패 시 원래 그리드, 원래 위치로 복귀
        if (!placed)
        {
            ReturnBlockToOriginalPosition();
        }

        GridManager.Instance.ClearAllPreviews();
        ResetDragState();
    }

    // 블록을 원래 위치로 복귀
    private void ReturnBlockToOriginalPosition()
    {
        if (draggingBlockInfo == null || originalGrid == null) return;

        // 저장해둔 원래 shape와 origin으로 복귀
        draggingBlockInfo.Shape = new List<Vector2Int>(originalShape);
        draggingBlockInfo.Origin = originalOrigin;

        originalGrid.TryPlaceWithInfo(originalOrigin, draggingBlockInfo);
    }

    private void ResetDragState()
    {
        isDraggingBlock = false;
        isAnyDragging = false;
        draggingBlockInfo = null;
        originalGrid = null;
        originalOrigin = Vector2Int.zero;
        originalShape = null;
        canvasGroup.blocksRaycasts = true;

        DestroyDragVisual();
    }

    #endregion

    #region 드래그 비주얼

    private void CreateDragVisual()
    {
        if (draggingBlockInfo == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        dragVisual = new GameObject("DragVisual");
        dragVisualRect = dragVisual.AddComponent<RectTransform>();
        dragVisual.transform.SetParent(canvas.transform, false);
        dragVisual.transform.SetAsLastSibling();

        CanvasGroup visualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        visualCanvasGroup.blocksRaycasts = false;
        visualCanvasGroup.alpha = 0.7f;

        float cellSize = 40f;
        float spacing = 4f;

        // 블록의 바운드 계산
        int maxX = 0, maxY = 0;
        foreach (var pos in draggingBlockInfo.Shape)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }

        // 전체 크기 계산
        float totalWidth = (maxX + 1) * cellSize + maxX * spacing;
        float totalHeight = (maxY + 1) * cellSize + maxY * spacing;

        // 중심 오프셋 (블록 중앙이 마우스 위치에 오도록)
        float centerOffsetX = -totalWidth / 2f + cellSize / 2f;
        float centerOffsetY = -totalHeight / 2f + cellSize / 2f;

        foreach (Vector2Int offset in draggingBlockInfo.Shape)
        {
            GameObject cellObj = new GameObject($"Cell_{offset.x}_{offset.y}");
            RectTransform cellRect = cellObj.AddComponent<RectTransform>();
            Image cellImg = cellObj.AddComponent<Image>();

            cellRect.SetParent(dragVisualRect, false);
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);
            cellRect.anchoredPosition = new Vector2(
                centerOffsetX + offset.x * (cellSize + spacing),
                centerOffsetY + offset.y * (cellSize + spacing)
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