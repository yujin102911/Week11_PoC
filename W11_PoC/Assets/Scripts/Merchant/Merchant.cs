using UnityEngine;

public class Merchant : MonoBehaviour
{
    private bool _isArrived = false;
    private Vector3 _targetPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isArrived)
        {
            MoveToCounter();
        }
    }

    // 초기화
    public void Setup(Vector3 targetPosition)
    {
        _targetPos = targetPosition;
        //_payment = data.paymentAmount;
    }

    // 이동 로직
    private void MoveToCounter()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, 5f * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
        {
            _isArrived = true;
            Debug.Log($"상인 도착!");

            // 상인 패널 열기
            OnPanel();
        }
    }

    // 도착하고 상인 패널 열기
    private void OnPanel()
    {
        UIManager.Instance.OpenMerchant();
        UIManager.Instance.OpenInven();
    }
}
