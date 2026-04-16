using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace VRPCCC.Scenario2
{
    [RequireComponent(typeof(AudioSource))]
    public class FireSource : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham Chiếu Hiệu Ứng")]
        [SerializeField] ParticleSystem m_FireParticles;
        [SerializeField] ParticleSystem m_SmokeParticles;
        [SerializeField] AudioClip m_CracklingClip;

        [Header("Cài Đặt Dập Lửa")]
        [Tooltip("Bật lên nếu muốn lửa không bao giờ tắt (Dùng cho kịch bản chạy trốn).")]
        [SerializeField] public bool m_IsInvincible = false; // <--- THÊM DÒNG NÀY

        [Header("Cài Đặt Bùng Phát")]
        [SerializeField] float m_IgnitionDelay = 2f;
        [SerializeField] bool m_AutoIgnite = true;

        [Header("Cài Đặt Dập Lửa")]
        [SerializeField] public float m_ExtinguishDuration = 5f;
        [SerializeField] float m_MaxFireSize = 1.5f;
        [SerializeField] float m_MaxEmissionRate = 50f;

        [Header("Collider Gốc Lửa")]
        [SerializeField] BoxCollider m_RootFireCollider;

        [Header("Tham chiếu Scenario Manager")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Header("Hiển thị Vùng Hướng Dẫn (In-Game)")]
        [Tooltip("Bật để hiển thị vòng tròn khoảng cách trên mặt đất khi chơi.")]
        [SerializeField] bool m_ShowZonesInGame = true;
        [Tooltip("Độ dày của viền vòng tròn.")]
        [SerializeField] float m_LineWidth = 0.04f;
        [SerializeField] Color m_DangerColor = new Color(1f, 0.2f, 0f, 0.8f); // Đỏ (Quá gần)
        [SerializeField] Color m_SafeColor = new Color(0f, 1f, 0.5f, 0.8f);   // Xanh (An toàn)

        [Header("Events")]
        public UnityEvent OnFireIgnited;
        public UnityEvent OnFireExtinguished;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        AudioSource   m_AudioSource;
        bool          m_IsActive;
        float         m_ExtinguishProgress; 
        bool          m_IsExtinguished;

        // Các biến chứa LineRenderer
        LineRenderer m_DangerZoneLine;
        LineRenderer m_SafeZoneLine;

        public bool IsActive => m_IsActive;
        public bool IsExtinguished => m_IsExtinguished;
        public float ExtinguishProgress => m_ExtinguishProgress;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();

            if (m_RootFireCollider != null)
            {
                m_RootFireCollider.gameObject.tag = "Root_Fire";
                m_RootFireCollider.isTrigger = false;
            }

            SetFireActive(false);
        }

        void Start()
        {
            // Tự động tạo vòng tròn nếu được bật
            if (m_ShowZonesInGame && m_Manager != null)
            {
                SetupZoneVisuals();
            }

            if (m_AutoIgnite)
                StartCoroutine(DelayedIgnition());
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        public void Ignite()
        {
            if (m_IsExtinguished) return;

            SetFireActive(true);
            m_IsActive = true;
            
            // Bật hiển thị vòng tròn khi cháy
            SetZonesVisibility(true);

            m_Manager?.OnFireIgnited();
            OnFireIgnited?.Invoke();
            Debug.Log("[FireSource] 🔥 Lửa bùng phát!");
        }

        public void ApplyExtinguishing(float deltaTime)
        {
            if (!m_IsActive || m_IsExtinguished) return;

            // --- THÊM ĐOẠN NÀY ---
            if (m_IsInvincible)
            {
                // Vẫn gửi tín hiệu xịt để Manager đếm ngược thời gian, nhưng KHÔNG giảm lửa
                m_Manager?.OnSprayProgress(0f); 
                return; // Thoát hàm ngay lập tức
            }
            // ---------------------

            m_ExtinguishProgress = Mathf.Clamp01(m_ExtinguishProgress + deltaTime / m_ExtinguishDuration);

            float remaining = 1f - m_ExtinguishProgress;
            UpdateFireScale(remaining);

            m_Manager?.OnSprayProgress(m_ExtinguishProgress);

            if (m_ExtinguishProgress >= 1f)
                Extinguish();
        }

        public void Extinguish()
        {
            if (m_IsExtinguished) return;

            m_IsExtinguished = true;
            m_IsActive       = false;

            SetFireActive(false);
            
            // Tắt vòng tròn khi lửa đã tắt
            SetZonesVisibility(false);

            m_Manager?.OnFireExtinguished();
            OnFireExtinguished?.Invoke();
            Debug.Log("[FireSource] ✅ Lửa đã tắt hoàn toàn!");
        }

        public void ResetFire()
        {
            m_IsExtinguished     = false;
            m_IsActive           = false;
            m_ExtinguishProgress = 0f;
            UpdateFireScale(1f);
            SetFireActive(false);
            SetZonesVisibility(false);

            if (m_AutoIgnite)
                StartCoroutine(DelayedIgnition());
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal Helpers & Zone Visuals
        // ──────────────────────────────────────────────────────────────────── //

        IEnumerator DelayedIgnition()
        {
            yield return new WaitForSeconds(m_IgnitionDelay);
            Ignite();
        }

        void SetFireActive(bool active)
        {
            if (m_FireParticles != null)
            {
                if (active) m_FireParticles.Play();
                else        m_FireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (m_SmokeParticles != null)
            {
                if (active) m_SmokeParticles.Play();
                else        m_SmokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (m_AudioSource != null && m_CracklingClip != null)
            {
                if (active && !m_AudioSource.isPlaying)
                {
                    m_AudioSource.clip   = m_CracklingClip;
                    m_AudioSource.loop   = true;
                    m_AudioSource.Play();
                }
                else if (!active)
                {
                    m_AudioSource.Stop();
                }
            }
        }

        void UpdateFireScale(float remaining)
        {
            if (m_FireParticles != null)
            {
                var main     = m_FireParticles.main;
                var emission = m_FireParticles.emission;
                main.startSizeMultiplier       = m_MaxFireSize        * remaining;
                emission.rateOverTimeMultiplier = m_MaxEmissionRate    * remaining;
            }

            if (m_SmokeParticles != null)
            {
                var smokeEmission = m_SmokeParticles.emission;
                smokeEmission.rateOverTimeMultiplier = (m_MaxEmissionRate * 0.5f) * remaining;
            }

            if (m_AudioSource != null)
                m_AudioSource.volume = Mathf.Lerp(0f, 1f, remaining);
        }

        public void EscalateFire(float multiplier)
        {
            m_MaxFireSize *= multiplier; //
            m_MaxEmissionRate *= multiplier;
            //UpdateFireScale(1.0f); // Ép cập nhật lại kích thước hạt
        }
        
        // --- CODE MỚI: TẠO VÀ VẼ VÒNG TRÒN TRỰC TIẾP TRONG GAME ---
        void SetupZoneVisuals()
        {
            // Lấy trực tiếp khoảng cách chuẩn từ Manager để luôn đồng bộ
            float minDistance = m_Manager.m_MinFireDistance;
            float maxDistance = m_Manager.m_MaxFireDistance;

            m_DangerZoneLine = CreateCircleLine("DangerZone_Visual", m_DangerColor, minDistance);
            m_SafeZoneLine = CreateCircleLine("SafeZone_Visual", m_SafeColor, maxDistance);
            
            SetZonesVisibility(false); // Tắt lúc mới load, chỉ bật khi lửa cháy
        }

        LineRenderer CreateCircleLine(string objName, Color color, float radius)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            
            // Sử dụng Material mặc định của Unity (hỗ trợ màu trong suốt)
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = m_LineWidth;
            lr.endWidth = m_LineWidth;
            lr.useWorldSpace = false;
            lr.loop = true; // Nối điểm cuối với điểm đầu thành vòng kín

            // Tính toán tọa độ để vẽ hình tròn mượt mà
            int segments = 60;
            lr.positionCount = segments;
            float angle = 0f;
            for (int i = 0; i < segments; i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                // Nâng y lên 0.02f để vòng tròn không bị chìm/nhấp nháy dưới sàn nhà
                lr.SetPosition(i, new Vector3(x, 0.02f, z)); 
                angle += (360f / segments);
            }
            return lr;
        }

        void SetZonesVisibility(bool isVisible)
        {
            if (m_DangerZoneLine != null) m_DangerZoneLine.gameObject.SetActive(isVisible);
            if (m_SafeZoneLine != null) m_SafeZoneLine.gameObject.SetActive(isVisible);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Gizmos (Giữ nguyên cho Editor)
        // ──────────────────────────────────────────────────────────────────── //
        void OnDrawGizmos()
        {
            if (m_RootFireCollider != null)
            {
                Gizmos.color  = new Color(1f, 0.2f, 0f, 0.35f);
                var bounds    = m_RootFireCollider.bounds;
                Gizmos.DrawCube(bounds.center, bounds.size);

                Gizmos.color  = new Color(1f, 0.4f, 0f, 0.9f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Đồng bộ màu Gizmos với màu in-game
            Gizmos.color = m_DangerColor;
            float minDist = m_Manager != null ? m_Manager.m_MinFireDistance : 2f;
            DrawCircle(transform.position, minDist, 32);

            Gizmos.color = m_SafeColor;
            float maxDist = m_Manager != null ? m_Manager.m_MaxFireDistance : 3f;
            DrawCircle(transform.position, maxDist, 32);
        }

        static void DrawCircle(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a1 = Mathf.Deg2Rad * (i * step);
                float a2 = Mathf.Deg2Rad * ((i + 1) * step);
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius,
                    center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius
                );
            }
        }
    }
}