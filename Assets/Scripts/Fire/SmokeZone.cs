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
        bool m_PlayerInDanger;  // camera nằm trong zone VÀ cao hơn safe height

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

            // Tìm camera ngay từ đầu — không cần chờ trigger
            if (Camera.main == null)
                Debug.LogWarning("[SmokeZone] Không tìm thấy Camera.main !");
        }

        void Update()
        {
            if (Camera.main == null || m_Collider == null) return;

            var camPos = Camera.main.transform.position;

            // ── 1. Dùng ClosestPoint để check chính xác camera có trong collider không ──
            //    Nếu camPos NẰM TRONG collider → ClosestPoint trả về chính camPos.
            //    Nếu ở NGOÀI → trả về điểm gần nhất trên bề mặt (khác camPos).
            Vector3 closest      = m_Collider.ClosestPoint(camPos);
            bool    insideZone   = Vector3.SqrMagnitude(closest - camPos) < 0.0001f;  // ~1mm tolerance

            // ── 2. Camera có cao hơn ngưỡng an toàn không? ──────────────────
            //    Đo từ đáy collider (world Y thấp nhất của bounds).
            float zoneBottom       = m_Collider.bounds.min.y;
            float headHeightInZone = camPos.y - zoneBottom;
            bool  headInDanger     = insideZone && headHeightInZone > m_SafeHeightFromBottom;

            // ── 3. Phát events nếu trạng thái thay đổi ──────────────────────
            if (headInDanger && !m_PlayerInDanger)
            {
                m_PlayerInDanger = true;
                OnPlayerEnterSmoke?.Invoke(m_SmokeDensity);
                Debug.Log($"[SmokeZone] '{name}': Vào khói! " +
                          $"cam.y={camPos.y:F2} | zoneBottom={zoneBottom:F2} | heightInZone={headHeightInZone:F2} | threshold={m_SafeHeightFromBottom:F2}");
            }
            else if (!headInDanger && m_PlayerInDanger)
            {
                m_PlayerInDanger = false;
                OnPlayerExitSmoke?.Invoke();
                Debug.Log($"[SmokeZone] '{name}': Thoát khói! " +
                          $"cam.y={camPos.y:F2} | insideZone={insideZone} | heightInZone={headHeightInZone:F2}");
            }
        }

        // Debug: Bấm chuột phải vào SmokeZone component → "Log Zone Debug Info"
        [ContextMenu("Log Zone Debug Info")]
        void LogDebugInfo()
        {
            if (m_Collider == null) m_Collider = GetComponent<Collider>();
            var b = m_Collider.bounds;
            Debug.Log($"[SmokeZone] '{name}' BOUNDS: " +
                      $"min={b.min} | max={b.max} | center={b.center} | size={b.size}");
            if (Camera.main != null)
            {
                var cam = Camera.main.transform.position;
                var cp  = m_Collider.ClosestPoint(cam);
                Debug.Log($"[SmokeZone] Camera.main pos={cam} | ClosestPoint={cp} | " +
                          $"sqrDist={Vector3.SqrMagnitude(cp - cam):F6} | insideZone={Vector3.SqrMagnitude(cp - cam) < 0.0001f}");
            }
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
