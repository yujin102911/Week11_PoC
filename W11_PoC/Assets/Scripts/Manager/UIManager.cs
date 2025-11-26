using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Timer")]
    [SerializeField]
    private GameObject _timer;
    [SerializeField]
    private Image _timeGauge;
    [SerializeField]
    private TextMeshProUGUI _timerTxt;

    [Header("Submit")]
    [SerializeField]
    private GameObject _submitPanel;
    [SerializeField]
    private GameObject _submitGrid;

    [Header("Spwan")]
    [SerializeField]
    private GameObject _spwanPanel;
    [SerializeField]
    private Button _playButton;

    [Header("Storage")]
    [SerializeField]
    private GameObject _storagePanel;
    [SerializeField]
    private GameObject[] _storageGrid;
    [SerializeField]
    private TextMeshProUGUI _storageTxt;

    [Header("Finish")]
    [SerializeField]
    private GameObject _finishPanel;
    [SerializeField]
    private TextMeshProUGUI _resultTxt;
    [SerializeField]
    private Button _nextButton;

    [Header("Finish")]
    [SerializeField]
    private GameObject _invenPanel;

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

    public void ClickPlayBtn()
    {
        _playButton.interactable = false;
        OffSpwan();

        GameManager.Instance.ChangePhase(Phase.sell);
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


        GameManager.Instance.ChangePhase(Phase.prepare);
        GameManager.Instance.StartPhase();

        _finishPanel.SetActive(false);
    }

    public void OpenSubmit()
    {
        _submitPanel.SetActive(true);
        _submitGrid.SetActive(true);
        Is_panel = true;
    }

    public void OffSubmit()
    {
        _submitPanel.SetActive(false);
        _submitGrid.SetActive(false);
        Is_panel = false;
    }

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

    public void OpenInven()
    {
        _invenPanel.SetActive(true);
    }

    public void OffInven()
    {
        _invenPanel.SetActive(false);
    }

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

    public void UpdateTimeGauge(float c, float m)
    {
        if (_timeGauge != null)
            _timeGauge.fillAmount = c / m;

        _timerTxt.text = Mathf.FloorToInt(c).ToString();
    }

    public void UpdatePhaseMessage(Phase phase)
    {
        switch (phase)
        {
            case Phase.prepare:
                _phaseTxt.text = "준비중...";
                _timer.SetActive(false);
                break;
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

}
