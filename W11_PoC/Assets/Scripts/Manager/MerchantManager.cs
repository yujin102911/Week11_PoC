using UnityEngine;

public class MerchantManager : MonoBehaviour
{
    public static MerchantManager Instance;

    [Header("상인 프리블록 패널")]
    [SerializeField]
    private FreeBlockPanel _merchantPanel;

    [Header("데이터베이스")]
    [SerializeField] private BlockDatabase[] _goods_DBs;

    [Header("설정")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private GameObject _merchantPrefab;

    private Merchant _currentMerchant;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //상인 소환
    public void SpawnMerchant()
    {
        if(_merchantPanel != null)
            _merchantPanel.SpawnAreaClear();

        GameObject prefab = _merchantPrefab;

        if (prefab == null)
        {
            Debug.LogError("오류: 상인 프리팹이 없습니다!");
            return;
        }

        GameObject go = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        _currentMerchant = go.GetComponent<Merchant>();
        _currentMerchant.Setup(counterPoint.position);
        
        //_currentGuest = go.GetComponent<Guest>();
        //_currentGuest.Setup(data, counterPoint.position);

        Debug.Log($"상인 등장");
    }

    public void ReturnMerchant()
    {
        if (_currentMerchant == null) return;

        UIManager.Instance.OffMerchant();
        Destroy(_currentMerchant.gameObject);
        _currentMerchant = null;

        _merchantPanel.Is_spawn = false;
    }
}
