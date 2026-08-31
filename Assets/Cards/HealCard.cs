using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Fully implemented — restores soldiers to the most wounded friendly
    /// regiment (+15). Existing behaviour; unchanged from the old Heal card.
    /// </summary>
    public class HealCard : MonoBehaviour
    {
        // Fully implemented. Healing is handled by PlayerState.PlaySpellCard /
        // CardManager.SpendAndCast via CardData.healAmount.
    }
}
