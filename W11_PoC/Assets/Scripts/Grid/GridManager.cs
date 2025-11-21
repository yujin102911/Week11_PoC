using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("UI 카메라 (Screen Space - Overlay면 비워두기)")]
    [SerializeField] private Camera uiCamera;

    // 등록된 그리드들
    private List<Grid> registeredGrids = new List<Grid>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Grid Registration

    public void RegisterGrid(Grid grid)
    {
        if (grid != null && !registeredGrids.Contains(grid))
        {
            registeredGrids.Add(grid);
            Debug.Log($"Grid 등록: {grid.GridId}");
        }
    }

    public void UnregisterGrid(Grid grid)
    {
        if (registeredGrids.Contains(grid))
        {
            registeredGrids.Remove(grid);
            Debug.Log($"Grid 해제: {grid.GridId}");
        }
    }

    public Grid GetGridById(string gridId)
    {
        return registeredGrids.Find(g => g.GridId == gridId);
    }

    public Grid GetGridByType(GridType type)
    {
        return registeredGrids.Find(g => g.Type == type);
    }

    public List<Grid> GetAllGrids() => registeredGrids;

    #endregion

    #region Grid Detection

    /// <summary>
    /// 스크린 좌표에서 어느 그리드 위에 있는지 찾기
    /// </summary>
    public Grid GetGridAtScreenPos(Vector2 screenPos)
    {
        foreach (var grid in registeredGrids)
        {
            if (grid.ContainsScreenPoint(screenPos, uiCamera) && grid.gameObject.activeSelf)
            {
                return grid;
            }
        }
        return null;
    }

    /// <summary>
    /// 스크린 좌표에서 그리드와 그리드 인덱스 함께 반환
    /// </summary>
    public (Grid grid, Vector2Int index) GetGridAndIndexAtScreenPos(Vector2 screenPos)
    {
        Grid grid = GetGridAtScreenPos(screenPos);
        if (grid == null) return (null, new Vector2Int(-1, -1));

        Vector2Int index = grid.GetGridIndexFromWorldPos(screenPos);
        return (grid, index);
    }

    #endregion

    #region Preview (모든 그리드)

    /// <summary>
    /// 특정 그리드에 프리뷰 표시
    /// </summary>
    public void ShowPreview(Grid targetGrid, Vector2Int startCoords, List<Vector2Int> shape, Color color)
    {
        ClearAllPreviews();
        targetGrid?.ShowPreviewWithShape(startCoords, shape, color);
    }

    /// <summary>
    /// 모든 그리드의 프리뷰 제거
    /// </summary>
    public void ClearAllPreviews()
    {
        foreach (var grid in registeredGrids)
        {
            grid.ClearPreview();
        }
    }

    #endregion

    #region Block Placement

    /// <summary>
    /// 특정 그리드에 블록 배치 시도
    /// </summary>
    public bool TryPlaceBlock(Grid targetGrid, Vector2Int startCoords, List<Vector2Int> shape, BlockData blockData)
    {
        if (targetGrid == null) return false;
        return targetGrid.TryPlaceBlockWithShape(startCoords, shape, blockData);
    }

    /// <summary>
    /// PlacedBlockInfo로 배치 시도
    /// </summary>
    public bool TryPlaceWithInfo(Grid targetGrid, Vector2Int startCoords, PlacedBlockInfo blockInfo)
    {
        if (targetGrid == null) return false;
        return targetGrid.TryPlaceWithInfo(startCoords, blockInfo);
    }

    /// <summary>
    /// 블록 제거
    /// </summary>
    public void RemovePlacedBlock(PlacedBlockInfo blockInfo)
    {
        blockInfo?.OwnerGrid?.RemovePlacedBlock(blockInfo);
    }

    #endregion

    #region Utility

    /// <summary>
    /// 모든 그리드 초기화
    /// </summary>
    public void ClearAllGrids()
    {
        foreach (var grid in registeredGrids)
        {
            grid.ClearAllCells();
        }
    }

    #endregion
}