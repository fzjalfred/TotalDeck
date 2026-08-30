using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// Drives the top-level UI flow:
    ///  MainMenu (start/settings/multiplayer/quit)
    ///    -> SetupScreen (map + spawn slot configuration) -> match
    ///  GameOver (results with kill/loss stats, rematch / back to menu)
    /// HUD visibility follows the state. Panels are wired in
    /// TotalDeckSceneSetup / scene patch scripts.
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject setupPanel;        // NEW: map/spawn config submenu
        public GameObject gameOverPanel;
        public GameObject hudRoot;

        [Header("Main Menu Buttons")]
        public Button startButton;           // "开始游戏" -> opens SetupScreen
        public Button settingsButton;
        public Button multiplayerButton;
        public Button quitButton;
        public Text menuNoticeText;

        [Header("Setup Screen Controls")]
        public Dropdown mapDropdown;
        public Dropdown playerSlotDropdown;
        public Dropdown enemySlotDropdown;
        public Button beginBattleButton;     // "开始战斗" -> launches the match
        public Button setupBackButton;       // back to main menu

        [Header("Results Screen")]
        public Text winnerText;
        public Text finalScoreText;
        public Text playerStatsText;
        public Text enemyStatsText;
        public Button rematchButton;
        public Button backToMenuButton;

        [Header("Pause Menu (Esc in match)")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button pauseSettingsButton;
        public Button exitToMenuButton;
        public Text pauseNoticeText;

        float noticeTimer;
        float pauseNoticeTimer;
        MapDef[] availableMaps;

        void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowNotice("设置功能开发中，敬请期待"));
            if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => ShowNotice("多人游戏开发中，敬请期待"));
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (beginBattleButton != null) beginBattleButton.onClick.AddListener(OnBeginBattleClicked);
            if (setupBackButton != null) setupBackButton.onClick.AddListener(OnSetupBackClicked);
            if (rematchButton != null) rematchButton.onClick.AddListener(OnRematchClicked);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (pauseSettingsButton != null) pauseSettingsButton.onClick.AddListener(() => ShowPauseNotice("设置功能开发中，敬请期待"));
            if (exitToMenuButton != null) exitToMenuButton.onClick.AddListener(OnExitToMenuClicked);

            if (playerSlotDropdown != null)
                playerSlotDropdown.onValueChanged.AddListener(_ => OnPlayerSlotChanged());
            if (enemySlotDropdown != null)
                enemySlotDropdown.onValueChanged.AddListener(_ => OnEnemySlotChanged());

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnGameEnded += OnGameEnded;
                GameManager.Instance.OnStateChanged += OnStateChanged;
            }

            PopulateMapDropdown();
            ApplyState(GameManager.Instance != null ? GameManager.Instance.State : GameState.MainMenu);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnGameEnded -= OnGameEnded;
                GameManager.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        /// <summary>
        /// GameState changed (e.g. exited to menu from the pause screen) —
        /// refresh all panels. Without this, ReturnToMenu left the HUD on.
        /// </summary>
        void OnStateChanged(GameState state)
        {
            ApplyState(state);
        }

        // ── Setup screen data ─────────────────────────────

        /// <summary>
        /// Fill the map dropdown from MapDef.Available() and build the slot
        /// dropdowns for the first map. Player defaults to slot 0.
        /// </summary>
        void PopulateMapDropdown()
        {
            availableMaps = MapDef.Available();

            if (mapDropdown != null)
            {
                mapDropdown.ClearOptions();
                var opts = new System.Collections.Generic.List<string>();
                foreach (var m in availableMaps) opts.Add(m.mapName);
                mapDropdown.AddOptions(opts);
                mapDropdown.onValueChanged.AddListener(_ => OnMapChanged());
                if (availableMaps.Length > 0)
                    OnMapChanged(); // initializes slot dropdowns from map 0
            }
        }

        void OnMapChanged()
        {
            if (mapDropdown == null || availableMaps == null) return;
            if (mapDropdown.value >= availableMaps.Length) return;
            BuildSlotDropdowns(availableMaps[mapDropdown.value]);
        }

        void BuildSlotDropdowns(MapDef map)
        {
            var slotOptions = new System.Collections.Generic.List<string>();
            for (int i = 0; i < map.spawnPoints.Length; i++)
                slotOptions.Add($"出生点 {i}");

            if (playerSlotDropdown != null)
            {
                playerSlotDropdown.ClearOptions();
                playerSlotDropdown.AddOptions(slotOptions);
                playerSlotDropdown.value = 0;
                playerSlotDropdown.RefreshShownValue();
            }

            if (enemySlotDropdown != null)
            {
                enemySlotDropdown.ClearOptions();
                enemySlotDropdown.AddOptions(slotOptions);
                enemySlotDropdown.value = map.spawnPoints.Length > 1 ? 1 : 0;
                enemySlotDropdown.RefreshShownValue();
            }
        }

        void OnPlayerSlotChanged()
        {
            if (playerSlotDropdown == null || enemySlotDropdown == null) return;
            if (playerSlotDropdown.value == enemySlotDropdown.value)
            {
                var map = CurrentMap();
                int firstOther = 0;
                for (int i = 0; i < map.spawnPoints.Length; i++)
                    if (i != playerSlotDropdown.value) { firstOther = i; break; }
                enemySlotDropdown.value = firstOther;
                enemySlotDropdown.RefreshShownValue();
            }
        }

        void OnEnemySlotChanged()
        {
            if (playerSlotDropdown == null || enemySlotDropdown == null) return;
            if (enemySlotDropdown.value == playerSlotDropdown.value)
            {
                var map = CurrentMap();
                int firstOther = 0;
                for (int i = 0; i < map.spawnPoints.Length; i++)
                    if (i != enemySlotDropdown.value) { firstOther = i; break; }
                playerSlotDropdown.value = firstOther;
                playerSlotDropdown.RefreshShownValue();
            }
        }

        MapDef CurrentMap()
        {
            if (availableMaps == null || availableMaps.Length == 0) return MapDef.Duel();
            int idx = mapDropdown != null ? mapDropdown.value : 0;
            if (idx >= availableMaps.Length) idx = 0;
            return availableMaps[idx];
        }

        // ── State handling ────────────────────────────────

        void OnPhaseChanged(GamePhase phase)
        {
            if (GameManager.Instance != null)
                ApplyState(GameManager.Instance.State);
        }

        /// <summary>
        /// Victory/defeat moment: refresh panels to show the results screen.
        /// Phase does NOT change when a game ends, so this needs its own hook.
        /// </summary>
        void OnGameEnded(Team winner)
        {
            ApplyState(GameState.GameOver);
        }

        /// <summary>
        /// Menu UI has one extra screen (setup) that isn't a GameState —
        /// it lives inside MainMenu and is toggled by the setup flag.
        /// </summary>
        bool setupScreenActive;

        void ApplyState(GameState state)
        {
            bool inMenu = state == GameState.MainMenu;
            bool gameOver = state == GameState.GameOver;
            bool playing = state == GameState.Playing;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(inMenu);
            if (gameOverPanel != null) gameOverPanel.SetActive(gameOver);
            if (hudRoot != null) hudRoot.SetActive(playing || gameOver);

            if (inMenu)
            {
                // Entering the menu always lands on the button stack —
                // a stale setup/pause state must not survive a state change
                setupScreenActive = false;
                ShowMenuSubscreens(false);
            }

            if (gameOver)
                FillResults();
        }

        void ShowMenuSubscreens(bool setup)
        {
            setupScreenActive = setup;

            // Everything on the main panel that belongs to the button stack
            var stackNames = new[]
            {
                "TitleText", "SubtitleText", "StartButton", "SettingsButton",
                "MultiplayerButton", "QuitButton", "MenuNoticeText"
            };
            var setupNames = new[]
            {
                "MapLabel", "MapDropdown", "PlayerSlotLabel", "PlayerSlotDropdown",
                "EnemySlotLabel", "EnemySlotDropdown", "BeginBattleButton", "SetupBackButton"
            };

            if (mainMenuPanel != null)
            {
                foreach (var n in stackNames)
                {
                    var t = mainMenuPanel.transform.Find(n);
                    if (t != null) t.gameObject.SetActive(!setup);
                }
                foreach (var n in setupNames)
                {
                    var t = mainMenuPanel.transform.Find(n);
                    if (t != null) t.gameObject.SetActive(setup);
                }
            }

            // Popup z-order fix: a dropdown's popup list is a CHILD of its
            // root, so it renders below later siblings — lower rows would
            // cover an open popup. Force dropdown rows to render LAST
            // (map row topmost of all) so every popup draws over the rows
            // below it.
            if (setup)
            {
                if (enemySlotDropdown != null) enemySlotDropdown.transform.SetAsLastSibling();
                if (playerSlotDropdown != null) playerSlotDropdown.transform.SetAsLastSibling();
                if (mapDropdown != null) mapDropdown.transform.SetAsLastSibling();
            }

            // Reset the notice when entering a screen
            if (menuNoticeText != null) menuNoticeText.text = "";
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

        void ShowPauseNotice(string msg)
        {
            if (pauseNoticeText == null) return;
            pauseNoticeText.text = msg;
            pauseNoticeTimer = 2.5f;
        }

        // ── Pause menu (Esc during a match) ───────────────

        /// <summary>True while the pause overlay is shown over a match.</summary>
        bool pauseShown;

        void Update()
        {
            // Fade notices (unscaled so they work while paused)
            if (noticeTimer > 0f)
            {
                noticeTimer -= Time.unscaledDeltaTime;
                if (noticeTimer <= 0f && menuNoticeText != null)
                    menuNoticeText.text = "";
            }
            if (pauseNoticeTimer > 0f)
            {
                pauseNoticeTimer -= Time.unscaledDeltaTime;
                if (pauseNoticeTimer <= 0f && pauseNoticeText != null)
                    pauseNoticeText.text = "";
            }

            // Esc toggles the pause menu — only during a match
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.State == GameState.Playing)
                {
                    if (pauseShown) OnResumeClicked();
                    else ShowPauseMenu();
                }
            }
        }

        /// <summary>
        /// Overlay the pause menu and freeze the battlefield.
        /// </summary>
        void ShowPauseMenu()
        {
            if (pausePanel == null) return;
            pauseShown = true;
            pausePanel.SetActive(true);
            if (pauseNoticeText != null) pauseNoticeText.text = "";
            Time.timeScale = 0f; // freeze gameplay; UI runs on unscaled time
        }

        /// <summary>
        /// Close the pause overlay and resume the match.
        /// </summary>
        public void OnResumeClicked()
        {
            if (pausePanel == null) return;
            pauseShown = false;
            pausePanel.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                Time.timeScale = 1f;
        }

        /// <summary>
        /// Abandon the match and return to the main menu.
        /// </summary>
        void OnExitToMenuClicked()
        {
            if (pausePanel != null)
            {
                pauseShown = false;
                pausePanel.SetActive(false);
            }
            setupScreenActive = false;
            GameManager.Instance?.ReturnToMenu();
        }

        // ── Button handlers ───────────────────────────────

        void OnStartClicked()
        {
            // "开始游戏" opens the setup submenu, does not start a match
            ShowMenuSubscreens(true);
        }

        void OnSetupBackClicked()
        {
            ShowMenuSubscreens(false);
        }

        void OnBeginBattleClicked()
        {
            var map = CurrentMap();
            var playerAssign = new SpawnAssignment(Team.Player,
                playerSlotDropdown != null ? playerSlotDropdown.value : 0);
            var enemyAssign = new SpawnAssignment(Team.Enemy,
                enemySlotDropdown != null ? enemySlotDropdown.value : 1);
            GameManager.Instance?.StartNewGame(map, playerAssign, enemyAssign);
        }

        void OnRematchClicked()
        {
            // Rematch reuses the last map + assignment (GameManager keeps them)
            GameManager.Instance?.StartNewGame(
                GameManager.Instance.CurrentMap,
                GameManager.Instance.PlayerAssign,
                GameManager.Instance.EnemyAssign);
        }

        void OnBackToMenuClicked()
        {
            setupScreenActive = false; // next menu entry shows the button stack
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
