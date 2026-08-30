using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Per-regiment combat brain for AI-owned regiments: auto-targets the
    /// nearest player regiment during combat. Economy and card spending
    /// live in AIController.
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
