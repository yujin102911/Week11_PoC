using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;


public class BlockItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("데이터")]
    public BlockData blockData;
    public int SlotIndex { get; set; } = -1;

    [Header("비주얼 설정")]
    [SerializeField] private GameObject cellVisualPrefab;
    [SerializeField] private float visualCellSize = 40f;
    [SerializeField] private float visualSpacing = 4f;

    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;

    // 회전용
    private List<Vector2Int> currentShape = new List<Vector2Int>();
    private int currentRotation = 0;

    // 드래그 상태
    private bool isDragging = false;
    private Grid currentHoverGrid = null; // 현재 마우스가 위치한 그리드

    private FreeBlockPanel _ownerPanel;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        raycaster = GetComponentInParent<GraphicRaycaster>();
    }

    private void Update()
    {
        if (isDragging)
        {
            FollowMouse();
            HandleRotationInput();

            if (Input.GetMouseButtonDown(0)) 
            {
                isDragging = false;
                DropBlock();
            }

            if (Input.GetMouseButtonDown(1))
            {
                RotateShape(true);
            }
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

        //NormalizeShape(rotatedShape);
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

    private void FollowMouse()
    {
        transform.position = Input.mousePosition;

        if (GridManager.Instance == null) return;

        // 현재 위치에서 그리드 찾기
        currentHoverGrid = GridManager.Instance.GetGridAtScreenPos(Input.mousePosition);

        if (currentHoverGrid != null)
        {
            Vector2Int gridPos = currentHoverGrid.GetGridIndexFromWorldPos(Input.mousePosition);
            GridManager.Instance.ShowPreview(currentHoverGrid, gridPos, currentShape, blockData.blockColor);
        }
        else
        {
            GridManager.Instance.ClearAllPreviews();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log("왼쪽 클릭");

            if (!isDragging)
            {
                isDragging = true;
                PickUpBlock();
            }
                
        }
            
    }

    // 블럭 들기
    private void PickUpBlock()
    {
        startPosition = transform.position;
        isDragging = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        transform.SetParent(canvas.transform, false);


        transform.SetAsLastSibling();
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        if (_ownerPanel != null)
            _ownerPanel.OnBlockUsed(this);
    }

    // 블럭 놓기
    private void DropBlock()
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
        Grid targetGrid = GridManager.Instance.GetGridAtScreenPos(Input.mousePosition);
        bool placed = false;

        if (targetGrid != null)
        {

            Vector2Int gridPos = targetGrid.GetGridIndexFromWorldPos(Input.mousePosition);
            placed = targetGrid.TryPlaceBlockWithShape(gridPos, currentShape, blockData);
        }

        // 놓았으면 사용되었음을 알려주고 자신 파괴
        if (placed)
        {
            Debug.Log("안함");

            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.OnBlockUsed(this);

            if (_ownerPanel != null)
                //_ownerPanel.OnBlockUsed(this);

            Debug.Log("Destroy 직전, name = " + gameObject.name);
            Debug.Log("parent = " + transform.parent);
            Debug.Log("activeSelf = " + gameObject.activeSelf);
            Debug.Log("InstanceID = " + gameObject.GetInstanceID());

            Destroy(gameObject);

            Debug.Log("왜안죽지?");
        }
        // 아니면 출발한 위치로 복귀
        else
        {
            // 1) PointerEventData에 마우스 위치만 넣어서 사용
            PointerEventData ped = new PointerEventData(eventSystem);
            ped.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(ped, results);

            foreach (var r in results)
            {
                // 2) 드롭된 위치에서 원하는 컴포넌트가 붙은 대상 찾기
                var target = r.gameObject.GetComponentInParent<FreeBlockPanel>();
                if (target != null
                    && target.gameObject.activeInHierarchy
                    && target.IsPointerOverSpawnArea(Input.mousePosition)
                    && target.CheckSize())
                {
                    Debug.Log("찾았다! → " + target.name);
                    // target 변수로 참조 작업


                    target.CreateBlockItemReturned(blockData, currentShape, Input.mousePosition);
                    GridManager.Instance.ClearAllPreviews();
                    currentHoverGrid = null;
                    Destroy(gameObject);
                    return;
                }
            }
            
            transform.position = startPosition;

            if (_ownerPanel != null)
                _ownerPanel.RecoverBlock(this);
        }

        GridManager.Instance.ClearAllPreviews();
        currentHoverGrid = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        PickUpBlock();
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

        // 놓았으면 사용되었음을 알려주고 자신 파괴
        if (placed)
        {
            if (BlockSpawner.Instance != null)
                BlockSpawner.Instance.OnBlockUsed(this);

            if (_ownerPanel != null)
                _ownerPanel.OnBlockUsed(this);
            
            Destroy(gameObject);
        }
        // 아니면 출발한 위치로 복귀
        else
        {
            // 1) 드롭된 위치에서 UI 레이캐스트
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                // 2) 드롭된 위치에서 원하는 컴포넌트가 붙은 대상 찾기
                var target = r.gameObject.GetComponentInParent<FreeBlockPanel>();
                if (target != null
                    && target.gameObject.activeInHierarchy
                    && target.IsPointerOverSpawnArea(eventData.position)
                    && target.CheckSize())
                {
                    Debug.Log("찾았다! → " + target.name);
                    // target 변수로 참조 작업
                    

                    target.CreateBlockItemReturned(blockData, currentShape, eventData.position);
                    GridManager.Instance.ClearAllPreviews();
                    currentHoverGrid = null;
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position = startPosition;

            if (_ownerPanel != null)
                _ownerPanel.RecoverBlock(this);
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
        //float startX = -totalWidth / 2f + visualCellSize / 2f;
        //float startY = -totalHeight / 2f + visualCellSize / 2f;

        float startX = 0;
        float startY = 0;

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

    //음수 범위까지 계산하도록
    private Vector2Int GetCurrentBounds()
    {
        if (currentShape == null || currentShape.Count == 0)
            return Vector2Int.one;

        int maxX = 0, maxY = 0;
        int minX = 0, minY = 0;
        foreach (var pos in currentShape)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;

            if(pos.x < minX) minX = pos.x;
            if(pos.y < minY) minY = pos.y;
        }

        int Xarea = maxX + 1 + Mathf.Abs(minX);
        int Yarea = maxY + 1 + Mathf.Abs(minY);

        return new Vector2Int(Xarea, Yarea);
    }

    public List<Vector2Int> GetCurrentShape() => currentShape;

    // 스폰 패널에 다시 놓아질때 블록 정보를 업데이트
    public void SetupFromPlacedInfo(PlacedBlockInfo blockInfo)
    {
        if (blockInfo == null) return;

        blockData = blockInfo.SourceData;
        currentShape = new List<Vector2Int>(blockInfo.Shape);
        currentRotation = 0;

        UpdateVisuals();
    }

    public void SetupFromPlacedInfo(BlockData _blockData, List<Vector2Int> _currentShape)
    {
        if (_blockData == null) return;

        blockData = _blockData;
        currentShape = _currentShape;
        currentRotation = 0;

        UpdateVisuals();
    }

    //원래 패널 세팅
    public void OwnerPanelSet(FreeBlockPanel _panel)
    {
        _ownerPanel = _panel;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 1) 드롭된 위치에서 UI 레이캐스트
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            // 2) 드롭된 위치에서 원하는 컴포넌트가 붙은 대상 찾기
            var target = r.gameObject.GetComponentInParent<FreeBlockPanel>();
            if (target != null)
            {
                Debug.Log("찾았다! → " + target.name);
                // target 변수로 참조 작업
                return;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        canvasGroup.alpha = 1.0f;
    }
}