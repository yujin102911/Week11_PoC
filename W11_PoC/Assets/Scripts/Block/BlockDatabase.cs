using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBlockDatabase", menuName = "Game/Block Database")]
public class BlockDatabase : ScriptableObject
{
    [Header("블록 목록")]
    public List<BlockData> blocks = new List<BlockData>();

    /// <summary>
    /// 랜덤하게 섞인 블록 리스트 반환 (Fisher-Yates 셔플)
    /// </summary>
    public List<BlockData> GetShuffledBlocks()
    {
        List<BlockData> shuffled = new List<BlockData>(blocks);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (shuffled[i], shuffled[randIndex]) = (shuffled[randIndex], shuffled[i]);
        }

        return shuffled;
    }

    public int Count => blocks.Count;
}