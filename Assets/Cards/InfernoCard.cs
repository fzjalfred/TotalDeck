using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Placeholder — area damage spell. Unplayable until implemented.
    /// TODO: damage-all-enemies / AoE effect wiring (spell targeting for
    /// enemy-side targets needs engine support first).
    /// </summary>
    public class InfernoCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget) => false;

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            // Placeholder card - TODO: implement
        }
    }
}
