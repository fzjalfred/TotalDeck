using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// ScriptableObject storing card definition data.
    /// Used by CardManager for the card pool and hand management.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardData", menuName = "TotalDeck/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Basic Info")]
        public int cardID;
        public string cardName = "New Card";
        [TextArea(2, 4)]
        public string description = "";
        public Sprite cardIcon;

        [Header("Cost & Type")]
        public int playCost = 60;
        public CardType cardType = CardType.Unit;

        [Header("Unit Card")]
        [Tooltip("Index into GameManager's regiment prefab array. 0 = Infantry.")]
        public int prefabIndex = 0;

        [Header("Spell Card")]
        public int healAmount = 0;
        public float damageAmount = 0f;
        [Tooltip("Stat buffs applied to target regiment's soldiers.")]
        public float attackBuff = 0f;
        public float hpBuff = 0f;
        public float speedBuff = 0f;

        /// <summary>
        /// Runtime copy used for instances in hand so originals are never mutated.
        /// </summary>
        public CardData CreateRuntimeCopy()
        {
            CardData copy = CreateInstance<CardData>();
            copy.cardID = cardID;
            copy.cardName = cardName;
            copy.description = description;
            copy.cardIcon = cardIcon;
            copy.playCost = playCost;
            copy.cardType = cardType;
            copy.prefabIndex = prefabIndex;
            copy.healAmount = healAmount;
            copy.damageAmount = damageAmount;
            copy.attackBuff = attackBuff;
            copy.hpBuff = hpBuff;
            copy.speedBuff = speedBuff;
            return copy;
        }
    }
}
