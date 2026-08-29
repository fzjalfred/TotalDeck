using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Simple RTS-style overhead camera. Looks down at the battlefield
    /// from a fixed angle. Supports optional edge-pan scrolling.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Position")]
        public float cameraHeight = 35f;
        public float cameraAngle = 55f;
        public Vector3 cameraCenter = new Vector3(0f, 0f, 0f);

        [Header("Edge Pan (optional)")]
        public bool enableEdgePan = false;
        public float edgePanSpeed = 20f;
        public float edgePanBorder = 20f;
        public Vector2 panLimitX = new Vector2(-20f, 20f);
        public Vector2 panLimitZ = new Vector2(-20f, 20f);

        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            PositionCamera();
        }

        void PositionCamera()
        {
            float rad = cameraAngle * Mathf.Deg2Rad;
            float horizDist = cameraHeight / Mathf.Tan(rad);
            // Place camera on player side (+Z), looking toward enemy (-Z)
            Vector3 offset = new Vector3(0f, cameraHeight, horizDist);
            transform.position = cameraCenter + offset;
            Vector3 lookDir = (cameraCenter - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }

        void Update()
        {
            if (!enableEdgePan) return;

            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 mousePos = Input.mousePosition;

            if (mousePos.y >= Screen.height - edgePanBorder)
                pos += forward * edgePanSpeed * Time.deltaTime;
            if (mousePos.y <= edgePanBorder)
                pos -= forward * edgePanSpeed * Time.deltaTime;
            if (mousePos.x >= Screen.width - edgePanBorder)
                pos += right * edgePanSpeed * Time.deltaTime;
            if (mousePos.x <= edgePanBorder)
                pos -= right * edgePanSpeed * Time.deltaTime;

            pos.x = Mathf.Clamp(pos.x, cameraCenter.x + panLimitX.x, cameraCenter.x + panLimitX.y);
            pos.z = Mathf.Clamp(pos.z, cameraCenter.z + panLimitZ.x, cameraCenter.z + panLimitZ.y);

            transform.position = pos;
        }
    }
}
