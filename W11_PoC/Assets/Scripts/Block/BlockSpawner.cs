using UnityEngine;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance;

    public bool Is_spawn = false;

    [Header("프리팹")]
    [SerializeField] private BlockItem blockItemPrefab;

    [Header("블록 데이터베이스")]
    [SerializeField] 
    private BlockDatabase[] _blockDatabases;
    private BlockDatabase blockDatabase;

    [Header("스폰 영역 설정")]
    [SerializeField] private RectTransform spawnArea;        // 블록들이 생성될 영역
    [SerializeField] private int columnsCount = 3;           // 한 줄에 몇 개
    [SerializeField] private float horizontalSpacing = 120f; // 가로 간격
    [SerializeField] private float verticalSpacing = 120f;   // 세로 간격
    [SerializeField] private Vector2 startOffset = new Vector2(60f, -60f); // 시작 위치 오프셋

    [Header("반환 영역 설정")]
    [SerializeField] private RectTransform returnArea;       // 블록 반환 감지 영역 (없으면 spawnArea 사용)
    [SerializeField] private Camera uiCamera;                // UI 카메라 (Screen Space - Overlay면 null)

    // 내부 상태
    private Queue<BlockData> blockQueue = new Queue<BlockData>();
    private List<BlockItem> currentBlocks = new List<BlockItem>();
    private List<RectTransform> spawnSlots = new List<RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        //InitializeQueue();
        //GenerateSpawnSlots();
        //FillEmptySlots();
    }

    private void OnEnable()
    {
        //최초 1회 스폰
        if (!Is_spawn)
        {
            SetBlocks(GameManager.Instance.CurrentStage);
        }
    }

    public void SetBlocks(int stageIndex)
    {
        blockDatabase = _blockDatabases[stageIndex];

        InitializeQueue();
        GenerateSpawnSlots();
        FillEmptySlots();

        Is_spawn = true;
    }

    /// <summary>
    /// DB의 블록들을 섞어서 큐에 넣기
    /// </summary>
    public void InitializeQueue()
    {
        blockQueue.Clear();

        if (blockDatabase == null) return;

        List<BlockData> shuffled = blockDatabase.GetShuffledBlocks();
        foreach (var block in shuffled)
        {
            blockQueue.Enqueue(block);
        }

        Debug.Log($"블록 큐 초기화: {blockQueue.Count}개");
    }

    /// <summary>
    /// 스폰 슬롯 동적 생성
    /// </summary>
    private void GenerateSpawnSlots()
    {
        // 기존 슬롯 제거
        foreach (var slot in spawnSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        spawnSlots.Clear();

        if (blockDatabase == null) return;

        int totalSlots = blockDatabase.Count;
        int rows = Mathf.CeilToInt((float)totalSlots / columnsCount);

        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % columnsCount;
            int row = i / columnsCount;

            // 슬롯 위치 계산
            Vector2 slotPos = new Vector2(
                startOffset.x + col * horizontalSpacing,
                startOffset.y - row * verticalSpacing
            );

            // 빈 슬롯 오브젝트 생성
            GameObject slotObj = new GameObject($"SpawnSlot_{i}");
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.SetParent(spawnArea, false);
            slotRect.anchorMin = new Vector2(0, 1); // 좌상단 기준
            slotRect.anchorMax = new Vector2(0, 1);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = slotPos;

            spawnSlots.Add(slotRect);
        }

        Debug.Log($"스폰 슬롯 {spawnSlots.Count}개 생성됨");
    }

    /// <summary>
    /// 비어있는 슬롯에 블록 채우기
    /// </summary>
    public void FillEmptySlots()
    {
        for (int i = 0; i < spawnSlots.Count; i++)
        {
            // 해당 슬롯에 이미 블록이 있는지 확인
            if (IsSlotOccupied(i)) continue;

            // 큐에서 블록 꺼내기
            BlockData nextBlock = DequeueBlock();
            if (nextBlock == null) break; // 더 이상 블록 없음

            SpawnBlockAtSlot(nextBlock, i);
        }
    }

    /// <summary>
    /// 특정 슬롯에 블록이 있는지 확인
    /// </summary>
    private bool IsSlotOccupied(int slotIndex)
    {
        foreach (var block in currentBlocks)
        {
            if (block == null) continue;
            if (block.SlotIndex == slotIndex) return true;
        }
        return false;
    }

    /// <summary>
    /// 큐에서 블록 하나 꺼내기
    /// </summary>
    private BlockData DequeueBlock()
    {
        if (blockQueue.Count == 0) return null;
        return blockQueue.Dequeue();
    }

    /// <summary>
    /// 특정 슬롯에 블록 생성
    /// </summary>
    private BlockItem SpawnBlockAtSlot(BlockData blockData, int slotIndex)
    {
        if (blockItemPrefab == null || blockData == null) return null;
        if (slotIndex < 0 || slotIndex >= spawnSlots.Count) return null;

        BlockItem newBlock = Instantiate(blockItemPrefab, spawnArea);
        newBlock.transform.position = spawnSlots[slotIndex].position;
        Debug.Log(spawnSlots[slotIndex].position);
        newBlock.SetupVisuals(blockData);
        newBlock.SlotIndex = slotIndex;
        newBlock.name = $"Block_{blockData.blockName}";

        currentBlocks.Add(newBlock);
        return newBlock;
    }

    /// <summary>
    /// 블록이 사용됨 (배치 또는 삭제)
    /// </summary>
    public void OnBlockUsed(BlockItem block)
    {
        currentBlocks.Remove(block);

        // 모든 블록 소진 체크
        if (blockQueue.Count == 0 && currentBlocks.Count == 0)
        {
            Debug.Log("모든 블록 소진!");
            OnAllBlocksUsed();
        }
    }

    /// <summary>
    /// 모든 블록 소진 시 호출
    /// </summary>
    private void OnAllBlocksUsed()
    {
        // 필요시 다시 섞어서 시작하거나 게임 종료 등
        // InitializeQueue();
        // FillEmptySlots();

        UIManager.Instance.ActivePlayBtn();
    }

    /// <summary>
    /// 남은 블록 수
    /// </summary>
    public int RemainingBlockCount => blockQueue.Count + currentBlocks.Count;

    /// <summary>
    /// 포인터가 스폰 영역 위에 있는지 확인
    /// </summary>
    public bool IsPointerOverSpawnArea(Vector2 screenPos)
    {
        // 비활성화 상태면 false 반환
        if (!gameObject.activeInHierarchy) return false;

        RectTransform targetArea = returnArea != null ? returnArea : spawnArea;
        if (targetArea == null || !targetArea.gameObject.activeInHierarchy) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(targetArea, screenPos, uiCamera);
    }

    /// <summary>
    /// 배치된 블록 정보로 BlockItem 다시 생성
    /// </summary>
    public BlockItem CreateBlockItemFromPlaced(PlacedBlockInfo blockInfo, Vector2 position)
    {
        if (blockItemPrefab == null || blockInfo == null) return null;

        BlockItem newBlock = Instantiate(blockItemPrefab, spawnArea);
        newBlock.transform.position = position;

        // BlockData와 회전된 Shape 설정
        newBlock.SetupFromPlacedInfo(blockInfo);
        newBlock.SlotIndex = -1; // 슬롯에 속하지 않음
        newBlock.name = $"Block_Returned_{blockInfo.SourceData?.blockName ?? "Unknown"}";

        currentBlocks.Add(newBlock);

        if (RemainingBlockCount > 0) 
        {
            UIManager.Instance.UnActivePlayBtn();
        }

        //Debug.Log("돌아왔어요");
        return newBlock;
    }

    /// <summary>
    /// 큐 리셋 (다시 섞기)
    /// </summary>
    public void ResetQueue()
    {
        // 현재 스폰된 블록들 제거
        foreach (var block in currentBlocks)
        {
            if (block != null) Destroy(block.gameObject);
        }
        currentBlocks.Clear();

        InitializeQueue();
        FillEmptySlots();
    }
}