using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [SerializeField] private float timeUntilHorde = 120f; 
    [SerializeField] private TextMeshProUGUI timerText; 

    private float _currentTime;
    private bool _hordeActivated = false;

    public bool IsHordeActive => _hordeActivated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        _currentTime = timeUntilHorde;
    }

    private void Update()
    {
        if (_hordeActivated) return;

        _currentTime -= Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(_currentTime / 60f);
            int seconds = Mathf.FloorToInt(_currentTime % 60f);
            timerText.text = $"Îðäà ÷åðåç: {minutes:00}:{seconds:00}";
        }

        if (_currentTime <= 0f)
        {
            ActivateHorde();
        }
    }

    private void ActivateHorde()
    {
        _hordeActivated = true;

        if (timerText != null)
        {
            timerText.text = "ÎÐÄÀ ÀÊÒÈÂÎÂÀÍÀ!";
            timerText.color = Color.red;
        }

        Debug.Log("HORDE ACTIVATED! All zombies now chase the player!");

        NPCControllerAI[] allNPCs = FindObjectsOfType<NPCControllerAI>();
        foreach (NPCControllerAI npc in allNPCs)
        {
            npc.ActivateHordeMode();
        }
    }


    public void ForceActivateHorde()
    {
        if (!_hordeActivated)
        {
            _currentTime = 0f;
            ActivateHorde();
        }
    }
}