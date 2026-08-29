using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Renders a 2D drag-selection box on screen during combat phase.
    /// Subscribes to RTSInputController drag events.
    /// </summary>
    public class DragSelectionVisual : MonoBehaviour
    {
        public Color boxColor = new Color(0f, 1f, 0.67f, 0.15f);
        public Color borderColor = new Color(0f, 1f, 0.67f, 0.8f);
        public float borderWidth = 1f;

        private Texture2D boxTexture;
        private Texture2D borderTexture;
        private bool isDragging = false;
        private Rect dragRect;

        void Start()
        {
            boxTexture = MakeTexture(boxColor);
            borderTexture = MakeTexture(borderColor);

            if (RTSInputController.Instance != null)
            {
                RTSInputController.Instance.OnDragUpdate += OnDragUpdate;
            }
        }

        void OnDestroy()
        {
            if (RTSInputController.Instance != null)
            {
                RTSInputController.Instance.OnDragUpdate -= OnDragUpdate;
            }
        }

        void OnDragUpdate(Vector3 start, Vector3 end, bool active)
        {
            isDragging = active;
            if (active)
            {
                float minX = Mathf.Min(start.x, end.x);
                float maxX = Mathf.Max(start.x, end.x);
                // Input.mousePosition Y is bottom-up (0=bottom), OnGUI Y is top-down (0=top)
                float minYScreen = Mathf.Min(start.y, end.y);
                float maxYScreen = Mathf.Max(start.y, end.y);
                float minY = Screen.height - maxYScreen;
                float height = maxYScreen - minYScreen;
                dragRect = new Rect(minX, minY, maxX - minX, height);
            }
        }

        void OnGUI()
        {
            if (!isDragging) return;

            // Fill
            GUI.color = boxColor;
            GUI.DrawTexture(dragRect, boxTexture);

            // Border
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(dragRect.x, dragRect.y, dragRect.width, borderWidth), borderTexture);
            GUI.DrawTexture(new Rect(dragRect.x, dragRect.y + dragRect.height - borderWidth, dragRect.width, borderWidth), borderTexture);
            GUI.DrawTexture(new Rect(dragRect.x, dragRect.y, borderWidth, dragRect.height), borderTexture);
            GUI.DrawTexture(new Rect(dragRect.x + dragRect.width - borderWidth, dragRect.y, borderWidth, dragRect.height), borderTexture);

            GUI.color = Color.white;
        }

        Texture2D MakeTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
