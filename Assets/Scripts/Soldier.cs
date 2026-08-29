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

        // Melee engagement
        private Soldier engagedEnemy;

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
                    engagedEnemy = null;

                // Seek an enemy within engagement radius
                if (engagedEnemy == null)
                    engagedEnemy = FindNearestEnemy(GameConfig.EngageRadius);

                if (engagedEnemy != null)
                {
                    EngageEnemy(engagedEnemy);
                }
                else if (hasFormationTarget)
                {
                    // No enemies nearby: resume formation
                    MoveTowardFormation();
                }
            }
            else
            {
                engagedEnemy = null;
                if (hasFormationTarget)
                {
                    MoveTowardFormation();
                }
            }
        }

        void MoveTowardFormation()
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

                float step = moveSpeed * Time.deltaTime;
                if (step > dist) step = dist;
                Vector3 move = toTarget.normalized * step;

                // Apply separation: push away from nearby soldiers to avoid overlap
                Vector3 separation = ComputeSeparation();
                move += separation * moveSpeed * Time.deltaTime;

                transform.position += move;
            }
        }

        /// <summary>
        /// Simple separation steering: push away from nearby overlapping soldiers.
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
        /// Nearest enemy soldier within radius. Colliders live on the child
        /// "Model" object, so resolve the Soldier via GetComponentInParent.
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
        /// and swings whenever the cooldown allows.
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
                enemy.TakeDamage(attack);
                attackCooldown = Random.Range(GameConfig.AttackCooldownMin, GameConfig.AttackCooldownMax);
            }
        }

        public void Disengage()
        {
            engagedEnemy = null;
        }

        /// <summary>
        /// Take damage. If HP drops to 0, die (SetActive false, not Destroy).
        /// </summary>
        public void TakeDamage(float damage)
        {
            hp -= damage;
            if (hp <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Kill this soldier: deactivate instead of destroy for performance.
        /// </summary>
        public void Die()
        {
            if (!gameObject.activeSelf) return;
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
