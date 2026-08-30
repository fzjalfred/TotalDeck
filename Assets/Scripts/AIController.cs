using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Enemy AI. Two responsibilities:
    ///  1. Planning phase — spend the treasury every turn: draw cards, cast
    ///     spells, deploy units near the hill. Uses the SAME PlayerState
    ///     rules as the human player (no stat privileges).
    ///  2. Combat phase — idle regiments march toward the hill; free ones
    ///     lock the nearest player regiment that is close to the hill.
    /// Rewritten: state references are re-resolved every tick instead of
    /// cached in Start, so StartNewGame's PlayerState swap can never
    /// orphan the AI's wallet.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("Timing")]
        public float planningThinkInterval = 0.5f;
        public float combatOrderInterval = 2f;

        float planningTimer;
        float combatTimer;
        int lastTurnSeen = -1;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            if (gm.CurrentPhase == GamePhase.Planning)
            {
                planningTimer -= Time.deltaTime;
                if (planningTimer > 0f) return;
                planningTimer = planningThinkInterval;
                RunPlanning(gm);
            }
            else
            {
                combatTimer -= Time.deltaTime;
                if (combatTimer > 0f) return;
                combatTimer = combatOrderInterval;
                RunCombatOrders(gm);
            }
        }

        // ── Planning: empty the treasury ──────────────────

        void RunPlanning(GameManager gm)
        {
            var ai = gm.Enemy; // fresh reference every tick

            // New turn bookkeeping (reset draw cost happens in GameManager)
            if (gm.TurnCount != lastTurnSeen)
                lastTurnSeen = gm.TurnCount;

            // Budget policy: keep some reserve for draws before deploying.
            // Draw while it's cheap relative to what we hold.
            bool acted = true;
            int safety = 20; // hard cap against infinite loops
            while (acted && safety-- > 0)
            {
                acted = false;

                // 1) Draw if we can still afford the next draw AND have
                //    fewer than a full hand
                if (ai.Hand.Count < 6 && ai.Treasury >= ai.CurrentDrawCost)
                {
                    if (ai.DrawCard())
                    {
                        acted = true;
                        continue;
                    }
                }

                // 2) Heal/buff own most valuable regiment if worthwhile
                if (TryCastSpell(ai))
                {
                    acted = true;
                    continue;
                }

                // 3) Deploy the strongest affordable unit — but keep a small
                //    reserve if we can still draw a cheaper card next tick
                if (TryDeployBestUnit(ai))
                {
                    acted = true;
                    continue;
                }

                // 4) Nothing affordable: if we have treasury left, one more
                //    draw attempt (cost resets next turn anyway)
                if (ai.Treasury >= ai.CurrentDrawCost && ai.Hand.Count < 10)
                {
                    if (ai.DrawCard())
                    {
                        acted = true;
                        continue;
                    }
                }
            }
        }

        bool TryCastSpell(PlayerState ai)
        {
            for (int i = ai.Hand.Count - 1; i >= 0; i--)
            {
                var card = ai.Hand[i];
                if (card.cardType != CardType.Spell) continue;
                if (!ai.CanAfford(card.playCost)) continue;
                if (ai.PlaySpellCard(card)) return true;
            }
            return false;
        }

        bool TryDeployBestUnit(PlayerState ai)
        {
            // Prefer the most expensive affordable unit (value per slot)
            CardData best = null;
            foreach (var card in ai.Hand)
            {
                if (card.cardType != CardType.Unit) continue;
                if (!ai.CanAfford(card.playCost)) continue;
                if (best == null || card.playCost > best.playCost)
                    best = card;
            }

            if (best == null) return false;
            return ai.PlayUnitCard(best, PickDeployPosition());
        }

        Vector3 PickDeployPosition()
        {
            // Deploy as close to the hill as the own half allows — the AI
            // should contest the objective, not camp its spawn
            var hill = HillZone.Instance;
            var gm = GameManager.Instance;

            float zEdge;
            if (hill != null)
                zEdge = -Mathf.Max(6f, hill.radius + 2f); // just outside hill, own side
            else
                zEdge = -10f;

            if (gm.enemySpawnPoint != null && gm.enemySpawnPoint.position.z > zEdge)
                zEdge = gm.enemySpawnPoint.position.z;

            return new Vector3(Random.Range(-6f, 6f), 0f, zEdge + Random.Range(0f, 2f));
        }

        // ── Combat: contest the hill ──────────────────────

        void RunCombatOrders(GameManager gm)
        {
            var hill = HillZone.Instance;

            foreach (var reg in gm.EnemyRegiments)
            {
                if (reg == null || reg.IsAttacking || reg.IsMoving) continue;

                // Prefer attacking a player regiment that stands ON the hill;
                // otherwise march into the hill circle
                var target = ClosestPlayerOnHill(gm, hill);
                if (target != null)
                {
                    reg.SetTargetEnemy(target);
                    continue;
                }

                if (hill != null)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    float r = Random.Range(0f, hill.radius * 0.7f);
                    reg.SetMoveTarget(hill.center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
            }
        }

        Regiment ClosestPlayerOnHill(GameManager gm, HillZone hill)
        {
            if (hill == null) return null;
            Regiment closest = null;
            float minDist = float.MaxValue;
            foreach (var reg in gm.PlayerRegiments)
            {
                if (!hill.IsInHill(reg.transform.position)) continue;
                float d = Vector3.Distance(reg.transform.position, transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = reg;
                }
            }
            return closest;
        }
    }
}
