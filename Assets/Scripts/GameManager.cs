using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public int totalPins = 9;
    private int pinsHit = 0;
    public float timeLimit = 30f;
    private float timeRemaining;
    public TMP_Text timerText;
    public TMP_Text gameStateText;
    public Button restartButton;
    private bool gameOver = false;
    public GameObject pinSet;
    private AudioSource backgroundMusic;
    public AudioClip mainTheme;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    private AudioSource victorySound;
    public AudioClip winSound;
    public AudioClip loseSound;

    void Start()
    {
        backgroundMusic = gameObject.AddComponent<AudioSource>();
        backgroundMusic.clip = mainTheme;
        backgroundMusic.loop = true;
        backgroundMusic.volume = musicVolume;
        backgroundMusic.playOnAwake = false;
        backgroundMusic.Play();

        timeRemaining = timeLimit;
        UpdateTimerUI();
        SetupUI();
    }

    void SetupUI()
    {
        if (gameStateText != null)
            gameStateText.gameObject.SetActive(false);
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    void Update()
    {
        if (!gameOver && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (pinsHit >= totalPins)
            {
                Win();
            }
            else if (timeRemaining <= 0)
            {
                TimeOut();
            }
        }
    }

    public void PinHit()
    {
        if (!gameOver)
        {
            pinsHit++;
            Debug.Log($"Quilles touchées : {pinsHit}/{totalPins}");
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Temps : {Mathf.Ceil(timeRemaining)}s\nQuilles : {pinsHit}/{totalPins}";
        }
    }

    void Win()
    {
        gameOver = true;
        
        if (winSound != null)
        {
            AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position, 0.5f);
        }
        
        var npc = FindAnyObjectByType<NPCController>();
        if (npc != null)
        {
            npc.OnGameWon();
        }

        if (gameStateText != null)
        {
            gameStateText.gameObject.SetActive(true);
            gameStateText.text = "VICTOIRE !";
            gameStateText.color = Color.green;
            StartCoroutine(AnimateText(gameStateText.transform));
        }
        ShowRestartButton();
    }

    public void Lose()
    {
        gameOver = true;

        if (loseSound != null)
        {
            AudioSource.PlayClipAtPoint(loseSound, Camera.main.transform.position, 0.5f);
        }

        var bowlingBall = FindAnyObjectByType<BowlingBall>();
        if (bowlingBall != null)
        {
            bowlingBall.SetGameOver(true);
        }

        if (gameStateText != null)
        {
            gameStateText.gameObject.SetActive(true);
            gameStateText.text = "GAME OVER!\nTimmy a attrapé la balle!";
            gameStateText.color = Color.red;
            StartCoroutine(AnimateText(gameStateText.transform));
        }
        ShowRestartButton();
    }

    void ShowRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameSequence());
    }

    private IEnumerator RestartGameSequence()
    {
        gameOver = false;
        pinsHit = 0;
        timeRemaining = timeLimit;
        
        if (gameStateText != null)
            gameStateText.gameObject.SetActive(false);
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);
        
        var bowlingBall = FindAnyObjectByType<BowlingBall>();
        if (bowlingBall != null)
        {
            bowlingBall.ResetBall();
            bowlingBall.RestartGame();
        }

        yield return new WaitForSeconds(0.2f);
        
        // Réinitialiser les quilles
        if (pinSet != null)
        {
            Pin[] pins = pinSet.GetComponentsInChildren<Pin>(true);
            foreach (Pin pin in pins)
            {
                pin.ResetPin();
                yield return new WaitForSeconds(0.05f); 
            }
        }

        yield return new WaitForSeconds(0.2f);
        
        var npcController = FindAnyObjectByType<NPCController>();
        if (npcController != null)
            npcController.RestartBehavior();
        
        UpdateTimerUI();
    }

    private IEnumerator AnimateText(Transform textTransform)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.3f;
            textTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        
        textTransform.localScale = Vector3.one;
    }

    public void TimeOut()
    {
        gameOver = true;
        if (gameStateText != null)
        {
            gameStateText.gameObject.SetActive(true);
            gameStateText.text = "TEMPS ÉCOULÉ!";
            gameStateText.color = Color.red;
            StartCoroutine(AnimateText(gameStateText.transform));
        }
        ShowRestartButton();
    }
}