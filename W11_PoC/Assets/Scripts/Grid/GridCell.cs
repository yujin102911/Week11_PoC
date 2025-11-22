using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GridCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2Int Coordinate { get; private set; }
    public bool IsOccupied { get; private set; }
    public PlacedBlockInfo PlacedBlock { get; private set; }
    public Grid OwnerGrid { get; private set; } // 이 셀이 속한 그리드

    [SerializeField] private Image cellImage;

    private Color defaultColor = Color.white;
    private Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.5f);
    private Color invalidHighlightColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    // 드래그 관련
    private bool isDraggingBlock = false;
    private PlacedBlockInfo draggingBlockInfo;
    private Grid originalGrid; // 드래그 시작한 그리드

    private void Update()
    {
        if (isDraggingBlock && draggingBlockInfo != null)
        {
            HandleRotationInput();
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

        // 1. 회전 변환 (BlockItem과 동일한 로직)
        foreach (Vector2Int pos in currentShape)
        {
            Vector2Int newPos = clockwise
                ? new Vector2Int(pos.y, -pos.x)
                : new Vector2Int(-pos.y, pos.x);
            rotatedShape.Add(newPos);
        }

        // 2. 정규화 (좌표를 (0,0) 기준으로 정렬)
        NormalizeShape(rotatedShape);

        // 3. 데이터 업데이트
        draggingBlockInfo.Shape = rotatedShape;

        // 4. 프리뷰 즉시 갱신 (현재 마우스 위치 기준)
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

    // 프리뷰 갱신 로직을 분리 (OnDrag와 Rotate에서 공통 사용)
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsOccupied || PlacedBlock == null) return;

        draggingBlockInfo = PlacedBlock;
        originalGrid = OwnerGrid;
        isDraggingBlock = true;

        // 해당 블록의 모든 셀 제거
        OwnerGrid.RemovePlacedBlock(draggingBlockInfo);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingBlock || draggingBlockInfo == null) return;

        UpdatePreviewAtPointer();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingBlock || draggingBlockInfo == null) return;

        // SpawnPanel 위에 드롭했는지 체크 (활성화 상태일 때만)
        if (BlockSpawner.Instance != null
            && BlockSpawner.Instance.gameObject.activeInHierarchy  // 추가된 체크
            && BlockSpawner.Instance.IsPointerOverSpawnArea(eventData.position))
        {
            BlockItem createdBlock = BlockSpawner.Instance.CreateBlockItemFromPlaced(draggingBlockInfo, eventData.position);

            // 블록 생성 실패 시 원래 위치로 복귀
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
        draggingBlockInfo = null;
        originalGrid = null;
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