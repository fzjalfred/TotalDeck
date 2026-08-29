using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Handles all mouse input: drag-selection of player regiments,
    /// right-click move/attack commands, and card deployment clicks.
    /// Supports shift+right-click for queued move commands.
    /// </summary>
    public class RTSInputController : MonoBehaviour
    {
        public static RTSInputController Instance { get; private set; }

        [Header("Layer Masks")]
        public LayerMask groundMask = ~0;
        public LayerMask regimentMask = ~0;

        [Header("Drag Selection")]
        public float dragThreshold = 8f;

        // ── Selection State ───────────────────────────────
        public List<Regiment> SelectedRegiments { get; } = new List<Regiment>();

        // ── Drag State ─────────────────────────────────────
        private bool isDragging = false;
        private Vector3 dragStartScreen;
        private Vector3 dragEndScreen;

        // ── Events ─────────────────────────────────────────
        public event System.Action<List<Regiment>> OnSelectionChanged;
        public event System.Action<Vector3, Vector3, bool> OnDragUpdate; // start, end, active

        private Camera cam;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            cam = Camera.main;
        }

        void Update()
        {
            bool inCombat = GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Combat;

            // Left mouse button down
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftMouseDown();
            }

            // Left mouse button held (dragging)
            if (isDragging)
            {
                dragEndScreen = Input.mousePosition;
                OnDragUpdate?.Invoke(dragStartScreen, dragEndScreen, true);
            }

            // Left mouse button up
            if (Input.GetMouseButtonUp(0))
            {
                HandleLeftMouseUp();
            }

            // Right mouse button (move/attack command) — works in both phases
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }

        void HandleLeftMouseDown()
        {
            // If a card is selected during Planning, try to play it
            if (CardManager.Instance != null && CardManager.Instance.SelectedCard != null &&
                GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Planning)
            {
                Vector3 worldPos;
                if (RaycastGround(out worldPos))
                {
                    CardManager.Instance.PlayCardAt(worldPos);
                }
                return;
            }

            // Allow selection in both Planning and Combat phases
            dragStartScreen = Input.mousePosition;
            dragEndScreen = dragStartScreen;
            isDragging = true;
        }

        void HandleLeftMouseUp()
        {
            if (!isDragging) return;
            isDragging = false;
            OnDragUpdate?.Invoke(dragStartScreen, dragEndScreen, false);

            float dragDist = Vector3.Distance(dragStartScreen, dragEndScreen);

            if (dragDist < dragThreshold)
            {
                // Click selection
                Vector3 worldPos;
                if (RaycastGround(out worldPos))
                {
                    ClickSelect(worldPos);
                }
            }
            else
            {
                // Drag box selection
                DragSelect();
            }
        }

        void ClickSelect(Vector3 worldPos)
        {
            SelectedRegiments.Clear();
            Regiment clicked = FindRegimentAt(worldPos, Team.Player);
            if (clicked != null)
            {
                SelectedRegiments.Add(clicked);
            }
            OnSelectionChanged?.Invoke(new List<Regiment>(SelectedRegiments));
        }

        void DragSelect()
        {
            SelectedRegiments.Clear();

            foreach (var reg in GameManager.Instance.AllRegiments)
            {
                if (reg.Team != Team.Player || reg.AliveCount == 0) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(reg.transform.position);
                if (IsPointInDragRect(screenPos))
                {
                    SelectedRegiments.Add(reg);
                }
            }

            OnSelectionChanged?.Invoke(new List<Regiment>(SelectedRegiments));
        }

        bool IsPointInDragRect(Vector3 point)
        {
            float minX = Mathf.Min(dragStartScreen.x, dragEndScreen.x);
            float maxX = Mathf.Max(dragStartScreen.x, dragEndScreen.x);
            float minY = Mathf.Min(dragStartScreen.y, dragEndScreen.y);
            float maxY = Mathf.Max(dragStartScreen.y, dragEndScreen.y);
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        void HandleRightClick()
        {
            if (SelectedRegiments.Count == 0) return;

            Vector3 worldPos;
            if (!RaycastGround(out worldPos)) return;

            bool queueMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Check if clicking on an enemy regiment
            Regiment enemyTarget = FindRegimentAt(worldPos, Team.Enemy);

            for (int i = 0; i < SelectedRegiments.Count; i++)
            {
                var reg = SelectedRegiments[i];
                if (enemyTarget != null)
                {
                    if (queueMode)
                        reg.QueueMoveTarget(worldPos);
                    else
                        reg.SetTargetEnemy(enemyTarget);
                }
                else
                {
                    // Stagger multiple regiments side by side
                    float offset = (i - (SelectedRegiments.Count - 1) * 0.5f) * 6f;
                    Vector3 movePos = worldPos + new Vector3(offset, 0f, 0f);
                    if (queueMode)
                        reg.QueueMoveTarget(movePos);
                    else
                        reg.SetMoveTarget(movePos);
                }
            }
        }

        /// <summary>
        /// Raycast from mouse position to the ground plane.
        /// </summary>
        bool RaycastGround(out Vector3 worldPos)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            {
                worldPos = hit.point;
                return true;
            }
            // Fallback: intersect with y=0 plane
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0)
            {
                worldPos = ray.origin + ray.direction * t;
                return true;
            }
            worldPos = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Find the closest regiment of the given team within selection radius.
        /// </summary>
        Regiment FindRegimentAt(Vector3 worldPos, Team team)
        {
            Regiment closest = null;
            float minDist = GameConfig.SelectRadius;
            foreach (var reg in GameManager.Instance.AllRegiments)
            {
                if (reg.Team != team || reg.AliveCount == 0) continue;
                float dist = Vector3.Distance(reg.transform.position, worldPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = reg;
                }
            }
            return closest;
        }

        public void ClearSelection()
        {
            SelectedRegiments.Clear();
            OnSelectionChanged?.Invoke(new List<Regiment>(SelectedRegiments));
        }
    }
}
