using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Bootstrapper that ensures all required prefabs are created before
    /// the BattleInitializer runs. Assigns them to GameManager.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Manual Prefab Assignments (optional)")]
        public GameObject soldierPrefab;
        public GameObject[] regimentPrefabs;

        void Awake()
        {
            EnsurePrefabs();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.soldierPrefab = soldierPrefab;
                GameManager.Instance.regimentPrefabs = regimentPrefabs;
            }
        }

        void EnsurePrefabs()
        {
            if (soldierPrefab == null)
            {
                soldierPrefab = SoldierPrefabFactory.CreateSoldierPrefab();
            }

            if (regimentPrefabs == null || regimentPrefabs.Length == 0)
            {
                regimentPrefabs = new GameObject[1];
                regimentPrefabs[0] = RegimentPrefabFactory.CreateRegimentPrefab();
            }
        }
    }
}
