using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Macro control layer. Manages 50-soldier formation, movement toward
    /// target position, and target enemy regiment tracking.
    /// Supports shift-queued move commands.
    /// </summary>
    public class Regiment : MonoBehaviour
    {
        public Team Team { get; private set; }
        public List<Soldier> Soldiers { get; } = new List<Soldier>();
        public int AliveCount { get; private set; }

        private Vector3 targetPosition;
        private Regiment targetEnemyRegiment;
        private Queue<Vector3> moveQueue = new Queue<Vector3>();
        private List<Vector3> formationOffsets = new List<Vector3>();
        // Set on death; formation compacts on the next UpdateSoldierPositions pass
        private bool formationDirty;
        private float moveSpeed = GameConfig.RegimentSpeed;
        private float currentAngle;

        private GameObject soldierPrefab;
        private GameManager gameManager;
        private bool initialized = false;

        // Buff multipliers
        private float attackBuffMul = 1f;
        private float speedBuffMul = 1f;

        // Public accessors for visualization
        public Vector3 CurrentMoveTarget => targetPosition;
        public bool HasMoveQueue => moveQueue.Count > 0;
        public bool IsAttacking => targetEnemyRegiment != null;

        /// <summary>
        /// True while the regiment has a move order it has not reached yet.
        /// Idle regiments auto-engage nearby foes instead of holding ground.
        /// </summary>
        public bool IsMoving
        {
            get
            {
                if (moveQueue.Count > 0) return true;
                if (targetEnemyRegiment != null) return false; // attack order, not a march
                return Vector3.Distance(transform.position, targetPosition) > 1.5f;
            }
        }

        /// <summary>
        /// True while any alive soldier is melee-locked with an enemy.
        /// </summary>
        public bool IsEngaged
        {
            get
            {
                foreach (var s in Soldiers)
                {
                    if (s == null || !s.gameObject.activeSelf) continue;
                    if (s.IsFighting) return true;
                }
                return false;
            }
        }

        void OnDestroy()
        {
            gameManager?.UnregisterRegiment(this);

            // Soldiers are unparented free-walkers; clean them up with the
            // regiment. Deactivate first so no Update tick runs mid-teardown.
            foreach (var s in Soldiers)
            {
                if (s == null || s.gameObject == null) continue;
                s.gameObject.SetActive(false);
                Destroy(s.gameObject);
            }
        }

        /// <summary>
        /// Initialize the regiment with team, position, and soldier prefab.
        /// </summary>
        public void Initialize(Team team, Vector3 position, GameObject soldierPrefab, GameManager gm)
        {
            this.Team = team;
            this.soldierPrefab = soldierPrefab;
            this.gameManager = gm;
            this.targetPosition = position;
            this.transform.position = position;

            // Face the enemy
            currentAngle = team == Team.Player ? Mathf.PI : 0f;

            SpawnSoldiers(GameConfig.RegimentSize);
            UpdateSoldierPositions();
            initialized = true;
        }

        void SpawnSoldiers(int count)
        {
            // Clear existing
            foreach (var s in Soldiers)
            {
                if (s != null && s.gameObject != null)
                    Destroy(s.gameObject);
            }
            Soldiers.Clear();
            formationOffsets.Clear();
            AliveCount = 0;

            int cols = GameConfig.RegimentCols;
            int rows = Mathf.CeilToInt(count / (float)cols);

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = Mathf.FloorToInt(i / (float)cols);
                Vector3 localOffset = GetFormationOffset(col, row, cols, rows);
                AddSoldier(localOffset);
            }
        }

        /// <summary>
        /// Spawn one soldier at a formation slot. Soldiers are NOT parented to
        /// the regiment root — they walk independently toward their slot,
        /// so the root only acts as an invisible formation anchor.
        /// </summary>
        void AddSoldier(Vector3 localOffset)
        {
            Vector3 worldPos = transform.position + localOffset;

            GameObject soldierObj = Instantiate(soldierPrefab, worldPos, Quaternion.identity);
            Soldier soldier = soldierObj.GetComponent<Soldier>();
            if (soldier == null)
                soldier = soldierObj.AddComponent<Soldier>();

            soldier.Initialize(this, worldPos, Team);
            soldier.SetStats(GameConfig.SoldierHP, GameConfig.SoldierAttack * attackBuffMul);
            Soldiers.Add(soldier);
            formationOffsets.Add(localOffset);
            AliveCount++;
        }

        Vector3 GetFormationOffset(int col, int row, int cols, int rows)
        {
            float x = (col - (cols - 1) * 0.5f) * GameConfig.SoldierSpacing;
            // Front-aligned depth: row 0 sits AT the anchor, deeper rows extend
            // behind it. The front line never shifts when row count changes —
            // gaps are filled from the back, TW-style.
            float z = -row * GameConfig.SoldierSpacing;
            return new Vector3(x, 0f, z);
        }

        void Update()
        {
            if (!initialized) return;
            if (AliveCount == 0) return;

            if (GameManager.Instance.CurrentPhase == GamePhase.Combat)
            {
                HandleCombatMovement();
            }

            UpdateSoldierPositions();
        }

        void HandleCombatMovement()
        {
            // Attack order: follow the enemy, halt on victory
            if (targetEnemyRegiment != null)
            {
                if (targetEnemyRegiment.AliveCount == 0)
                {
                    targetEnemyRegiment = null;
                    targetPosition = transform.position;
                }
                else
                {
                    targetPosition = targetEnemyRegiment.transform.position;
                }
            }

            // Hold the line while any soldier is melee-locked — UNLESS this is
            // an attack order: then the whole regiment keeps pressing forward
            // so back ranks flood into the fight instead of standing idle
            bool holdAnchor = IsEngaged && !IsAttacking;
            if (holdAnchor) return;

            // Attack order with engaged soldiers: stop so the FRONT LINE
            // (the anchor) reaches contact distance of the enemy — the
            // formation body trails behind and never walks through the foe
            float stopDist = IsAttacking ? GameConfig.AttackRange : 1.5f;

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist <= stopDist)
            {
                // Reached destination, dequeue next waypoint if available
                if (!IsAttacking && moveQueue.Count > 0)
                {
                    targetPosition = moveQueue.Dequeue();
                }
            }
            else
            {
                // Face movement direction
                if (toTarget != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }

                float step = moveSpeed * speedBuffMul * Time.deltaTime;
                if (step > dist) step = dist;
                transform.position += toTarget.normalized * step;
            }
        }

        /// <summary>
        /// Feed each soldier its formation slot in world space;
        /// the soldier walks there on its own (Soldier.Update).
        /// On rank gaps (deaths) the formation compacts lazily: the
        /// rebuild only runs when a death flagged it, amortized into
        /// this already-running O(n) loop — zero steady-state cost.
        /// </summary>
        void UpdateSoldierPositions()
        {
            if (formationDirty)
            {
                CompactFormation();
                formationDirty = false;
            }

            for (int i = 0; i < Soldiers.Count && i < formationOffsets.Count; i++)
            {
                var s = Soldiers[i];
                if (s == null || !s.gameObject.activeSelf) continue;
                Vector3 worldOffset = transform.rotation * formationOffsets[i];
                s.SetFormationTarget(transform.position + worldOffset);
            }
        }

        /// <summary>
        /// Close ranks TW-style: alive soldiers are re-assigned to the front
        /// slots of a fresh AliveCount-sized block, preserving their relative
        /// order, so back rows shift forward into gaps left by the dead.
        /// </summary>
        void CompactFormation()
        {
            int cols = GameConfig.RegimentCols;
            int rows = Mathf.CeilToInt(AliveCount / (float)cols);

            int slot = 0;
            for (int i = 0; i < Soldiers.Count && slot < AliveCount; i++)
            {
                var s = Soldiers[i];
                if (s == null || !s.gameObject.activeSelf) continue;
                formationOffsets[i] = GetFormationOffset(slot % cols, slot / cols, cols, rows);
                slot++;
            }
        }

        // ── Public Commands ───────────────────────────────

        public void SetMoveTarget(Vector3 position)
        {
            moveQueue.Clear();
            targetPosition = position;
            targetEnemyRegiment = null;
            DisengageAll();
        }

        public void QueueMoveTarget(Vector3 position)
        {
            moveQueue.Enqueue(position);
            DisengageAll();
        }

        /// <summary>
        /// Break off melee so soldiers obey the latest move command
        /// instead of fighting while the anchor marches away.
        /// </summary>
        public void DisengageAll()
        {
            foreach (var s in Soldiers)
            {
                if (s == null) continue;
                s.Disengage();
            }
        }

        public Vector3[] GetMovePath()
        {
            // If tracking an enemy, show path to the enemy's position
            if (targetEnemyRegiment != null && targetEnemyRegiment.AliveCount > 0)
            {
                return new Vector3[] { transform.position, targetEnemyRegiment.transform.position };
            }

            // If arrived at final destination and no queued waypoints, no path to show
            float distToTarget = Vector3.Distance(transform.position, targetPosition);
            if (moveQueue.Count == 0 && distToTarget <= 1.5f)
                return new Vector3[] { transform.position };

            var path = new List<Vector3> { transform.position };
            path.Add(targetPosition);
            foreach (var pos in moveQueue)
                path.Add(pos);
            return path.ToArray();
        }

        public void SetTargetEnemy(Regiment enemy)
        {
            moveQueue.Clear();
            targetEnemyRegiment = enemy;
        }

        // ── Soldier Modification ──────────────────────────

        /// <summary>
        /// Add or remove soldiers. Positive = heal/recruit, negative = desertion/damage.
        /// </summary>
        public void ModifySoldiers(int amount)
        {
            if (amount > 0)
            {
                // Revive dead soldiers first, then increase count
                int revived = 0;
                for (int i = 0; i < Soldiers.Count && revived < amount; i++)
                {
                    if (!Soldiers[i].gameObject.activeSelf)
                    {
                        Soldiers[i].Revive();
                        revived++;
                        AliveCount++;
                    }
                }

                // If still need more and under cap, respawn
                int stillNeeded = amount - revived;
                int maxNew = GameConfig.RegimentSize - AliveCount;
                int toAdd = Mathf.Min(stillNeeded, maxNew);
                if (toAdd > 0)
                {
                    for (int i = 0; i < toAdd; i++)
                    {
                        int col = AliveCount % GameConfig.RegimentCols;
                        int row = AliveCount / GameConfig.RegimentCols;
                        Vector3 localOffset = GetFormationOffset(col, row, GameConfig.RegimentCols, Mathf.CeilToInt(GameConfig.RegimentSize / (float)GameConfig.RegimentCols));
                        AddSoldier(localOffset);
                    }
                    UpdateSoldierPositions();
                }
            }
            else if (amount < 0)
            {
                int toKill = Mathf.Min(-amount, AliveCount);
                for (int i = 0; i < Soldiers.Count && toKill > 0; i++)
                {
                    if (Soldiers[i].gameObject.activeSelf)
                    {
                        Soldiers[i].Die();
                        toKill--;
                        // AliveCount-- happens in OnSoldierDied (via Die) —
                        // decrementing here too would double-count
                    }
                }
            }
        }

        /// <summary>
        /// Apply damage to all alive soldiers (for spell cards).
        /// </summary>
        public void DamageAllSoldiers(float damage)
        {
            foreach (var s in Soldiers)
            {
                if (s != null && s.gameObject.activeSelf)
                {
                    s.TakeDamage(damage);
                }
            }
        }

        /// <summary>
        /// Apply stat buffs to all soldiers.
        /// </summary>
        public void ApplyBuff(float atkBuff, float hpBuff, float spdBuff)
        {
            if (spdBuff != 0f)
            {
                speedBuffMul += spdBuff;
                moveSpeed *= speedBuffMul;
            }
            foreach (var s in Soldiers)
            {
                if (s != null && s.gameObject.activeSelf)
                {
                    if (atkBuff != 0f) s.ModifyAttack(atkBuff);
                    if (hpBuff != 0f) s.ModifyMaxHP(hpBuff);
                }
            }
        }

        /// <summary>
        /// Called by Soldier when it dies. Handles bounty and cleanup.
        /// The killer's side earns the bounty into its own PlayerState.
        /// </summary>
        public void OnSoldierDied(Soldier soldier)
        {
            AliveCount = Mathf.Max(0, AliveCount - 1);
            formationDirty = true;

            // Bounty goes to the OPPOSITE side of the victim
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (soldier.Team == Team.Enemy)
                    gm.AddBounty(GameConfig.KillBounty);
                else
                    gm.AddEnemyBounty(GameConfig.KillBounty);
            }

            if (AliveCount == 0)
            {
                // Delay destruction to let death effects play
                Destroy(gameObject, 2f);
            }
        }

        public bool IsSelected()
        {
            return RTSInputController.Instance != null &&
                   RTSInputController.Instance.SelectedRegiments.Contains(this);
        }
    }
}
