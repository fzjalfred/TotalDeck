using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Fully implemented — restores soldiers to a friendly regiment. Target:
    /// the regiment the player clicked, otherwise the most wounded friendly
    /// regiment (AI auto-target). Amount comes from CardData.healAmount.
    /// </summary>
    public class HealCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
            => clickedTarget != null || FindMostWounded(caster) != null;

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            var target = clickedTarget != null ? clickedTarget : FindMostWounded(caster);
            if (target != null && card.healAmount > 0)
                target.ModifySoldiers(card.healAmount);
        }

        static Regiment FindMostWounded(PlayerState caster)
        {
            Regiment best = null;
            int bestMissing = int.MinValue;
            foreach (var reg in caster.FriendlyRegiments)
            {
                int missing = GameConfig.RegimentSize - reg.AliveCount;
                if (missing > bestMissing)
                {
                    bestMissing = missing;
                    best = reg;
                }
            }
            return best;
        }
    }
}
