using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public bool Is_panel = false;

    [Header("Timer")]
    [SerializeField]
    private Image _timeGauge;
    [SerializeField]
    private TextMeshProUGUI _timerTxt;

    [Header("Storage")]
    [SerializeField]
    private GameObject _storagePanel;
    [SerializeField]
    private GameObject[] _storageGrid;
    [SerializeField]
    private TextMeshProUGUI _storageTxt;

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
        if(Is_panel && Input.GetKeyDown(KeyCode.Escape))
        {
            OffStorage();
        }
    }

    public void OpenStorage(string id)
    {
        _storagePanel.SetActive(true);

        switch (id)
        {
            case "1":
                _storageGrid[0].SetActive(true);
                _storageTxt.text = "1번 상자";
                break;
            case "2":
                _storageGrid[1].SetActive(true);
                _storageTxt.text = "2번 상자";
                break;
            case "3":
                _storageGrid[2].SetActive(true);
                _storageTxt.text = "3번 상자";
                break;
            case "4":
                _storageGrid[3].SetActive(true);
                _storageTxt.text = "4번 상자";
                break;
        }

        Is_panel = true;
    }

    public void OffStorage()
    {
        for(int i = 0; i < _storageGrid.Length; i++)
        {
            _storageGrid[i].SetActive(false);
        }

        _storagePanel.SetActive(false);

        Is_panel = false;
    }

    public void UpdateTimeGauge(float c, float m)
    {
        if (_timeGauge != null)
            _timeGauge.fillAmount = c / m;

        _timerTxt.text = Mathf.FloorToInt(c).ToString();
    }
}
