using UnityEngine;

public enum Phase
{
    None,
    prepare,
    sell
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Object")]
    [SerializeField]
    private GameObject _prepareBox;

    [SerializeField]
    private BlockSpawner _blockSpawner;

    [Header("Play_setting")]
    public float MaxTime = 60.0f;
    public float CurrentTime;
    public int MaxStage;
    public int CurrentStage;
    public int TotalPoint;
    public int StagePointed;



    private Phase _phase;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangePhase(Phase.prepare);
        StartPhase();
    }

    // Update is called once per frame
    void Update()
    {
        if (_phase == Phase.sell)
        {
            CurrentTime -= Time.deltaTime;
            UIManager.Instance.UpdateTimeGauge(CurrentTime, MaxTime);

            if (CurrentTime < 0)
            {
                Debug.Log("장사 끝!!!");
                RoundOver();
            }
        }
    }

    private void FixedUpdate()
    {
        //CurrentTime -= Time.deltaTime;
    }

    public void ChangePhase(Phase next)
    {
        _phase = next;
    }

    public void StartPhase()
    {
        StagePointed = 0;

        switch (_phase)
        {
            case Phase.prepare:
                _prepareBox.SetActive(true);
                _blockSpawner.Is_spawn = false;
                //BlockSpawner.Instance.Is_spawn = false;
                break;
            case Phase.sell:
                if (CurrentStage >= MaxStage)
                {
                    QuitGame();
                }

                _prepareBox.SetActive(false);
                CurrentTime = MaxTime;
                GuestManager.Instance.LoadStage(CurrentStage);
                CurrentStage++;
                
                break;
        }

        UIManager.Instance.UpdatePhaseMessage(_phase);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        // 에디터에서 플레이 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // 빌드된 게임 종료
    Application.Quit();
#endif
    }


    public void RoundClear()
    {
        UIManager.Instance.OpenFinish();
    }

    public void RoundOver()
    {
        UIManager.Instance.OpenFinish();
    }

    public void GainPoint(int point)
    {
        StagePointed += point;
        TotalPoint += point;
        UIManager.Instance.UpdatePoint(TotalPoint);
    }
}
