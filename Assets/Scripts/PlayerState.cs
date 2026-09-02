using System.Collections.Generic;
using UnityEngine;
using TotalDeck.Cards;

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
        /// Strategy-pattern entry point for playing any card. The engine never
        /// knows WHICH card it is settling — it resolves the card's effect
        /// (CardEffectResolver), lets it gate legality (CanPlay), pays, then
        /// applies it (Execute). Deploy-zone and click-target context come
        /// from the caller (CardManager for the human, AIController for AI).
        /// </summary>
        public bool PlayCard(CardData card, Vector3 position, Regiment clickedTarget = null)
        {
            if (card == null) return false;
            if (gm.CurrentPhase != GamePhase.Planning) return false;
            if (!CanAfford(card.playCost)) return false;

            var effect = CardEffectResolver.Resolve(card);
            if (effect == null || !effect.CanPlay(card, this, position, clickedTarget))
                return false;

            if (!Spend(card.playCost)) return false;
            effect.Execute(card, this, position, clickedTarget);
            Hand.Remove(card);
            return true;
        }

        /// <summary>Spawn a regiment for the card currently being resolved (unit cards).</summary>
        public Regiment DeployRegiment(Vector3 position, int prefabIndex)
            => gm.SpawnRegiment(position, Team, prefabIndex);

        /// <summary>This player's living regiments (spell targeting helper for card effects).</summary>
        public List<Regiment> FriendlyRegiments => gm.RegimentsOf(Team);
    }
}
