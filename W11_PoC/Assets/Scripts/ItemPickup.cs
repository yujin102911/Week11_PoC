using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public enum Type
    {
        Box,
        PrepareBox,
        FreeBlock,
        customer
    }

    [SerializeField] private string itemId;   // 나중에 인벤토리 연동할 때 쓸 ID
    [SerializeField]
    private Type type;

    public void Interact()
    {
        // TODO: 나중에 InventoryManager에 연결하기
        // InventoryManager.Instance.Add(itemId);

        //Debug.Log($"아이템 줍기: {itemId}");

        if (type.Equals(Type.customer))
        {
            //판매 패널
            UIManager.Instance.OpenSubmit(null, false);
        }
        else if (type.Equals(Type.PrepareBox))
        {
            //블럭 스폰 패널
            UIManager.Instance.OpenSpwan();
        }
        else if (type.Equals(Type.Box))
        {
            //박스 패널
            UIManager.Instance.OpenStorage(itemId);
        }
        else if (type.Equals(Type.FreeBlock))
        {
            UIManager.Instance.OpenFreeBlock(itemId);
        }

        // 지금은 그냥 씬에서 제거 = 줍힌 것처럼 보이게
        //Destroy(gameObject);
    }

    private void OnMouseEnter()
    {
        if (UIManager.Instance.Is_panel || GameManager.Instance.GetPhase().Equals(Phase.sell)) return;

        SetHighlight();
    }

    private void OnMouseOver()
    {
        if (UIManager.Instance.Is_panel || GameManager.Instance.GetPhase().Equals(Phase.sell)) return;

        if (Input.GetMouseButtonDown(0)) 
        {
            Debug.Log(itemId + " 클릭됨!!");
            Interact();
            RestoreAlpha();
        }
        
    }

    private void OnMouseExit()
    {
        if (UIManager.Instance.Is_panel || GameManager.Instance.GetPhase().Equals(Phase.sell)) return;

        RestoreAlpha();
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
