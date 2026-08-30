using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// Drives the top-level UI flow: main menu, HUD visibility, and the
    /// post-game results screen with per-side kill/loss statistics.
    /// Panels are wired in TotalDeckSceneSetup / scene patch scripts.
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject gameOverPanel;
        public GameObject hudRoot; // TopBar + BottomPanel + HillScoreboard etc.

        [Header("Main Menu Buttons")]
        public Button startButton;
        public Button settingsButton;
        public Button multiplayerButton;
        public Button quitButton;
        public Text menuNoticeText; // "coming soon" placeholder message

        [Header("Results Screen")]
        public Text winnerText;
        public Text finalScoreText;
        public Text playerStatsText;
        public Text enemyStatsText;
        public Button rematchButton;
        public Button backToMenuButton;

        float noticeTimer;

        void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowNotice("设置功能开发中，敬请期待"));
            if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => ShowNotice("多人游戏开发中，敬请期待"));
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (rematchButton != null) rematchButton.onClick.AddListener(OnRematchClicked);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnGameEnded += OnGameEnded;
            }

            ApplyState(GameManager.Instance != null ? GameManager.Instance.State : GameState.MainMenu);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnGameEnded -= OnGameEnded;
            }
        }

        /// <summary>
        /// Victory/defeat moment: refresh panels to show the results screen.
        /// Phase does NOT change when a game ends, so this needs its own hook.
        /// </summary>
        void OnGameEnded(Team winner)
        {
            ApplyState(GameState.GameOver);
        }

        void Update()
        {
            // Fade the placeholder notice after a moment
            if (noticeTimer > 0f)
            {
                noticeTimer -= Time.unscaledDeltaTime;
                if (noticeTimer <= 0f && menuNoticeText != null)
                    menuNoticeText.text = "";
            }
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (GameManager.Instance != null)
                ApplyState(GameManager.Instance.State);
        }

        void ApplyState(GameState state)
        {
            bool inMenu = state == GameState.MainMenu;
            bool gameOver = state == GameState.GameOver;
            bool playing = state == GameState.Playing;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(inMenu);
            if (gameOverPanel != null) gameOverPanel.SetActive(gameOver);
            if (hudRoot != null) hudRoot.SetActive(playing || gameOver);
            if (menuNoticeText != null && menuNoticeText.transform.parent == mainMenuPanel.transform)
                menuNoticeText.text = "";

            if (gameOver)
                FillResults();
        }

        void FillResults()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool playerWon = gm.LastWinner == Team.Player;
            if (winnerText != null)
            {
                winnerText.text = playerWon ? "VICTORY!" : "DEFEAT";
                winnerText.color = playerWon
                    ? new Color(0f, 1f, 0.67f)
                    : new Color(1f, 0.3f, 0.3f);
            }

            if (finalScoreText != null)
            {
                var hill = HillZone.Instance;
                int pScore = hill != null ? hill.PlayerScore : 0;
                int eScore = hill != null ? hill.EnemyScore : 0;
                finalScoreText.text = $"占领积分  {pScore} : {eScore}";
            }

            if (playerStatsText != null)
            {
                var s = gm.PlayerStats;
                playerStatsText.text = $"击杀 {s.Kills}    阵亡 {s.Losses}";
            }

            if (enemyStatsText != null)
            {
                var s = gm.EnemyStats;
                enemyStatsText.text = $"击杀 {s.Kills}    阵亡 {s.Losses}";
            }
        }

        void ShowNotice(string msg)
        {
            if (menuNoticeText == null) return;
            menuNoticeText.text = msg;
            noticeTimer = 2.5f;
        }

        void OnStartClicked()
        {
            GameManager.Instance?.StartNewGame();
        }

        void OnRematchClicked()
        {
            GameManager.Instance?.StartNewGame();
        }

        void OnBackToMenuClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
