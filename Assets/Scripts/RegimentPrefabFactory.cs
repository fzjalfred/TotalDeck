using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Procedurally creates the regiment root prefab at runtime if not assigned.
    /// The regiment root is an empty GameObject with Regiment, RegimentVisual,
    /// and EnemyAI (for enemy regiments) components.
    /// </summary>
    public class RegimentPrefabFactory : MonoBehaviour
    {
        public static GameObject CreateRegimentPrefab()
        {
            GameObject regiment = new GameObject("RegimentPrefab");
            regiment.AddComponent<Regiment>();
            regiment.AddComponent<RegimentVisual>();
            return regiment;
        }

        public static GameObject CreateEnemyRegimentPrefab()
        {
            GameObject regiment = new GameObject("EnemyRegimentPrefab");
            regiment.AddComponent<Regiment>();
            regiment.AddComponent<RegimentVisual>();
            regiment.AddComponent<EnemyAI>();
            return regiment;
        }
    }
}
