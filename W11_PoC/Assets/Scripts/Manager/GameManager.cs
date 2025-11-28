using UnityEngine;

public enum Phase
{
    None,
    prepare,
    sell, 
    New_mode
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Is_new")]
    public bool Is_new;

    [Header("Object")]
    [SerializeField]
    private GameObject _prepareBox;
    [SerializeField]
    private GameObject _player;

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
        //게임 시작
        if (Is_new)
        {
            ChangePhase(Phase.New_mode);
        }
        else
        {
            ChangePhase(Phase.prepare);
        }
            
        StartPhase();
    }

    // Update is called once per frame
    void Update()
    {
        if (_phase == Phase.sell || _phase == Phase.New_mode)
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

    public Phase GetPhase()
    {
        return _phase;
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
                if (CurrentStage >= MaxStage)
                {
                    QuitGame();
                }

                PlayerOff();
                //_prepareBox.SetActive(true);
                UIManager.Instance.OpenSpwan();
                _blockSpawner.Is_spawn = false;
                //BlockSpawner.Instance.Is_spawn = false;
                break;
            case Phase.sell:

                PlayerOn();
                //_prepareBox.SetActive(false);
                CurrentTime = MaxTime;
                GuestManager.Instance.LoadStage(CurrentStage);
                CurrentStage++;
                
                break;

            case Phase.New_mode:
                CurrentTime = MaxTime;
                GuestManager.Instance.LoadStage(CurrentStage);
                CurrentStage++;
                return;
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

    //플레이어 온
    public void PlayerOn()
    {
        // _player.SetActive(true);
        UIManager.Instance.OpenInven();
    }

    //플레이어 온
    public void PlayerOff()
    {
        // _player.SetActive(false);
        UIManager.Instance.OffInven();
    }
}
