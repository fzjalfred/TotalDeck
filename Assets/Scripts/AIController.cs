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

            // Play priority: Troop > Spell (Mage) > Draw.
            // Exhaust units first, then spells, and only draw when nothing
            // playable remains — keeps the board flooded with regiments.
            bool acted = true;
            int safety = 20; // hard cap against infinite loops
            while (acted && safety-- > 0)
            {
                acted = false;

                // 1) Troop: deploy the strongest affordable unit
                if (TryDeployBestUnit(ai))
                {
                    acted = true;
                    continue;
                }

                // 2) Spell (Mage): heal/buff own most valuable regiment
                if (TryCastSpell(ai))
                {
                    acted = true;
                    continue;
                }

                // 3) Draw: only when nothing playable is left in hand
                if (ai.Hand.Count < 10 && ai.Treasury >= ai.CurrentDrawCost)
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
            // Deploy on the AI's own half of the map, as close to the hill
            // as allowed — derived from the AI's assigned spawn slot direction
            var gm = GameManager.Instance;
            var hill = HillZone.Instance;

            Vector3 spawnPos = gm.SpawnPosOf(Team.Enemy);
            Vector3 hillCenter = hill != null ? hill.center : CurrentHillCenter();

            // Direction from hill toward the AI's spawn = "own half" side
            Vector3 dir = spawnPos - hillCenter;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = new Vector3(0f, 0f, -1f);
            dir.Normalize();

            // Stand between the hill edge and the spawn
            float hillEdge = hill != null ? hill.radius + 2f : 10f;
            float spawnDist = dir.magnitude > 0.001f ? Vector3.Distance(hillCenter, spawnPos) : 20f;
            float dist = Mathf.Clamp(hillEdge, 4f, spawnDist);

            // Spread laterally across the spawn direction
            Vector3 lateral = new Vector3(-dir.z, 0f, dir.x);
            Vector3 pos = hillCenter + dir * dist + lateral * Random.Range(-6f, 6f);
            return pos;
        }

        Vector3 CurrentHillCenter()
        {
            var map = GameManager.Instance.CurrentMap;
            return map != null ? map.hillCenter : Vector3.zero;
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
