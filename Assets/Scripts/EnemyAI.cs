using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Simple AI for enemy regiments: auto-targets the nearest
    /// player regiment during combat phase.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        private Regiment regiment;
        private float searchInterval = 1f;
        private float searchTimer = 0f;

        void Start()
        {
            regiment = GetComponent<Regiment>();
            if (regiment == null)
            {
                regiment = gameObject.AddComponent<Regiment>();
            }
        }

        void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentPhase != GamePhase.Combat) return;
            if (regiment == null || regiment.AliveCount == 0) return;

            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                FindNearestEnemy();
                searchTimer = searchInterval;
            }
        }

        void FindNearestEnemy()
        {
            // If already has a target and it's alive, keep it
            // (Regiment handles clearing dead targets internally)

            var playerRegs = GameManager.Instance.PlayerRegiments;
            if (playerRegs.Count == 0) return;

            Regiment nearest = null;
            float minDist = float.MaxValue;
            Vector3 myPos = transform.position;

            foreach (var pr in playerRegs)
            {
                float dist = Vector3.Distance(myPos, pr.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = pr;
                }
            }

            if (nearest != null)
            {
                regiment.SetTargetEnemy(nearest);
            }
        }
    }
}
