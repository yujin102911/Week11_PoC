using UnityEngine;
using System.Collections.Generic;

public class GuestManager : MonoBehaviour
{
    public static GuestManager Instance;

    [Header("데이터베이스")]
    [SerializeField] private GuestDatabase[] stageGuestDBs;

    [Header("설정")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private GameObject defaultGuestPrefab;

    private Queue<GuestData> _guestQueue = new Queue<GuestData>();
    private Guest _currentGuest;
    private int _currentStage = 0;

    private Grid _servingGrid; // 판매대

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 시작할 때 Serving 그리드를 못 찾을 수도 있으니 일단 시도
        FindServingGrid();
        LoadStage(_currentStage);
    }

    // 그리드를 찾는 함수 분리 (못 찾으면 다시 찾으려고)
    private void FindServingGrid()
    {
        if (_servingGrid == null)
        {
            _servingGrid = GridManager.Instance.GetGridByType(GridType.Serving);
            if (_servingGrid != null)
                Debug.Log($"[성공] 판매대(Serving Grid) 연결됨: {_servingGrid.name}");
        }
    }

    private void Update()
    {
        // 손님 스폰 로직
        if (_currentGuest == null && _guestQueue.Count > 0)
        {
            SpawnNextGuest();
        }

        // 실시간 갱신 (옵션)
        if (_currentGuest != null)
        {
            // 안전장치: 그리드가 없으면 다시 찾기 시도
            if (_servingGrid == null) FindServingGrid();
            else UpdateGuestOrderFeedback();
        }
    }

    public void LoadStage(int stageIndex)
    {
        _currentStage = stageIndex;
        _guestQueue.Clear();

        if (stageGuestDBs != null && stageIndex < stageGuestDBs.Length)
        {
            foreach (var guest in stageGuestDBs[stageIndex].guests)
            {
                _guestQueue.Enqueue(guest);
            }
        }
        else
        {
            Debug.LogError("오류: GuestDB가 연결되지 않았거나 스테이지 인덱스가 범위를 벗어남!");
        }
    }

    private void SpawnNextGuest()
    {
        if (_guestQueue.Count == 0) return;

        GuestData data = _guestQueue.Dequeue();
        GameObject prefab = data.guestPrefab != null ? data.guestPrefab : defaultGuestPrefab;

        if (prefab == null)
        {
            Debug.LogError("오류: 손님 프리팹이 없습니다!");
            return;
        }

        GameObject go = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        _currentGuest = go.GetComponent<Guest>();
        _currentGuest.Setup(data, counterPoint.position);

        Debug.Log($"손님 등장: {data.guestID}");
    }

    // ─────────────────────────────────────────────
    // [중요] 버튼이랑 연결된 함수
    // ─────────────────────────────────────────────
    public void OnClickSellButton()
    {
        Debug.Log("=== 판매 버튼 클릭됨 ===");

        // 1. 손님이 있는지 확인
        if (_currentGuest == null)
        {
            Debug.LogWarning("실패: 현재 손님이 없습니다.");
            return;
        }

        // 2. 판매대 그리드가 연결되었는지 확인
        if (_servingGrid == null)
        {
            FindServingGrid(); // 다시 찾아봄
            if (_servingGrid == null)
            {
                Debug.LogError("실패: 'Serving' 타입의 Grid를 찾을 수 없습니다. 씬에 배치된 Grid의 Inspector에서 Grid Type을 확인하세요.");
                return;
            }
        }

        // 3. 물건 검사
        List<BlockData> placedBlocks = GetBlocksOnServingGrid();
        List<BlockData> requiredBlocks = new List<BlockData>(_currentGuest.GetData().orderList);

        Debug.Log($"제출된 블록 수: {placedBlocks.Count} / 요구하는 블록 수: {requiredBlocks.Count}");

        bool isSuccess = CheckOrderMatch(placedBlocks, requiredBlocks);

        if (isSuccess)
        {
            Debug.Log("성공: 주문하신 물건이 맞습니다! (판매 완료)");

            // 정산 처리 (돈 오르는 로직 여기에 추가)

            _servingGrid.ClearAllCells(); // 그리드 비우기
            OnGuestLeave(_currentGuest, true);
        }
        else
        {
            Debug.LogWarning("실패: 주문 목록과 일치하지 않습니다.");
            // 디버깅을 위해 현재 올려둔 목록 출력
            string placedNames = "현재 올린 거: ";
            foreach (var b in placedBlocks) placedNames += b.blockName + ", ";
            Debug.Log(placedNames);
        }
    }

    private List<BlockData> GetBlocksOnServingGrid()
    {
        List<BlockData> results = new List<BlockData>();
        if (_servingGrid == null) return results;

        int w = _servingGrid.Width;
        int h = _servingGrid.Height;
        HashSet<PlacedBlockInfo> checkedBlocks = new HashSet<PlacedBlockInfo>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = _servingGrid.GetCell(x, y);
                if (cell != null && cell.IsOccupied && cell.PlacedBlock != null)
                {
                    if (!checkedBlocks.Contains(cell.PlacedBlock))
                    {
                        results.Add(cell.PlacedBlock.SourceData);
                        checkedBlocks.Add(cell.PlacedBlock);
                    }
                }
            }
        }
        return results;
    }

    private bool CheckOrderMatch(List<BlockData> placed, List<BlockData> required)
    {
        if (placed.Count != required.Count) return false;

        List<BlockData> tempPlaced = new List<BlockData>(placed);

        foreach (var req in required)
        {
            var match = tempPlaced.Find(p => p.blockName == req.blockName);
            if (match != null) tempPlaced.Remove(match);
            else return false;
        }
        return true;
    }

    public void OnGuestLeave(Guest guest, bool isSuccess)
    {
        if (guest != null) Destroy(guest.gameObject);
        _currentGuest = null;
    }

    private void UpdateGuestOrderFeedback()
    {
        if (_servingGrid == null) return;

        List<BlockData> placed = GetBlocksOnServingGrid();
        List<BlockData> required = new List<BlockData>(_currentGuest.GetData().orderList);

        foreach (var p in placed)
        {
            var match = required.Find(r => r.blockName == p.blockName);
            if (match != null) required.Remove(match);
        }
        _currentGuest.UpdateOrderUI(required);
    }
}