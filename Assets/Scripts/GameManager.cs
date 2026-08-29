using System;
using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Central game manager singleton. Controls phase timer, economy system,
    /// and the bankruptcy/desertion mechanic.
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

        // ── Phase & Timer ──────────────────────────────────
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Planning;
        public float PhaseTimer { get; private set; }
        public int TurnCount { get; private set; } = 1;

        // ── Economy ────────────────────────────────────────
        public int Treasury { get; private set; } = GameConfig.StartingTreasury;
        public int PendingBounty { get; private set; } = 0;
        public int CurrentDrawCost { get; private set; } = GameConfig.InitialDrawCost;

        // ── Regiment Tracking ──────────────────────────────
        public List<Regiment> AllRegiments { get; } = new List<Regiment>();
        public List<Regiment> PlayerRegiments => AllRegiments.FindAll(r => r.Team == Team.Player && r.AliveCount > 0);
        public List<Regiment> EnemyRegiments => AllRegiments.FindAll(r => r.Team == Team.Enemy && r.AliveCount > 0);

        // ── Events ─────────────────────────────────────────
        public event Action<GamePhase> OnPhaseChanged;
        public event Action OnEconomyChanged;
        public event Action OnTreasuryChanged;

        // ── Economy getters for UI ─────────────────────────
        public int TotalIncome => GameConfig.BaseIncome + PendingBounty;
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
        }

        void Update()
        {
            PhaseTimer -= Time.deltaTime;
            if (PhaseTimer <= 0f)
            {
                SwitchPhase();
            }
        }

        /// <summary>
        /// Switch between Planning and Combat phases, running economy settlement
        /// when entering a new Planning phase.
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
                CurrentDrawCost = GameConfig.InitialDrawCost;
                SpawnEnemyReinforcements();
            }

            OnPhaseChanged?.Invoke(CurrentPhase);
            OnEconomyChanged?.Invoke();
        }

        /// <summary>
        /// Settle economy: income + bounty - upkeep. If treasury goes negative,
        /// trigger bankruptcy and desertion.
        /// </summary>
        void SettleEconomy()
        {
            var playerRegs = PlayerRegiments;
            int upkeep = playerRegs.Count * GameConfig.UpkeepPerRegiment;
            int gross = GameConfig.BaseIncome + PendingBounty;
            int net = gross - upkeep;
            Treasury += net;

            if (Treasury < 0)
            {
                int deficit = Mathf.Abs(Treasury);
                // Desertion: each regiment loses 'deficit' soldiers' worth of HP
                foreach (var reg in playerRegs)
                {
                    reg.ModifySoldiers(-deficit);
                }
                Treasury = 0;
            }

            PendingBounty = 0;
            OnTreasuryChanged?.Invoke();
        }

        /// <summary>
        /// Enemy gets a new regiment every 2 turns.
        /// </summary>
        void SpawnEnemyReinforcements()
        {
            if (TurnCount % 2 == 0 && enemySpawnPoint != null)
            {
                Vector3 pos = enemySpawnPoint.position;
                pos.x += UnityEngine.Random.Range(-3f, 3f);
                SpawnRegiment(pos, Team.Enemy, 0);
            }
        }

        // ── Economy Operations ────────────────────────────

        public bool SpendTreasury(int amount)
        {
            if (Treasury < amount) return false;
            Treasury -= amount;
            OnTreasuryChanged?.Invoke();
            return true;
        }

        public void AddBounty(int amount)
        {
            PendingBounty += amount;
            OnEconomyChanged?.Invoke();
        }

        public void IncrementDrawCost()
        {
            CurrentDrawCost += GameConfig.DrawCostIncrement;
            OnEconomyChanged?.Invoke();
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
        /// Check if a position is in the player's deployment zone (lower half of battlefield).
        /// </summary>
        public bool IsInPlayerZone(Vector3 worldPos)
        {
            // Assuming battlefield is centered at origin on XZ plane, player zone is +Z half
            return worldPos.z > 0f;
        }
    }
}
