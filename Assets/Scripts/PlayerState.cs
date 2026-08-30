using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Per-player state: treasury, bounty, draw cost and hand.
    /// One instance for the human player, one for the AI — identical rules.
    /// </summary>
    public class PlayerState
    {
        public Team Team { get; }
        public int Treasury { get; private set; }
        public int PendingBounty { get; private set; }
        public int CurrentDrawCost { get; private set; }
        public List<CardData> Hand { get; } = new List<CardData>();

        GameManager gm;

        public PlayerState(Team team, GameManager gameManager)
        {
            Team = team;
            gm = gameManager;
            Treasury = GameConfig.StartingTreasury;
            PendingBounty = 0;
            CurrentDrawCost = GameConfig.InitialDrawCost;
        }

        public void ResetDrawCost()
        {
            CurrentDrawCost = GameConfig.InitialDrawCost;
        }

        /// <summary>
        /// End-of-planning settlement: income + bounty - upkeep for THIS player.
        /// Negative treasury triggers desertion in own regiments, exactly like
        /// the player's bankruptcy rule.
        /// </summary>
        public void SettleEconomy(int aliveRegiments)
        {
            int upkeep = aliveRegiments * GameConfig.UpkeepPerRegiment;
            int net = GameConfig.BaseIncome + PendingBounty - upkeep;
            Treasury += net;

            if (Treasury < 0)
            {
                int deficit = Mathf.Abs(Treasury);
                foreach (var reg in gm.RegimentsOf(Team))
                {
                    reg.ModifySoldiers(-deficit);
                }
                Treasury = 0;
            }

            PendingBounty = 0;
        }

        public void AddBounty(int amount)
        {
            PendingBounty += amount;
        }

        public bool CanAfford(int amount) => Treasury >= amount;

        public bool Spend(int amount)
        {
            if (Treasury < amount) return false;
            Treasury -= amount;
            return true;
        }

        public void Refund(int amount)
        {
            Treasury += amount;
        }

        /// <summary>
        /// Draw a card into hand at the current scaling cost. Same rule as
        /// the player's draw button.
        /// </summary>
        public bool DrawCard()
        {
            if (gm.CurrentPhase != GamePhase.Planning) return false;
            if (!Spend(CurrentDrawCost)) return false;

            CardData template = gm.PickRandomCard();
            if (template == null) return false;

            Hand.Add(template.CreateRuntimeCopy());
            CurrentDrawCost += GameConfig.DrawCostIncrement;
            return true;
        }

        /// <summary>
        /// Deploy a unit card at a position. Player path goes through
        /// CardManager for UI sync; AI calls this directly.
        /// </summary>
        public bool PlayUnitCard(CardData card, Vector3 position)
        {
            if (card == null || card.cardType != CardType.Unit) return false;
            if (gm.CurrentPhase != GamePhase.Planning) return false;
            if (!Spend(card.playCost)) return false;

            gm.SpawnRegiment(position, Team, card.prefabIndex);
            Hand.Remove(card);
            return true;
        }

        /// <summary>
        /// Cast a spell card on the best valid target (own side).
        /// </summary>
        public bool PlaySpellCard(CardData card)
        {
            if (card == null || card.cardType != CardType.Spell) return false;
            if (gm.CurrentPhase != GamePhase.Planning) return false;

            Regiment target = FindBestSpellTarget(card);
            if (target == null) return false;
            if (!Spend(card.playCost)) return false;

            if (card.healAmount > 0) target.ModifySoldiers(card.healAmount);
            if (card.damageAmount > 0) target.DamageAllSoldiers(card.damageAmount);
            if (card.attackBuff != 0f || card.hpBuff != 0f || card.speedBuff != 0f)
                target.ApplyBuff(card.attackBuff, card.hpBuff, card.speedBuff);

            Hand.Remove(card);
            return true;
        }

        Regiment FindBestSpellTarget(CardData card)
        {
            Regiment best = null;
            int bestScore = int.MinValue;
            foreach (var reg in gm.RegimentsOf(Team))
            {
                int score;
                if (card.healAmount > 0)
                {
                    // Heal: most wounded regiment
                    score = GameConfig.RegimentSize - reg.AliveCount;
                }
                else if (card.damageAmount > 0)
                {
                    // Damage spell: hit the strongest ENEMY — but spells target
                    // own side in the original design, so skip if no sense
                    return null;
                }
                else
                {
                    // Buff: strongest regiment
                    score = reg.AliveCount;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = reg;
                }
            }
            return best;
        }
    }
}
