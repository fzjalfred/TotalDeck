using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Placeholder — melee cavalry troop. Unplayable until implemented.
    /// TODO: charge behaviour and stat overrides.
    /// </summary>
    public class KnightCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget) => false;

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            // Placeholder card - TODO: implement
        }
    }
}
