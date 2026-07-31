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
        // SoundManager.Instance.PlaySound(SoundType.GameBackgroundMusic);

        Application.targetFrameRate = 200;
        QualitySettings.vSyncCount = 1;
        
        Play(); // Otomatik başlat
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
        if (scoreText != null) scoreText.text = score.ToString();
        
        if (playButton != null) playButton.SetActive(false);

        player.enabled = true;
        Time.timeScale = 1f;
        if (spawner != null) spawner.StartSpawning();
        if (bgSystem != null) bgSystem.StartMoving();
    }

    public void GameOver()
    {
        if (bgSystem != null) bgSystem.StopMoving();
        if (restartButton != null) restartButton.SetActive(true);
        if (gameOver != null) gameOver.SetActive(true);
        if (spawner != null) spawner.StopSpawning();
        // SoundManager.Instance.StopSound(SoundType.GameBackgroundMusic);
        // SoundManager.Instance.PlaySound(SoundType.DieSound);
        Pause();
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
        return;
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

                if (scoreText != null)
                {
                    scoreText.text = score.ToString();
                    var anim = scoreText.GetComponent<Animation>();
                    if (anim != null) anim.Play();
                }
            }
        }

        if (frameText != null) frameText.text = ((int)(1.0f / Time.deltaTime)).ToString();
    }
}
