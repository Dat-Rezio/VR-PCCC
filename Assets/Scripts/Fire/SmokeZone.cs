using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Fire
{
    /// <summary>
    /// Vùng khói trong scene PCCC.
    /// Khi đầu người chơi lọt vào collider này → thông báo cho SmokeVisionEffect.
    /// Gắn lên một GameObject có BoxCollider/SphereCollider (Is Trigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SmokeZone : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector Fields
        // ------------------------------------------------------------------ //

        [Header("Cài Đặt Khói")]
        [Tooltip("Mức độ khói dày đặc (0 = không khói, 1 = dày đặc nhất).")]
        [Range(0f, 1f)]
        [SerializeField] float m_SmokeDensity = 0.8f;

        [Tooltip("Chiều cao an toàn bên trong zone này (m tính từ đáy zone). Dưới mức này không bị khói.")]
        [SerializeField] float m_SafeHeightFromBottom = 0.8f;

        [Tooltip("Particle System cho khói. Nếu có, sẽ bật/tắt tự động.")]
        [SerializeField] ParticleSystem m_SmokeParticles;

        [Tooltip("Bật particle khi scene Start.")]
        [SerializeField] bool m_PlayOnStart = true;

        [Header("Events")]
        [Tooltip("Khi đầu người chơi vào vùng khói nguy hiểm.")]
        public UnityEvent<float> OnPlayerEnterSmoke;  // density
        [Tooltip("Khi đầu người chơi thoát khỏi vùng khói (cúi thấp hoặc ra khỏi zone).")]
        public UnityEvent OnPlayerExitSmoke;

        // ------------------------------------------------------------------ //
        //  Runtime State
        // ------------------------------------------------------------------ //

        Collider m_Collider;
        Transform m_PlayerCamera;
        bool m_PlayerInsideZone;
        bool m_PlayerInDanger;  // trong zone VÀ đầu cao hơn safe height

        /// <summary>Mức độ khói (0-1).</summary>
        public float SmokeDensity => m_SmokeDensity;

        // ------------------------------------------------------------------ //
        //  Unity Lifecycle
        // ------------------------------------------------------------------ //

        void Awake()
        {
            m_Collider = GetComponent<Collider>();
            m_Collider.isTrigger = true;
        }

        void Start()
        {
            if (m_SmokeParticles != null && m_PlayOnStart)
                m_SmokeParticles.Play();
        }

        void Update()
        {
            if (!m_PlayerInsideZone || m_PlayerCamera == null) return;

            // Tính chiều cao của đầu người chơi so với đáy của zone
            float zoneBottom = m_Collider.bounds.min.y;
            float headHeightInZone = m_PlayerCamera.position.y - zoneBottom;

            bool headInDanger = headHeightInZone > m_SafeHeightFromBottom;

            if (headInDanger && !m_PlayerInDanger)
            {
                m_PlayerInDanger = true;
                OnPlayerEnterSmoke?.Invoke(m_SmokeDensity);
                Debug.Log($"[SmokeZone] '{name}': Đầu người chơi trong vùng khói nguy hiểm! (cao {headHeightInZone:F2}m so với đáy zone)");
            }
            else if (!headInDanger && m_PlayerInDanger)
            {
                m_PlayerInDanger = false;
                OnPlayerExitSmoke?.Invoke();
                Debug.Log($"[SmokeZone] '{name}': Người chơi CÚI NGƯỜI né khói thành công!");
            }
        }

        // ------------------------------------------------------------------ //
        //  Trigger Callbacks
        // ------------------------------------------------------------------ //

        void OnTriggerEnter(Collider other)
        {
            // Tìm camera trong object hoặc children (hỗ trợ XR Origin structure)
            var cam = FindCameraInHierarchy(other.gameObject);
            if (cam == null) return;

            m_PlayerCamera = cam;
            m_PlayerInsideZone = true;
            Debug.Log($"[SmokeZone] '{name}': Người chơi vào vùng khói.");
        }

        void OnTriggerExit(Collider other)
        {
            if (m_PlayerCamera == null) return;
            // Chỉ xử lý nếu đúng camera đã vào
            var cam = FindCameraInHierarchy(other.gameObject);
            if (cam != m_PlayerCamera) return;

            m_PlayerInsideZone = false;
            if (m_PlayerInDanger)
            {
                m_PlayerInDanger = false;
                OnPlayerExitSmoke?.Invoke();
            }
            m_PlayerCamera = null;
            Debug.Log($"[SmokeZone] '{name}': Người chơi rời vùng khói.");
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        static Transform FindCameraInHierarchy(GameObject go)
        {
            // Tìm Camera.main trực tiếp
            if (Camera.main != null)
            {
                // Kiểm tra xem go hoặc ancestor của Camera.main có trùng không
                Transform t = Camera.main.transform;
                while (t != null)
                {
                    if (t == go.transform) return Camera.main.transform;
                    t = t.parent;
                }
            }

            // Fallback: tìm Camera component trong children
            var cam = go.GetComponentInChildren<Camera>();
            return cam != null ? cam.transform : null;
        }

        /// <summary>Bật/tắt vùng khói từ code.</summary>
        public void SetSmokeActive(bool active)
        {
            if (m_SmokeParticles != null)
            {
                if (active) m_SmokeParticles.Play();
                else m_SmokeParticles.Stop();
            }
            m_Collider.enabled = active;

            if (!active && m_PlayerInDanger)
            {
                m_PlayerInDanger = false;
                m_PlayerInsideZone = false;
                OnPlayerExitSmoke?.Invoke();
            }
        }

        /// <summary>Cập nhật mật độ khói runtime.</summary>
        public void SetSmokeDensity(float density)
        {
            m_SmokeDensity = Mathf.Clamp01(density);
        }

        // ------------------------------------------------------------------ //
        //  Gizmos
        // ------------------------------------------------------------------ //

        void OnDrawGizmos()
        {
            if (m_Collider == null) m_Collider = GetComponent<Collider>();
            if (m_Collider == null) return;

            var bounds = m_Collider.bounds;

            // Vùng khói (đỏ mờ = nguy hiểm)
            Gizmos.color = new Color(0.8f, 0.1f, 0.0f, 0.15f);
            float dangerBottom = bounds.min.y + m_SafeHeightFromBottom;
            float dangerHeight = bounds.max.y - dangerBottom;
            var dangerCenter = new Vector3(bounds.center.x, dangerBottom + dangerHeight * 0.5f, bounds.center.z);
            Gizmos.DrawCube(dangerCenter, new Vector3(bounds.size.x, dangerHeight, bounds.size.z));

            // Vùng an toàn (xanh = cúi thấp)
            Gizmos.color = new Color(0.0f, 0.8f, 0.2f, 0.15f);
            float safeHeight = m_SafeHeightFromBottom;
            var safeCenter = new Vector3(bounds.center.x, bounds.min.y + safeHeight * 0.5f, bounds.center.z);
            Gizmos.DrawCube(safeCenter, new Vector3(bounds.size.x, safeHeight, bounds.size.z));

            // Đường phân ranh
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            var lineY = bounds.min.y + m_SafeHeightFromBottom;
            Gizmos.DrawLine(new Vector3(bounds.min.x, lineY, bounds.min.z), new Vector3(bounds.max.x, lineY, bounds.min.z));
            Gizmos.DrawLine(new Vector3(bounds.max.x, lineY, bounds.min.z), new Vector3(bounds.max.x, lineY, bounds.max.z));
            Gizmos.DrawLine(new Vector3(bounds.max.x, lineY, bounds.max.z), new Vector3(bounds.min.x, lineY, bounds.max.z));
            Gizmos.DrawLine(new Vector3(bounds.min.x, lineY, bounds.max.z), new Vector3(bounds.min.x, lineY, bounds.min.z));
        }
    }
}
