using System.Collections.Generic;
using TMPro; // TextMeshPro 필수
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class Guest : MonoBehaviour
{
    [Tab("타이머")]
    [Header("타이머")]
    [SerializeField]
    private GameObject _timer;
    [SerializeField]
    private Image _timeGauge;
    [SerializeField] private TextMeshProUGUI timerText; // 머리 위 타이머 텍스트

    [Tab("주문")]
    [Header("주문")]
    [SerializeField]
    private GameObject[] _orders;
    [SerializeField]
    private Image[] _orederImages;
    [SerializeField] private TextMeshProUGUI[] orderTexts; // 머리 위 주문 목록 (World Space UI)

    private GuestData _data;
    private float _maxPatience;
    private float _currentPatience;
    private bool _isArrived = false;
    private Vector3 _targetPos;

    private int _payment;

    // 초기화
    public void Setup(GuestData data, Vector3 targetPosition)
    {
        _data = data;
        _targetPos = targetPosition;
        _maxPatience = data.patienceTime;
        _currentPatience = _maxPatience;
        _payment = data.paymentAmount;

        //타이머 끄기
        _timer.SetActive(false);

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
            
            //타이머 켜기
            _timer.SetActive(true);
        }
    }

    // 대기 시간 로직
    private void HandlePatience()
    {
        _currentPatience -= Time.deltaTime;

        if (_timeGauge != null)
            _timeGauge.fillAmount = _currentPatience / _maxPatience;

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
        if (orderTexts == null || _orederImages == null) return;

        // 블록 이름별 개수 카운트
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var block in currentOrders)
        {
            if (counts.ContainsKey(block.blockName)) counts[block.blockName]++;
            else counts[block.blockName] = 1;
        }

        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        foreach (var block in currentOrders)
        {
            if (sprites.ContainsKey(block.blockName)) continue;
            else sprites[block.blockName] = block.BlockSprite;
        }

        OrederClear();

        // 주문 목록 세팅
        int index = 0;
        foreach (var pair in counts)
        {
            _orederImages[index].sprite = sprites[pair.Key];
            orderTexts[index].text = $" x{pair.Value}";
            _orders[index].SetActive(true);
            index++;
        }

        //string displayText = "";
        //foreach (var pair in counts)
        //{
        //    displayText += $"{pair.Key} x{pair.Value}\n";
        //}
        //orderText.text = displayText;
    }

    //점수 계산
    public int CaculatePoint()
    {
        int point = 0;
        float f_point = _payment * (_currentPatience / _maxPatience);

        point = (int)Mathf.Floor(f_point);

        return point;
    }

    // 주문목록 끄기
    private void OrederClear()
    {
        for (int i = 0; i < _orders.Length; i++)
        {
            _orders[i].SetActive(false);
        }
    }

    public GuestData GetData() => _data;
}