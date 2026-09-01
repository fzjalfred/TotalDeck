using UnityEngine;
using UnityEngine.EventSystems;

namespace TotalDeck
{
    /// <summary>
    /// Hover-to-reveal helper for the Balance text. Lives on the Text object;
    /// shows the assigned detail panel while the pointer is over this element.
    /// Interface callbacks survive scene rebuilds (unlike EventTrigger
    /// delegates added at build time, which are lost on domain reload).
    /// </summary>
    public class BalanceHoverReveal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject detailPanel;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (detailPanel != null) detailPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
        }
    }
}
