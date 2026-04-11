using UnityEngine;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Phát hiện khi người dùng tiếp cận tủ PCCC trong bán kính cho phép.
    /// Gắn script này lên GameObject đại diện cho Tủ PCCC, thêm SphereCollider (IsTrigger=true).
    /// Thêm tag "Player" hoặc "XRRig" vào XR Origin để trigger hoạt động.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PCCCCabinetZone : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Cài Đặt Vùng Tiếp Cận")]
        [Tooltip("Bán kính vùng phát hiện người dùng tiếp cận tủ PCCC (m).")]
        [SerializeField] float m_DetectionRadius = 1.5f;

        [Tooltip("Tag của XR Origin / Player Rig để nhận diện người dùng.")]
        [SerializeField] string m_PlayerTag = "Player";

        [Tooltip("Cho phép sử dụng Camera.main thay cho player tag nếu tag không có.")]
        [SerializeField] bool m_UseCameraFallback = true;

        [Header("Tham Chiếu")]
        [Tooltip("ScenarioManager để thông báo khi người dùng đến gần.")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Tooltip("Nếu có, kích hoạt UI hướng dẫn mở tủ khi player đến gần.")]
        [SerializeField] GameObject m_OpenCabinetPrompt;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        SphereCollider m_Collider;
        bool           m_PlayerHasApproached;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_Collider              = GetComponent<SphereCollider>();
            m_Collider.isTrigger    = true;
            m_Collider.radius       = m_DetectionRadius;
        }

        void Start()
        {
            if (m_OpenCabinetPrompt != null)
                m_OpenCabinetPrompt.SetActive(false);
        }

        void Update()
        {
            // Fallback: kiểm tra Camera.main mỗi frame nếu không dùng Physics trigger
            if (!m_UseCameraFallback || m_PlayerHasApproached) return;
            if (Camera.main == null) return;

            // Chỉ check XZ-plane (không tính chiều cao cửa tủ)
            Vector3 cameraXZ = Camera.main.transform.position;
            cameraXZ.y       = transform.position.y;
            float dist       = Vector3.Distance(cameraXZ, transform.position);

            if (dist <= m_DetectionRadius)
                NotifyApproach();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Physics Trigger (dự phòng)
        // ──────────────────────────────────────────────────────────────────── //

        void OnTriggerEnter(Collider other)
        {
            if (m_PlayerHasApproached) return;

            if (other.CompareTag(m_PlayerTag))
                NotifyApproach();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal Helpers
        // ──────────────────────────────────────────────────────────────────── //

        void NotifyApproach()
        {
            m_PlayerHasApproached = true;

            if (m_OpenCabinetPrompt != null)
                m_OpenCabinetPrompt.SetActive(true);

            m_Manager?.OnPlayerApproachCabinet();

            Debug.Log("[CabinetZone] 📦 Người dùng đã tiếp cận tủ PCCC.");
        }

        /// <summary>Đặt lại để có thể phát hiện lại (khi reset kịch bản).</summary>
        public void Reset()
        {
            m_PlayerHasApproached = false;

            if (m_OpenCabinetPrompt != null)
                m_OpenCabinetPrompt.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Gizmos
        // ──────────────────────────────────────────────────────────────────── //

        void OnDrawGizmosSelected()
        {
            Gizmos.color = m_PlayerHasApproached
                ? new Color(0f, 1f, 0f, 0.3f)
                : new Color(0f, 0.8f, 1f, 0.3f);

            Gizmos.DrawSphere(transform.position, m_DetectionRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, m_DetectionRadius);
        }
    }
}
