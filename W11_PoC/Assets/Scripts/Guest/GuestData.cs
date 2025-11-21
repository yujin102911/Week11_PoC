using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGuest", menuName = "Game/Guest Data")]
public class GuestData : ScriptableObject
{
    [Header("손님 기본 정보")]
    public string guestID;      // 왔는지 체크용 ID
    public GameObject guestPrefab; // 손님 외형 프리팹 (없으면 기본값 사용)

    [Header("주문 정보")]
    public List<BlockData> orderList; // 원하는 블록 리스트

    [Header("조건")]
    public float patienceTime = 30f; // 기다릴 수 있는 시간
    public int paymentAmount = 100;  // 지불 금액
}

