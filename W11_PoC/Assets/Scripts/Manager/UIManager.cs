using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public bool Is_panel = false;

    [Header("Phase_Message")]
    [SerializeField]
    private TextMeshProUGUI _phaseTxt;

    [Header("Point")]
    [SerializeField]
    private TextMeshProUGUI _pointTxt;

    [Header("Gold")]
    [SerializeField]
    private TextMeshProUGUI _goldTxt;

    [Header("Customer_Num")]
    [SerializeField]
    private TextMeshProUGUI _customerTxt;

    [Header("Timer")]
    [SerializeField]
    private GameObject _timer;
    [SerializeField]
    private Image _timeGauge;
    [SerializeField]
    private TextMeshProUGUI _timerTxt;

    [Tab("제출 패널")]
    [Header("Submit")]
    [SerializeField]
    private GameObject _submitPanel;
    [SerializeField]
    private Grid _submitGrid;
    [SerializeField]
    private Button _submitButton;

    [Tab("입고 패널")]
    [Header("Spwan")]
    [SerializeField]
    private GameObject _spwanPanel;
    [SerializeField]
    private Button _playButton;

    [Tab("저장 박스 패널")]
    [Header("Storage")]
    [SerializeField]
    private GameObject _storagePanel;
    [SerializeField]
    private GameObject[] _storageGrid;
    [SerializeField]
    private TextMeshProUGUI _storageTxt;

    [Tab("자유블록 박스 패널")]
    [SerializeField]
    private GameObject _freeBlockPanel;
    [SerializeField]
    private FreeBlockPanel[] _blockArea;
    [SerializeField]
    private TextMeshProUGUI _freeBlockTxt;

    [Tab("종료 패널")]
    [Header("Finish")]
    [SerializeField]
    private GameObject _finishPanel;
    [SerializeField]
    private TextMeshProUGUI _resultTxt;
    [SerializeField]
    private Button _nextButton;

    [Tab("플레이어 가방")]
    [Header("Inven")]
    [SerializeField]
    private GameObject _invenPanel;

    [Tab("상인")]
    [Header("Merchant")]
    [SerializeField]
    private GameObject _merchantPanel;

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


    #region 종료 패널
    public void OpenFinish()
    {
        _finishPanel.SetActive(true);
        _resultTxt.text = $"점수: +{GameManager.Instance.StagePointed}";

        _nextButton.onClick.RemoveAllListeners();
        _nextButton.onClick.AddListener(ClickNextBtn);

        Time.timeScale = 0f;
    }

    public void ClickNextBtn()
    {
        Time.timeScale = 1.0f;

        if(GameManager.Instance.GetPhase() == Phase.sell )
            GameManager.Instance.ChangePhase(Phase.prepare);

        if (GameManager.Instance.GetPhase() == Phase.New_sell)
            GameManager.Instance.ChangePhase(Phase.New_prepare);

        GameManager.Instance.StartPhase();

        _finishPanel.SetActive(false);
    }
    #endregion

    #region 제출 패널
    public void OpenSubmit(GuestPattern guestPattern,bool is_new)
    {
        _submitPanel.SetActive(true);
        _submitGrid.gameObject.SetActive(true);

        //새로운 버전일때만 패턴 넘기기
        if (is_new)
        {
            _submitGrid.SetGrid(guestPattern);
        }
        else
        {
            _submitGrid.SetGrid(null);
            Is_panel = true;
        }
    }

    public void OffSubmit()
    {
        _submitPanel.SetActive(false);
        _submitGrid.gameObject.SetActive(false);
        Is_panel = false;
    }

    public void ClickSubmitBtn()
    {
        _submitButton.interactable = false;
        OffSubmit();
        //점수 계산
        GuestManager.Instance.GridSuccess();
    }

    // 제출 Grid에서 판단
    public void ActiveSubmitBtn()
    {
        _submitButton.interactable = true;

        _submitButton.onClick.RemoveAllListeners();
        _submitButton.onClick.AddListener(ClickSubmitBtn);
    }

    public void UnActiveSubmitBtn()
    {
        _submitButton.interactable = false;

        _submitButton.onClick.RemoveAllListeners();
    }
    #endregion

    #region 상인
    public void OpenMerchant()
    {
        _merchantPanel.SetActive(true);
    }

    public void OffMerchant()
    {
        _merchantPanel.SetActive(false);
    }
    #endregion

    #region 입고 박스
    public void OpenSpwan()
    {
        _spwanPanel.SetActive(true);
        //이제 정리 페이즈에서는 플레이어 없음
        //Is_panel = true;
    }

    public void OffSpwan()
    {
        _spwanPanel.SetActive(false);
        //이제 정리 페이즈에서는 플레이어 없음
        //Is_panel = false;
    }

    public void ClickPlayBtn()
    {
        _playButton.interactable = false;
        OffSpwan();

        if (GameManager.Instance.GetPhase() == Phase.prepare) {
            GameManager.Instance.ChangePhase(Phase.sell);
        }
        else if(GameManager.Instance.GetPhase() == Phase.New_prepare){
            GameManager.Instance.ChangePhase(Phase.New_sell);
        }

        GameManager.Instance.StartPhase();
    }

    //blockSpawner에서 호출
    public void ActivePlayBtn()
    {
        _playButton.interactable = true;

        _playButton.onClick.RemoveAllListeners();
        _playButton.onClick.AddListener(ClickPlayBtn);
    }

    public void UnActivePlayBtn()
    {
        _playButton.interactable = false;

        _playButton.onClick.RemoveAllListeners();
    }
    #endregion

    #region 인벤토리
    public void OpenInven()
    {
        _invenPanel.SetActive(true);
    }

    public void OffInven()
    {
        _invenPanel.SetActive(false);
    }
#endregion

    #region 저장 박스
    //저장 박스 패널 열기
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
    #endregion

    //저장 박스 패널 열기
    public void OpenFreeBlock(string id)
    {
        _freeBlockPanel.SetActive(true);

        if (int.TryParse(id, out int value))
        {
            _blockArea[value - 1].gameObject.SetActive(true);
            _freeBlockTxt.text = id + "번 상자";
        }

        Is_panel = true;
    }

    public void OffFreeBlock()
    {
        for (int i = 0; i < _blockArea.Length; i++)
        {
            _blockArea[i].gameObject.SetActive(false);
        }

        _freeBlockPanel.SetActive(false);

        Is_panel = false;
    }


    //타이머 게이지 줄어드는 효과
    public void UpdateTimeGauge(float c, float m)
    {
        if (_timeGauge != null)
            _timeGauge.fillAmount = c / m;

        _timerTxt.text = Mathf.FloorToInt(c).ToString();
    }

    public void UpdatePhaseMessage(Phase phase)
    {
        Debug.Log("안해?");

        switch (phase)
        {
            case Phase.New_prepare:
            case Phase.prepare:
                _phaseTxt.text = "준비중...";
                _timer.SetActive(false);
                _customerTxt.text = "";
                break;
            case Phase.New_sell:
            case Phase.sell:
                _phaseTxt.text = "영업중...";
                _timer.SetActive(true);
                break;
        }
    }
    public void UpdatePoint(int point)
    {
        _pointTxt.text = $"점수: {point}";
    }

    public void UpdateGold()
    {
        _goldTxt.text = $"G: " + string.Format("{0:#,###}", GameManager.Instance.CurrentGold);
    }

    public void UpdateCustomer(int c, int max)
    {
        _customerTxt.text = $"손님: ({c} / {max})";
    }

}
