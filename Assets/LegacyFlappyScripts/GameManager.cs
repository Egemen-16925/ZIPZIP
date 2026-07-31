namespace Legacy2D {
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Player player;
    [SerializeField] private Spawner spawner;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private BackgroundSystem bgSystem;
    [SerializeField] private TextMeshProUGUI frameText;
    public PipeMover mover;

    [Header("Ground Flow")]
    [SerializeField, Min(0f)] private float groundFlowSpeed = 1.35f;

    public int score { get; private set; } = 0;

    private float scoreTimer;
    private bool isRunActive;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        ApplyGroundFlowSpeed();
        Pause();
        SoundManager.Instance.PlaySound(SoundType.GameBackgroundMusic);

        Application.targetFrameRate = 200;
        QualitySettings.vSyncCount = 1;
    }

    private void OnValidate()
    {
        groundFlowSpeed = Mathf.Max(0f, groundFlowSpeed);
        ApplyGroundFlowSpeed();
    }

    private void ApplyGroundFlowSpeed()
    {
        if (bgSystem != null)
        {
            bgSystem.SetForegroundSpeed(groundFlowSpeed);
        }
    }

    public void Pause()
    {
        isRunActive = false;
        player.enabled = false;
        mover.canMove = false;
    }

    public void Play()
    {
        player.OnPlay();
        mover.canMove = true;
        mover.moveSpawner = false;
        score = 0;
        scoreTimer = 0f;
        isRunActive = true;
        scoreText.text = score.ToString();
        
        playButton.SetActive(false);

        player.enabled = true;
        Time.timeScale = 1f;
        spawner.StartSpawning();
        bgSystem.StartMoving();
    }

    public void GameOver()
    {
        bgSystem.StopMoving();
        restartButton.SetActive(true);
        gameOver.SetActive(true);
        spawner.StopSpawning();
        SoundManager.Instance.StopSound(SoundType.GameBackgroundMusic);
        SoundManager.Instance.PlaySound(SoundType.DieSound);
        bool isGameEnded = true;
        UnityPipeCommunication.ClearLogFile();
        UnityPipeCommunication.SendMessageToElectron($"SCORE:{4}:{score}:{DateTime.Now}:{PlayBoxLauncherDataManager.RoundToNearestVolume()}");
        Pause();
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void IncreaseScore()
    {
        // Progress is time-based. Passing an obstacle must not change it.
    }

    private void Update()
    {
        if (isRunActive)
        {
            scoreTimer += Time.deltaTime;
            while (scoreTimer >= 1f)
            {
                scoreTimer -= 1f;
                score++;
                scoreText.text = score.ToString();
                scoreText.GetComponent<Animation>().Play();
            }
        }

        frameText.text = ((int)(1.0f / Time.deltaTime)).ToString();
    }
}

}
