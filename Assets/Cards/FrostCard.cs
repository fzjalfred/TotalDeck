using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Placeholder — slow/freeze debuff spell. Unplayable until implemented.
    /// TODO: movement speed debuff application to enemy regiments (needs a
    /// regiment-level slow modifier in Regiment/ApplyBuff first).
    /// </summary>
    public class FrostCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget) => false;

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            // Placeholder card - TODO: implement
        }
    }
}
