using UnityEngine;
using TMPro; // TextMeshPro 필수
using System.Collections.Generic;

public class Guest : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI timerText; // 머리 위 타이머 텍스트
    [SerializeField] private TextMeshProUGUI orderText; // 머리 위 주문 목록 (World Space UI)

    private GuestData _data;
    private float _currentPatience;
    private bool _isArrived = false;
    private Vector3 _targetPos;

    // 초기화
    public void Setup(GuestData data, Vector3 targetPosition)
    {
        _data = data;
        _targetPos = targetPosition;
        _currentPatience = data.patienceTime;

        UpdateOrderUI(data.orderList); // 초기 주문 표시
    }

    private void Update()
    {
        if (!_isArrived)
        {
            MoveToCounter();
        }
        else
        {
            HandlePatience();
        }
    }

    // 이동 로직
    private void MoveToCounter()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, 5f * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
        {
            _isArrived = true;
            Debug.Log($"손님 {_data.guestID} 도착!");
        }
    }

    // 대기 시간 로직
    private void HandlePatience()
    {
        _currentPatience -= Time.deltaTime;

        if (timerText != null)
            timerText.text = Mathf.Ceil(_currentPatience).ToString();

        if (_currentPatience <= 0)
        {
            GuestManager.Instance.OnGuestLeave(this, false); // 시간 초과로 떠남
        }
    }

    // 주문 UI 갱신 (ㄴ자: 2개, .자: 1개 형식으로 텍스트 조합)
    public void UpdateOrderUI(List<BlockData> currentOrders)
    {
        if (orderText == null) return;

        // 블록 이름별 개수 카운트
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var block in currentOrders)
        {
            if (counts.ContainsKey(block.blockName)) counts[block.blockName]++;
            else counts[block.blockName] = 1;
        }

        string displayText = "";
        foreach (var pair in counts)
        {
            displayText += $"{pair.Key} x{pair.Value}\n";
        }
        orderText.text = displayText;
    }

    public GuestData GetData() => _data;
}