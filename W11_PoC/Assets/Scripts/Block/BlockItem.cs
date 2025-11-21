using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class BlockItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [Header("데이터")]
    public BlockData blockData;
    public int SlotIndex { get; set; } = -1;

    [Header("비주얼 설정")]
    [SerializeField] private GameObject cellVisualPrefab;
    [SerializeField] private float visualCellSize = 40f;
    [SerializeField] private float visualSpacing = 4f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;

    // 회전용
    private List<Vector2Int> currentShape = new List<Vector2Int>();
    private int currentRotation = 0;

    // 드래그 상태
    private bool isDragging = false;
    private Grid currentHoverGrid = null; // 현재 마우스가 위치한 그리드

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (isDragging)
        {
            HandleRotationInput();
        }
    }

    private void HandleRotationInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
            RotateShape(true);
        else if (scroll < 0f)
            RotateShape(false);
    }

    public void RotateShape(bool clockwise)
    {
        if (currentShape == null || currentShape.Count == 0) return;

        List<Vector2Int> rotatedShape = new List<Vector2Int>();

        foreach (Vector2Int pos in currentShape)
        {
            Vector2Int newPos = clockwise
                ? new Vector2Int(pos.y, -pos.x)
                : new Vector2Int(-pos.y, pos.x);
            rotatedShape.Add(newPos);
        }

        NormalizeShape(rotatedShape);
        currentShape = rotatedShape;

        currentRotation = clockwise
            ? (currentRotation + 90) % 360
            : (currentRotation - 90 + 360) % 360;

        UpdateVisuals();
        UpdatePreviewAtCurrentPos();
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

    private void UpdatePreviewAtCurrentPos()
    {
        if (currentHoverGrid != null)
        {
            Vector2Int gridPos = currentHoverGrid.GetGridIndexFromWorldPos(transform.position);
            GridManager.Instance.ShowPreview(currentHoverGrid, gridPos, currentShape, blockData.blockColor);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = transform.position;
        isDragging = true;

        transform.SetAsLastSibling();
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;

        if (GridManager.Instance == null) return;

        // 현재 위치에서 그리드 찾기
        currentHoverGrid = GridManager.Instance.GetGridAtScreenPos(eventData.position);

        if (currentHoverGrid != null)
        {
            Vector2Int gridPos = currentHoverGrid.GetGridIndexFromWorldPos(eventData.position);
            GridManager.Instance.ShowPreview(currentHoverGrid, gridPos, currentShape, blockData.blockColor);
        }
        else
        {
            GridManager.Instance.ClearAllPreviews();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        isDragging = false;

        if (GridManager.Instance == null)
        {
            transform.position = startPosition;
            return;
        }

        // 현재 위치에서 그리드 찾기
        Grid targetGrid = GridManager.Instance.GetGridAtScreenPos(eventData.position);
        bool placed = false;

        if (targetGrid != null)
        {
            
            Vector2Int gridPos = targetGrid.GetGridIndexFromWorldPos(eventData.position);
            placed = targetGrid.TryPlaceBlockWithShape(gridPos, currentShape, blockData);
        }

        if (placed)
        {
            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.OnBlockUsed(this);

            Destroy(gameObject);
        }
        else
        {
            transform.position = startPosition;
        }

        GridManager.Instance.ClearAllPreviews();
        currentHoverGrid = null;
    }

    public void SetupVisuals(BlockData data)
    {
        blockData = data;
        if (blockData == null) return;

        currentShape = new List<Vector2Int>(blockData.shape);
        currentRotation = 0;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (cellVisualPrefab == null) return;

        // 1. 기존 자식 제거
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 2. 전체 사이즈 계산
        Vector2Int bounds = GetCurrentBounds(); // (가로 개수, 세로 개수)

        float totalWidth = bounds.x * visualCellSize + (bounds.x - 1) * visualSpacing;
        float totalHeight = bounds.y * visualCellSize + (bounds.y - 1) * visualSpacing;

        // 3. BlockItem(부모)의 크기를 내용물에 딱 맞게 조정
        rectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);

        // 4. 시작 좌표 계산 (정중앙 (0,0) 기준, 왼쪽 아래 셀의 중심 좌표)
        // 식: -(전체길이 / 2) + (셀크기 / 2)
        float startX = -totalWidth / 2f + visualCellSize / 2f;
        float startY = -totalHeight / 2f + visualCellSize / 2f;

        // 5. 셀 배치
        foreach (Vector2Int offset in currentShape)
        {
            GameObject cellVisual = Instantiate(cellVisualPrefab, transform);

            RectTransform cellRect = cellVisual.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                // 앵커를 중앙으로 고정 (중요!)
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f); // 프리팹 설정이 달라도 강제로 중앙으로 맞춤

                float posX = startX + offset.x * (visualCellSize + visualSpacing);
                float posY = startY + offset.y * (visualCellSize + visualSpacing);

                cellRect.anchoredPosition = new Vector2(posX, posY);
                cellRect.sizeDelta = new Vector2(visualCellSize, visualCellSize);
            }

            Image cellImage = cellVisual.GetComponent<Image>();
            if (cellImage != null)
            {
                cellImage.color = blockData.blockColor;
            }
        }

        // AdjustRectSize()는 위에서 이미 sizeDelta를 맞췄으므로 
        // 따로 호출할 필요가 없거나, 호출해도 같은 값을 계산하게 됩니다.
    }

    private void AdjustRectSize()
    {
        Vector2Int bounds = GetCurrentBounds();
        float totalWidth = bounds.x * (visualCellSize + visualSpacing) - visualSpacing;
        float totalHeight = bounds.y * (visualCellSize + visualSpacing) - visualSpacing;

        rectTransform.sizeDelta = new Vector2(
            Mathf.Max(totalWidth, visualCellSize),
            Mathf.Max(totalHeight, visualCellSize)
        );
    }

    private Vector2Int GetCurrentBounds()
    {
        if (currentShape == null || currentShape.Count == 0)
            return Vector2Int.one;

        int maxX = 0, maxY = 0;
        foreach (var pos in currentShape)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public List<Vector2Int> GetCurrentShape() => currentShape;

    public void SetupFromPlacedInfo(PlacedBlockInfo blockInfo)
    {
        if (blockInfo == null) return;

        blockData = blockInfo.SourceData;
        currentShape = new List<Vector2Int>(blockInfo.Shape);
        currentRotation = 0;

        UpdateVisuals();
    }
}