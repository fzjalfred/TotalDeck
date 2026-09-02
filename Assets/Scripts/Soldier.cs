using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Individual soldier unit. Moves independently toward formation position,
    /// handles melee combat via OverlapSphere against enemy soldiers.
    /// </summary>
    public class Soldier : MonoBehaviour
    {
        public Team Team { get; private set; }
        public Regiment ParentRegiment { get; private set; }

        private float hp = GameConfig.SoldierHP;
        private float maxHP = GameConfig.SoldierHP;
        private float attack = GameConfig.SoldierAttack;
        private float attackCooldown = 0f;

        // Individual movement — soldiers run faster than the regiment anchor
        // so they can catch up to their formation slot while it advances
        private Vector3 formationTarget;
        private float moveSpeed = GameConfig.RegimentSpeed * 1.8f;
        private bool hasFormationTarget = false;

        // Fighting while marching slows the soldier down (TW-style combat pace)
        const float FightMoveMultiplier = 0.55f;
        // Auto-engage chase window: if no melee contact within this time, give
        // up the chase and return to the formation slot
        const float ChaseGiveUpTime = 2f;

        // Melee engagement — only attack orders let a soldier chase;
        // marching soldiers trade blows on the move instead (TW-style)
        private Soldier engagedEnemy;
        // Auto-engage chase timer: if no contact within this window, give up
        private float chaseTimer;

        // Visual
        private Renderer soldierRenderer;
        private MaterialPropertyBlock propBlock;

        // Shared non-alloc overlap buffer (single-threaded Update only)
        static readonly Collider[] overlapBuffer = new Collider[128];

        void Awake()
        {
            soldierRenderer = GetComponent<Renderer>();
            if (soldierRenderer == null)
                soldierRenderer = GetComponentInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Initialize the soldier with its parent regiment and team.
        /// </summary>
        public void Initialize(Regiment parent, Vector3 worldPosition, Team team)
        {
            ParentRegiment = parent;
            Team = team;
            hp = maxHP;
            attackCooldown = 0f;

            transform.position = worldPosition;
            formationTarget = worldPosition;
            hasFormationTarget = true;

            SetTeamColor();
        }

        void SetTeamColor()
        {
            if (soldierRenderer == null) return;
            soldierRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", Team == Team.Player
                ? new Color(0.3f, 0.67f, 0.97f)  // Blue
                : new Color(1f, 0.42f, 0.42f));   // Red
            soldierRenderer.SetPropertyBlock(propBlock);
        }

        public void SetHighlight(bool highlighted)
        {
            if (soldierRenderer == null) return;
            soldierRenderer.GetPropertyBlock(propBlock);
            Color baseColor = Team == Team.Player
                ? new Color(0.3f, 0.67f, 0.97f)
                : new Color(1f, 0.42f, 0.42f);
            propBlock.SetColor("_Color", highlighted
                ? Color.Lerp(baseColor, new Color(0f, 1f, 0.67f), 0.6f)
                : baseColor);
            soldierRenderer.SetPropertyBlock(propBlock);
        }

        /// <summary>
        /// Set combat stats (called during init by Regiment).
        /// </summary>
        public void SetStats(float maxHp, float atk)
        {
            maxHP = maxHp;
            hp = maxHp;
            attack = atk;
        }

        public void ModifyAttack(float delta)
        {
            attack += delta;
        }

        public void ModifyMaxHP(float delta)
        {
            maxHP += delta;
            hp += delta;
        }

        /// <summary>
        /// Set the world-space position this soldier should move toward.
        /// </summary>
        public void SetFormationTarget(Vector3 worldPos)
        {
            formationTarget = worldPos;
            hasFormationTarget = true;
        }

        void Update()
        {
            if (!gameObject.activeSelf) return;
            if (ParentRegiment == null) return;

            if (GameManager.Instance.CurrentPhase == GamePhase.Combat)
            {
                // Tick attack cooldown
                if (attackCooldown > 0f)
                {
                    attackCooldown -= Time.deltaTime;
                }

                // Drop cached enemy if it died
                if (engagedEnemy != null && !engagedEnemy.gameObject.activeSelf)
                    SetEngaged(null);

                // Auto-engage behavior:
                // - Released regiment (melee blob): fight freely — chase
                //   anything in reach, no timeout, never walk back to slots
                // - Attack order: charge any foe within engage radius (front rank
                //   dives in; back ranks follow the advancing formation slots)
                // - Idle regiment: seek foes within engage radius, break off to fight
                // - March order: hold the column, fight on the move instead
                bool released = ParentRegiment.MeleeReleased;
                bool chase = released || ParentRegiment.IsAttacking || !ParentRegiment.IsMoving;
                if (engagedEnemy == null && chase)
                {
                    Soldier found = FindNearestEnemy(GameConfig.EngageRadius);
                    // Released blobs must not stretch the fight away from the
                    // regiment — skip targets beyond the anchor leash
                    if (found != null && released && IsBeyondLeash(found.transform.position))
                        found = null;
                    if (found != null)
                    {
                        SetEngaged(found);
                        chaseTimer = ChaseGiveUpTime;
                    }
                }
                else if (engagedEnemy != null && !ParentRegiment.IsAttacking && !released && ParentRegiment.IsMoving)
                {
                    // A move order arrived mid-fight: drop the chase, keep swinging
                    SetEngaged(null);
                }

                if (engagedEnemy != null)
                {
                    // Non-attack-order chases time out: no contact within the
                    // window means the foe got away — fall back into line.
                    // Released soldiers chase indefinitely (melee blob) but
                    // never beyond the anchor leash.
                    if (!ParentRegiment.IsAttacking && !released)
                    {
                        chaseTimer -= Time.deltaTime;
                        if (chaseTimer <= 0f || !IsInCombatWith(engagedEnemy))
                        {
                            SetEngaged(null);
                        }
                    }
                    else if (released && IsBeyondLeash(engagedEnemy.transform.position))
                    {
                        // Target dragged the chase off the leash — release it
                        SetEngaged(null);
                    }

                    if (engagedEnemy != null)
                        EngageEnemy(engagedEnemy);
                }

                if (engagedEnemy == null)
                {
                    if (released)
                    {
                        // Blob mode with nothing in melee reach: drift toward
                        // the nearest enemy regiment's center (separation
                        // steering routes around friends), leashed to the
                        // anchor. Keeps the brawl pooled around the fight.
                        SwingAtContact();
                        var foeReg = ParentRegiment.FindNearestEnemyRegiment();
                        if (foeReg != null && !IsBeyondLeash(foeReg.transform.position))
                        {
                            Vector3 toFoe = foeReg.transform.position - transform.position;
                            toFoe.y = 0f;
                            if (toFoe.magnitude > GameConfig.AttackRange * 0.8f)
                            {
                                Vector3 step = toFoe.normalized * (moveSpeed * 0.7f * Time.deltaTime);
                                step += ComputeSeparation() * moveSpeed * Time.deltaTime;
                                transform.position += step;
                            }
                        }
                    }
                    else if (hasFormationTarget)
                    {
                        // Follow the formation (which advances toward the enemy on
                        // attack orders) AND swing at anyone in contact along the way
                        fightingMove = SwingAtContact();
                        MoveTowardFormation(fightingMove ? FightMoveMultiplier : 1f);
                    }
                }

                // Hard collision AFTER all movement: friend or foe, no two
                // soldiers may occupy the same spot
                ResolveHardCollisions();
            }
            else
            {
                // Planning phase: the battlefield is paused — soldiers hold
                // their exact positions from the combat scramble instead of
                // walking back into formation. Movement resumes next Combat.
                SetEngaged(null);
            }
        }

        // Local flag for the current frame's fight-move state
        bool fightingMove;

        void MoveTowardFormation(float speedMultiplier = 1f)
        {
            Vector3 toTarget = formationTarget - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > 0.05f)
            {
                // Face movement direction
                if (toTarget != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * 360f);
                }

                float step = moveSpeed * speedMultiplier * Time.deltaTime;
                if (step > dist) step = dist;
                Vector3 move = toTarget.normalized * step;

                // Apply separation: push away from nearby soldiers to avoid overlap
                Vector3 separation = ComputeSeparation();
                move += separation * moveSpeed * speedMultiplier * Time.deltaTime;

                transform.position += move;
            }
        }

        /// <summary>
        /// Simple separation steering: push away from nearby FRIENDLY soldiers
        /// to avoid overlap. Enemies are excluded — spacing against the foe is
        /// the job of melee range, otherwise marching through a crowd crawls.
        /// Colliders live on the child "Model" object, so resolve the Soldier
        /// via GetComponentInParent.
        /// </summary>
        Vector3 ComputeSeparation()
        {
            float separationRadius = GameConfig.SoldierRadius * 2f;
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, overlapBuffer);
            Vector3 push = Vector3.zero;
            int count = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Soldier other = overlapBuffer[i].GetComponentInParent<Soldier>();
                if (other == null || other == this) continue;
                if (other.Team != Team) continue;
                if (!other.gameObject.activeSelf) continue;

                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;
                float d = away.magnitude;
                if (d > 0.001f && d < separationRadius)
                {
                    push += away.normalized * (1f - d / separationRadius);
                    count++;
                }
            }

            if (count > 0)
                push = (push / count).normalized * 0.5f;

            return push;
        }

        /// <summary>
        /// Positional collision solver: push self out of ANY overlapping
        /// soldier (friend or foe). Runs after movement so overlap exists
        /// for at most one frame. Half-and-half separation keeps both
        /// soldiers' movement stable under mutual resolution.
        /// </summary>
        void ResolveHardCollisions()
        {
            float minDist = GameConfig.SoldierDiameter;
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, minDist, overlapBuffer);

            Vector3 correction = Vector3.zero;
            for (int i = 0; i < hitCount; i++)
            {
                Soldier other = overlapBuffer[i].GetComponentInParent<Soldier>();
                if (other == null || other == this) continue;
                if (!other.gameObject.activeSelf) continue;

                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;
                float d = away.magnitude;
                if (d < 0.0001f)
                {
                    // Perfect overlap: nudge in a stable pseudo-random direction
                    away = new Vector3(Mathf.Sin(transform.position.x * 12.9898f), 0f, Mathf.Cos(transform.position.z * 78.233f));
                    d = 0.0001f;
                }
                if (d < minDist)
                {
                    float push = (minDist - d) * 0.5f; // half each; the other resolves too
                    correction += away / d * push;
                }
            }

            if (correction != Vector3.zero)
                transform.position += correction;
        }

        /// <summary>
        /// Nearest enemy soldier within radius. Simple proximity targeting —
        /// no fight-slot dedup; soldiers independently pick the closest foe.
        /// Colliders live on the child "Model" object, so resolve the Soldier
        /// via GetComponentInParent.
        /// </summary>
        Soldier FindNearestEnemy(float radius)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer);
            Soldier closest = null;
            float minSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Soldier enemy = overlapBuffer[i].GetComponentInParent<Soldier>();
                if (enemy == null) continue;
                if (enemy.Team == Team) continue;
                if (!enemy.gameObject.activeSelf) continue;

                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    closest = enemy;
                }
            }
            return closest;
        }

        /// <summary>
        /// Charge and melee an engaged enemy. Closes distance, faces the target,
        /// and swings whenever the cooldown allows. Chase duration is handled
        /// by the chaseTimer in Update — attack orders chase indefinitely.
        /// </summary>
        void EngageEnemy(Soldier enemy)
        {
            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;

            // Face the enemy
            if (toEnemy.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toEnemy, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * 540f);
            }

            // Close in until inside attack range
            if (dist > GameConfig.AttackRange * 0.8f)
            {
                float step = moveSpeed * Time.deltaTime;
                if (step > dist) step = dist;
                Vector3 move = toEnemy.normalized * step;
                move += ComputeSeparation() * moveSpeed * Time.deltaTime;
                transform.position += move;
            }

            // Swing when in range and off cooldown
            if (dist <= GameConfig.AttackRange && attackCooldown <= 0f)
            {
                enemy.TakeDamage(attack, this);
                attackCooldown = Random.Range(GameConfig.AttackCooldownMin, GameConfig.AttackCooldownMax);
            }
        }

        public void Disengage()
        {
            // Clear chase state; the soldier keeps fighting on the move,
            // so extraction costs hits but never becomes a punching bag
            SetEngaged(null);
        }

        public bool IsFighting => engagedEnemy != null;

        /// <summary>Assign the current melee target (plain auto-targeting, no registry).</summary>
        void SetEngaged(Soldier enemy) { engagedEnemy = enemy; }

        /// <summary>
        /// True while the chase target is within a generous pursuit envelope —
        /// slightly beyond melee range so brief gaps don't reset the fight.
        /// </summary>
        bool IsInCombatWith(Soldier enemy)
        {
            return enemy != null && enemy.gameObject.activeSelf &&
                   Vector3.Distance(enemy.transform.position, transform.position) <= GameConfig.EngageRadius;
        }

        /// <summary>
        /// Strike the nearest enemy in contact WITHOUT stopping — used while
        /// marching so movement never disables combat (Total War style).
        /// Returns true when the soldier is trading blows on the move.
        /// </summary>
        bool SwingAtContact()
        {
            Soldier enemy = FindNearestEnemy(GameConfig.AttackRange);
            if (enemy == null) return false;

            // Face the foe even while moving
            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toEnemy, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * 540f);
            }

            if (attackCooldown > 0f) return true;

            enemy.TakeDamage(attack, this);
            attackCooldown = Random.Range(GameConfig.AttackCooldownMin, GameConfig.AttackCooldownMax);
            return true;
        }

        /// <summary>
        /// True when a point lies beyond the melee-blob leash around our
        /// regiment anchor — released soldiers never chase or drift past it.
        /// </summary>
        bool IsBeyondLeash(Vector3 point)
        {
            return ParentRegiment == null ||
                   Vector3.Distance(point, ParentRegiment.transform.position) > GameConfig.MeleeBlobLeash;
        }

        /// <summary>
        /// Take damage. If HP drops to 0, die (SetActive false, not Destroy).
        /// </summary>
        public void TakeDamage(float damage, Soldier attacker = null)
        {
            hp -= damage;
            if (hp <= 0f)
            {
                RecordKillCredit(attacker);
                Die();
            }
        }

        /// <summary>
        /// Credit the killing side and charge the victim's side.
        /// </summary>
        void RecordKillCredit(Soldier attacker)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (attacker != null)
            {
                if (attacker.Team == Team.Player) gm.PlayerStats.AddKill();
                else gm.EnemyStats.AddKill();
            }

            if (Team == Team.Player) gm.PlayerStats.AddLoss();
            else gm.EnemyStats.AddLoss();
        }

        /// <summary>
        /// Kill this soldier: deactivate instead of destroy for performance.
        /// </summary>
        public void Die()
        {
            if (!gameObject.activeSelf) return;
            // Release any fight-slot lock we held — dead soldiers stop
            // ticking Update, so the registry entry would never clear
            SetEngaged(null);
            gameObject.SetActive(false);
            ParentRegiment?.OnSoldierDied(this);
        }

        /// <summary>
        /// Revive a previously killed soldier (used by heal spells).
        /// </summary>
        public void Revive()
        {
            gameObject.SetActive(true);
            hp = maxHP;
            attackCooldown = 0f;
        }

        public float GetHP() => hp;
        public float GetMaxHP() => maxHP;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, GameConfig.AttackRange);
        }
    }
}
