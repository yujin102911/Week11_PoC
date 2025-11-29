using System.Collections.Generic;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "NewGuest", menuName = "Game/Guest Data")]
public class GuestData : ScriptableObject
{
    [Header("손님 기본 정보")]
    public string guestID;      // 왔는지 체크용 ID
    public GameObject guestPrefab; // 손님 외형 프리팹 (없으면 기본값 사용)
    
    [Header("조건")]
    public float patienceTime = 30f; // 기다릴 수 있는 시간
    public int paymentAmount = 100;  // 지불 금액

    [Space(10)]
    [Tab("주문 정보(구)")]
    [Header("주문 정보")]
    public List<BlockData> orderList; // 원하는 블록 리스트
    [Tab("주문 정보(신)")]
    [Header("주문 정보")]
    public GuestPattern GuestPattern;
    [Header("긍정 요구")]
    public P_Request P_Request = P_Request.None;
    [Header("부정 요구")]
    public N_Request N_Request = N_Request.None;
    [Header("요구 텍스트")]
    [TextArea(3, 10)]
    public string RequestMessage;
}

public enum Request_Type
{
    None,
    R,
    G,
    B
}

public enum P_Request
{
    None,
    R,
    G,
    B
}

public enum N_Request
{
    None,
    R,
    G,
    B
}
