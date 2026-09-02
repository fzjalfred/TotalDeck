using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Placeholder — ranged troop. Unplayable until implemented.
    /// TODO: ranged attack behaviour, own regiment prefab and stats.
    /// </summary>
    public class ArcherCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget) => false;

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            // Placeholder card - TODO: implement
        }
    }
}
