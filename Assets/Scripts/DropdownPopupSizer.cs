using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// Watches uGUI Dropdown popups and forces them to display up to 10
    /// option rows (360px), overriding Tuanjie's cramped sizing that only
    /// fits one row. Popup reparenting to canvas root is handled by
    /// DropdownPopupLifter on the template.
    /// </summary>
    public class DropdownPopupSizer : MonoBehaviour
    {
        const float RowHeight = 36f;
        const int MaxVisibleRows = 10;

        Dropdown dropdown;
        RectTransform lastPopup;
        float fixCooldown;

        void Start()
        {
            dropdown = GetComponent<Dropdown>();
        }

        void Update()
        {
            if (dropdown == null) return;

            fixCooldown -= Time.unscaledDeltaTime;
            if (fixCooldown > 0f) return;

            // Poll for the live "Dropdown List" clone Unity creates on Show()
            var popup = FindPopup();
            if (popup == null) return;

            // Force full height + item spacing
            popup.sizeDelta = new Vector2(popup.sizeDelta.x, MaxVisibleRows * RowHeight);
            var content = popup.Find("Viewport/Content");
            if (content != null)
            {
                var crt = content as RectTransform;
                if (crt != null)
                    crt.sizeDelta = new Vector2(crt.sizeDelta.x, MaxVisibleRows * RowHeight);
            }
            fixCooldown = 0.2f;
        }

        RectTransform FindPopup()
        {
            // The live popup clone is nested under the dropdown root
            // (MapDropdown > Dropdown List) — search recursively
            var found = FindRecursive(transform.root, "Dropdown List");
            return found as RectTransform;
        }

        static Transform FindRecursive(Transform parent, string name)
        {
            foreach (Transform c in parent)
            {
                if (c.name == name) return c;
                var r = FindRecursive(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
