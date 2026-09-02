using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Fully implemented — deploys the 50-man footman regiment at the clicked
    /// deploy-zone position. Deploy-zone validation lives in the player input
    /// path (CardManager); the AI picks its own legal spot.
    /// </summary>
    public class FootmanCard : ICardEffect
    {
        public bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
            => card.prefabIndex >= 0; // nothing card-specific to gate yet

        public void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget)
        {
            caster.DeployRegiment(position, card.prefabIndex);
        }
    }
}
