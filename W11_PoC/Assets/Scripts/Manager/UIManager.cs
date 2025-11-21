using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Timer")]
    [SerializeField]
    private Image _timeGauge;
    [SerializeField]
    private TextMeshProUGUI _timerTxt;

    [Header("Storage")]
    [SerializeField]
    private GameObject _storagePanel;

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

    public void OpenStorage()
    {
        _storagePanel.SetActive(true);
    }

    public void UpdateTimeGauge(float c, float m)
    {
        if (_timeGauge != null)
            _timeGauge.fillAmount = c / m;

        _timerTxt.text = Mathf.FloorToInt(c).ToString();
    }
}
