using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// UI element for a single card in the hand.
    /// Displays cost, name, description, and selection state.
    /// </summary>
    public class CardUIElement : MonoBehaviour
    {
        public Text costText;
        public Text titleText;
        public Text descText;
        public Image cardImage;
        public Image selectionHighlight;
        public GameObject disabledOverlay;

        public void Setup(CardData card, bool selected)
        {
            if (costText != null)
                costText.text = "$" + card.playCost;

            if (titleText != null)
                titleText.text = card.cardName;

            if (descText != null)
                descText.text = card.description;

            if (cardImage != null && card.cardIcon != null)
                cardImage.sprite = card.cardIcon;

            // Selection highlight
            if (selectionHighlight != null)
                selectionHighlight.gameObject.SetActive(selected);

            // Disabled state (can't afford)
            bool affordable = GameManager.Instance != null && GameManager.Instance.Treasury >= card.playCost;
            if (disabledOverlay != null)
                disabledOverlay.SetActive(!affordable);
        }
    }
}
