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

    [Header("Play_setting")]
    public float MaxTime = 60.0f;
    public float CurrentTime;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CurrentTime = MaxTime;
    }

    // Update is called once per frame
    void Update()
    {
        CurrentTime -= Time.deltaTime;
        UIManager.Instance.UpdateTimeGauge(CurrentTime, MaxTime);

        if (CurrentTime < 0) 
        {
            Debug.Log("장사 끝!!!");
        }
    }

    private void FixedUpdate()
    {
        //CurrentTime -= Time.deltaTime;
    }
}
