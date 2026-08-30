using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Runs the AI player during the Planning phase: draws cards and spends
    /// the entire treasury every turn, mirroring the human player's exact
    /// economy rules (same PlayerState class, same costs).
    /// </summary>
    public class AIController : MonoBehaviour
    {
        PlayerState ai;
        float thinkInterval = 0.5f;
        float thinkTimer = 0f;

        void Start()
        {
            ai = GameManager.Instance.Enemy;
        }

        void Update()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Planning) return;
            if (ai == null) return;

            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = thinkInterval;

            Think();
        }

        /// <summary>
        /// Spend down the treasury: draw first, then deploy/cast everything
        /// affordable. Runs every think tick so it reacts to new draws.
        /// </summary>
        void Think()
        {
            bool acted = true;
            while (acted)
            {
                acted = false;

                // 1. Keep drawing while affordable — build hand options
                if (ai.Treasury >= ai.CurrentDrawCost && ai.Hand.Count < 10)
                {
                    ai.DrawCard();
                    acted = true;
                    continue;
                }

                // 2. Cast spells on worthwhile targets
                for (int i = ai.Hand.Count - 1; i >= 0; i--)
                {
                    var card = ai.Hand[i];
                    if (card.cardType == CardType.Spell && ai.CanAfford(card.playCost))
                    {
                        if (ai.PlaySpellCard(card))
                        {
                            acted = true;
                            break;
                        }
                    }
                }
                if (acted) continue;

                // 3. Deploy the most expensive affordable unit in own half
                CardData bestUnit = null;
                foreach (var card in ai.Hand)
                {
                    if (card.cardType != CardType.Unit) continue;
                    if (!ai.CanAfford(card.playCost)) continue;
                    if (bestUnit == null || card.playCost > bestUnit.playCost)
                        bestUnit = card;
                }

                if (bestUnit != null)
                {
                    Vector3 pos = PickDeployPosition();
                    if (ai.PlayUnitCard(bestUnit, pos))
                        acted = true;
                }
            }
        }

        /// <summary>
        /// Deploy position: own half (-Z), behind the front line, spread on X.
        /// </summary>
        Vector3 PickDeployPosition()
        {
            var gm = GameManager.Instance;
            float z = gm.enemySpawnPoint != null
                ? gm.enemySpawnPoint.position.z + Random.Range(2f, 6f)
                : -Random.Range(8f, 14f);
            float x = Random.Range(-6f, 6f);
            return new Vector3(x, 0f, z);
        }
    }
}
