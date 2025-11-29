using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inven_Panel : MonoBehaviour
{
    [Header("균일가")]
    public int Cost = 1000;

    [SerializeField]
    private Grid _inven;

    [SerializeField]
    private TextMeshProUGUI _description;
    [SerializeField]
    private Button _buyBtn;
    [SerializeField]
    private Button _toSellBtn;

    private bool is_buy = false;

    private void OnEnable()
    {
        _description.text = $"담을수록 이득! <color=red>균일가</color> G " + string.Format("{0:#,###}", Cost);
        is_buy = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _buyBtn.onClick.RemoveAllListeners();
        _toSellBtn.onClick.RemoveAllListeners();

        _buyBtn.onClick.AddListener(BuyBlock);
        _toSellBtn.onClick.AddListener(ToSell);
    }

    // Update is called once per frame
    void Update()
    {
        if (!is_buy)
        {
            _buyBtn.interactable = GameManager.Instance.CurrentGold >= Cost;
            //_toSellBtn.interactable = false;
        }
        else
        {
            _buyBtn.interactable = false;
            //_toSellBtn.interactable = true;
        }
        

    }

    //구매완료 버튼
    public void BuyBlock()
    {
        is_buy = true;
        GameManager.Instance.CurrentGold -= Cost;
        MerchantManager.Instance.ReturnMerchant();
        UIManager.Instance.UpdateGold();
    }

    public void ToSell()
    {
        GameManager.Instance.ChangePhase(Phase.New_sell);
        GameManager.Instance.StartPhase();
        MerchantManager.Instance.ReturnMerchant();
        UIManager.Instance.OffInven();
    }
}
