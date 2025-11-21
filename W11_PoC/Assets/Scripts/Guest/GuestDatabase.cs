using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGuestDB", menuName = "Game/Guest Database")]
public class GuestDatabase : ScriptableObject
{
    public List<GuestData> guests;
}