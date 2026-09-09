using TMPro;
using UnityEngine;

namespace YesChef
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string HighScoreKey = "YesChef.HighScore";
        public static GameManager Instance { get; private set; }

        [Header("Game")]
        public float matchSeconds = 180f;
        public PlayerController player;
        public CustomerWindow[] windows;
        public ChoppingTableStation choppingTable;
        public StoveStation stove;

        [Header("HUD")]
        public TMP_Text timerText;
        public TMP_Text scoreText;
        public TMP_Text highScoreText;
        public TMP_Text interactionText;

        [Header("Panels")]
        public GameObject instructionsPanel;
        public GameObject pausePanel;
        public GameObject resultsPanel;
        public TMP_Text resultScoreText;
        public TMP_Text newHighScoreText;

        public GamePhase Phase { get; private set; } = GamePhase.Instructions;
        public int Score { get; private set; }
        public int HighScore { get; private set; }

        private float remainingTime;

        private void Awake()
        {
            Instance = this;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            remainingTime = matchSeconds;
            Time.timeScale = 0f;
        }

        private void Start()
        {
            ShowInstructions();
            RefreshHud();
        }

        private void Update()
        {
            if (Phase != GamePhase.Playing) return;
            remainingTime -= Time.unscaledDeltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                EndGame();
            }
            RefreshHud();
        }

        public void BeginGame()
        {
            Time.timeScale = 1f;
            Phase = GamePhase.Playing;
            Score = 0;
            remainingTime = matchSeconds;
            player.ResetPlayer();
            choppingTable.ResetStation();
            stove.ResetStation();
            foreach (var customerWindow in windows) customerWindow.ResetWindow();
            instructionsPanel.SetActive(false);
            pausePanel.SetActive(false);
            resultsPanel.SetActive(false);
            SetInteractionPrompt(string.Empty);
            RefreshHud();
        }

        public void TogglePause()
        {
            if (Phase == GamePhase.Playing)
            {
                Phase = GamePhase.Paused;
                Time.timeScale = 0f;
                pausePanel.SetActive(true);
            }
            else if (Phase == GamePhase.Paused)
            {
                Phase = GamePhase.Playing;
                Time.timeScale = 1f;
                pausePanel.SetActive(false);
            }
        }

        public void AddScore(int amount)
        {
            Score += amount;
            RefreshHud();
        }

        public void SetInteractionPrompt(string value)
        {
            if (interactionText != null) interactionText.text = value;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowInstructions()
        {
            Phase = GamePhase.Instructions;
            instructionsPanel.SetActive(true);
            pausePanel.SetActive(false);
            resultsPanel.SetActive(false);
        }

        private void EndGame()
        {
            Phase = GamePhase.Results;
            Time.timeScale = 0f;
            var isNewHighScore = Score > HighScore;
            if (isNewHighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            resultScoreText.text = $"Final score: <b>{Score}</b>\nHigh score: <b>{HighScore}</b>";
            newHighScoreText.text = isNewHighScore ? "NEW HIGH SCORE!" : string.Empty;
            resultsPanel.SetActive(true);
            RefreshHud();
        }

        private void RefreshHud()
        {
            var seconds = Mathf.CeilToInt(remainingTime);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            scoreText.text = $"SCORE  {Score}";
            highScoreText.text = $"BEST  {HighScore}";
        }
    }
}
