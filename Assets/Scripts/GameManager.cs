using System;
using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Central game manager singleton. Controls phase timer and hosts one
    /// PlayerState instance per side — the AI runs the exact same economy
    /// and card rules as the human player.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Regiment Prefabs")]
        public GameObject[] regimentPrefabs;

        [Header("Soldier Prefab")]
        public GameObject soldierPrefab;

        [Header("Battlefield")]
        public Transform playerDeployZone;
        public Transform enemySpawnPoint;

        [Header("Card Deck")]
        public CardData[] cardPool;

        // ── Phase & Timer ──────────────────────────────────
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Planning;
        public float PhaseTimer { get; private set; }
        public int TurnCount { get; private set; } = 1;

        /// <summary>Top-level game flow: menu, in-game, post-game screen.</summary>
        public GameState State { get; private set; } = GameState.MainMenu;

        // ── Per-player state ───────────────────────────────
        public PlayerState Player { get; private set; }
        public PlayerState Enemy { get; private set; }

        // ── Combat statistics ──────────────────────────────
        public SideStats PlayerStats { get; private set; }
        public SideStats EnemyStats { get; private set; }

        /// <summary>Side that won the last game (valid in GameOver state).</summary>
        public Team LastWinner { get; private set; }

        /// <summary>Human player's treasury (UI convenience).</summary>
        public int Treasury => Player.Treasury;
        /// <summary>Human player's current draw cost (UI convenience).</summary>
        public int CurrentDrawCost => Player.CurrentDrawCost;

        // ── Regiment Tracking ──────────────────────────────
        public List<Regiment> AllRegiments { get; } = new List<Regiment>();
        public List<Regiment> PlayerRegiments => RegimentsOf(Team.Player);
        public List<Regiment> EnemyRegiments => RegimentsOf(Team.Enemy);

        public List<Regiment> RegimentsOf(Team team) =>
            AllRegiments.FindAll(r => r.Team == team && r.AliveCount > 0);

        // ── Events ─────────────────────────────────────────
        public event Action<GamePhase> OnPhaseChanged;
        public event Action OnEconomyChanged;
        public event Action<Team> OnGameEnded;

        // ── Economy getters for UI (player side) ───────────
        public int TotalIncome => GameConfig.BaseIncome + Player.PendingBounty;
        public int TotalUpkeep => PlayerRegiments.Count * GameConfig.UpkeepPerRegiment;
        public int NetBalance => TotalIncome - TotalUpkeep;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            PhaseTimer = GameConfig.PlanningTime;
            Player = new PlayerState(Team.Player, this);
            Enemy = new PlayerState(Team.Enemy, this);
            PlayerStats = new SideStats(Team.Player);
            EnemyStats = new SideStats(Team.Enemy);
            Time.timeScale = 0f; // main menu shows first
        }

        void Start()
        {
            // Starting hands: both sides draw the same opening hand
            var cm = CardManager.Instance;
            if (cm != null && cm.startingHand != null)
            {
                foreach (var card in cm.startingHand)
                {
                    if (card == null) continue;
                    Player.Hand.Add(card.CreateRuntimeCopy());
                    Enemy.Hand.Add(card.CreateRuntimeCopy());
                }
            }
            cm?.NotifyHandChanged();
        }

        void Update()
        {
            if (State != GameState.Playing) return; // menu / game over: frozen

            PhaseTimer -= Time.deltaTime;
            if (PhaseTimer <= 0f)
            {
                SwitchPhase();
            }
        }

        // ── Game Flow ──────────────────────────────────────

        /// <summary>
        /// Start a fresh game from the menu: reset economy, stats, scores,
        /// clear the field and redeploy starting regiments.
        /// </summary>
        public void StartNewGame()
        {
            // Wipe any field units (fresh deployment)
            ClearField();

            // Reset per-player state
            Player = new PlayerState(Team.Player, this);
            Enemy = new PlayerState(Team.Enemy, this);
            PlayerStats.Reset();
            EnemyStats.Reset();

            // Reset phases and scores
            CurrentPhase = GamePhase.Planning;
            PhaseTimer = GameConfig.PlanningTime;
            TurnCount = 1;

            if (HillZone.Instance != null)
                HillZone.Instance.ResetScores();

            // Redeploy opening regiments
            DeployStartingRegiments();

            // Refill starting hands
            var cm = CardManager.Instance;
            if (cm != null && cm.startingHand != null)
            {
                foreach (var card in cm.startingHand)
                {
                    if (card == null) continue;
                    Player.Hand.Add(card.CreateRuntimeCopy());
                    Enemy.Hand.Add(card.CreateRuntimeCopy());
                }
            }
            cm?.NotifyHandChanged();

            State = GameState.Playing;
            Time.timeScale = 1f;
            OnPhaseChanged?.Invoke(CurrentPhase);
            OnEconomyChanged?.Invoke();
        }

        /// <summary>
        /// Called by HillZone when a side reaches the winning score.
        /// Freezes the battlefield and shows the results screen.
        /// </summary>
        public void EndGame(Team winner)
        {
            if (State != GameState.Playing) return;
            LastWinner = winner;
            State = GameState.GameOver;
            Time.timeScale = 0f; // freeze battlefield; menus run on unscaled UI
            OnGameEnded?.Invoke(winner);
        }

        /// <summary>
        /// Leave the results screen for the main menu.
        /// </summary>
        public void ReturnToMenu()
        {
            ClearField();
            State = GameState.MainMenu;
            Time.timeScale = 0f;
        }

        void ClearField()
        {
            var all = new List<Regiment>(AllRegiments);
            foreach (var reg in all)
                if (reg != null)
                    Destroy(reg.gameObject);
            AllRegiments.Clear();

            // Safety net: any soldier objects orphaned by deferred destroys
            foreach (var s in FindObjectsOfType<Soldier>())
                Destroy(s.gameObject);
        }

        /// <summary>
        /// Deploy the opening player + enemy regiments at their spawn points.
        /// </summary>
        void DeployStartingRegiments()
        {
            Vector3 playerPos = new Vector3(0f, 0f, 30f);
            Vector3 enemyPos = enemySpawnPoint != null
                ? enemySpawnPoint.position
                : new Vector3(0f, 0f, -30f);
            SpawnRegiment(playerPos, Team.Player, 0);
            SpawnRegiment(enemyPos, Team.Enemy, 0);
        }

        /// <summary>
        /// Switch between Planning and Combat phases. Entering Planning runs
        /// the settlement for BOTH players under identical rules.
        /// </summary>
        public void SwitchPhase()
        {
            if (CurrentPhase == GamePhase.Planning)
            {
                CurrentPhase = GamePhase.Combat;
                PhaseTimer = GameConfig.CombatTime;
            }
            else
            {
                CurrentPhase = GamePhase.Planning;
                PhaseTimer = GameConfig.PlanningTime;
                TurnCount++;
                SettleEconomy();
                Player.ResetDrawCost();
                Enemy.ResetDrawCost();
            }

            OnPhaseChanged?.Invoke(CurrentPhase);
            OnEconomyChanged?.Invoke();
        }

        /// <summary>
        /// Settle both players: income + bounty - upkeep, bankruptcy desertion.
        /// </summary>
        void SettleEconomy()
        {
            Player.SettleEconomy(PlayerRegiments.Count);
            Enemy.SettleEconomy(EnemyRegiments.Count);
            OnEconomyChanged?.Invoke();
        }

        // ── Economy Operations (player-facing, kept for UI) ──

        public bool SpendTreasury(int amount) => Player.Spend(amount);

        public void AddBounty(int amount)
        {
            Player.AddBounty(amount);
            OnEconomyChanged?.Invoke();
        }

        public void AddEnemyBounty(int amount)
        {
            Enemy.AddBounty(amount);
        }

        public void IncrementDrawCost()
        {
            // Player's draw cost now lives in PlayerState; kept as a no-op hook
            // for legacy UI calls. The real increment happens inside DrawCard.
            OnEconomyChanged?.Invoke();
        }

        /// <summary>
        /// Weighted-uniform random pick from the shared card pool.
        /// </summary>
        public CardData PickRandomCard()
        {
            if (cardPool == null || cardPool.Length == 0)
            {
                var cm = CardManager.Instance;
                if (cm == null || cm.cardPool == null || cm.cardPool.Length == 0) return null;
                return cm.cardPool[UnityEngine.Random.Range(0, cm.cardPool.Length)];
            }
            return cardPool[UnityEngine.Random.Range(0, cardPool.Length)];
        }

        // ── Regiment Spawning ─────────────────────────────

        /// <summary>
        /// Spawn a regiment at the given position with the specified prefab index.
        /// </summary>
        public Regiment SpawnRegiment(Vector3 position, Team team, int prefabIndex)
        {
            GameObject prefab = regimentPrefabs != null && prefabIndex < regimentPrefabs.Length && regimentPrefabs[prefabIndex] != null
                ? regimentPrefabs[prefabIndex]
                : new GameObject("RegimentRoot");

            GameObject regObj = Instantiate(prefab, position, Quaternion.identity);
            Regiment reg = regObj.GetComponent<Regiment>();
            if (reg == null)
            {
                reg = regObj.AddComponent<Regiment>();
            }

            reg.Initialize(team, position, soldierPrefab, this);
            AllRegiments.Add(reg);

            return reg;
        }

        /// <summary>
        /// Remove a regiment from tracking when it's fully destroyed.
        /// </summary>
        public void UnregisterRegiment(Regiment reg)
        {
            AllRegiments.Remove(reg);
        }

        /// <summary>
        /// Check if a position is in the given team's deployment half.
        /// Player deploys at +Z; the enemy mirrors at -Z.
        /// </summary>
        public bool IsInDeployZone(Vector3 worldPos, Team team)
        {
            return team == Team.Player ? worldPos.z > 0f : worldPos.z < 0f;
        }
    }
}
