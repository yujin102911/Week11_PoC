using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemId;   // 나중에 인벤토리 연동할 때 쓸 ID

    public void Interact()
    {
        // TODO: 나중에 InventoryManager에 연결하기
        // InventoryManager.Instance.Add(itemId);

        Debug.Log($"아이템 줍기: {itemId}");
        UIManager.Instance.OpenStorage();

        // 지금은 그냥 씬에서 제거 = 줍힌 것처럼 보이게
        //Destroy(gameObject);
    }
    public void SetHighlight()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        Color c = renderer.color;
        c.a = 0.75f;
        renderer.color = c;
    }

    public void RestoreAlpha()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        Color c = renderer.color;
        c.a = 1f;
        renderer.color = c;
    }

}
