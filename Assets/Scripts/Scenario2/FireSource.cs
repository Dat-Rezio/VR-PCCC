using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Điều khiển đám lửa tại tủ điện hành lang.
    /// Gắn lên GameObject chứa ParticleSystem lửa (có BoxCollider tag "Root_Fire").
    /// Tự động bùng phát sau một khoảng delay và giảm dần khi bị phun CO2.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FireSource : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham Chiếu Hiệu Ứng")]
        [Tooltip("ParticleSystem của lửa chính (ngọn lửa màu cam/đỏ).")]
        [SerializeField] ParticleSystem m_FireParticles;

        [Tooltip("ParticleSystem của khói đen bốc lên.")]
        [SerializeField] ParticleSystem m_SmokeParticles;

        [Tooltip("AudioClip tiếng lách tách của lửa điện.")]
        [SerializeField] AudioClip m_CracklingClip;

        [Header("Cài Đặt Bùng Phát")]
        [Tooltip("Delay (giây) từ khi scene Start đến khi lửa bùng.")]
        [SerializeField] float m_IgnitionDelay = 2f;

        [Tooltip("Nếu true, tự động bùng phát khi scene Start.")]
        [SerializeField] bool m_AutoIgnite = true;

        [Header("Cài Đặt Dập Lửa")]
        [Tooltip("Thời gian phun CO2 liên tục để dập tắt hoàn toàn (giây).")]
        [SerializeField] public float m_ExtinguishDuration = 5f;

        [Tooltip("Start size mặc định của ParticleSystem lửa khi đang cháy đầy đủ.")]
        [SerializeField] float m_MaxFireSize = 1.5f;

        [Tooltip("Tốc độ phát hạt tối đa (emission rate over time) khi cháy.")]
        [SerializeField] float m_MaxEmissionRate = 50f;

        [Header("Collider Gốc Lửa")]
        [Tooltip("BoxCollider đại diện cho vùng gốc lửa - cần đủ lớn cho Raycast dễ bắt trúng.")]
        [SerializeField] BoxCollider m_RootFireCollider;

        [Header("Tham chiếu Scenario Manager")]
        [SerializeField] FirefightingScenarioManager m_Manager;

        [Header("Events")]
        public UnityEvent OnFireIgnited;
        public UnityEvent OnFireExtinguished;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        AudioSource   m_AudioSource;
        bool          m_IsActive;
        float         m_ExtinguishProgress; // 0 = đang cháy, 1 = đã tắt
        bool          m_IsExtinguished;

        /// <summary>Lửa đang hoạt động.</summary>
        public bool IsActive => m_IsActive;

        /// <summary>Lửa đã tắt hoàn toàn.</summary>
        public bool IsExtinguished => m_IsExtinguished;

        /// <summary>Tiến độ dập lửa (0–1).</summary>
        public float ExtinguishProgress => m_ExtinguishProgress;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();

            // Đảm bảo collider gốc lửa có đúng tag
            if (m_RootFireCollider != null)
            {
                m_RootFireCollider.gameObject.tag = "Root_Fire";
                m_RootFireCollider.isTrigger = false;
            }

            // Tắt lửa ban đầu
            SetFireActive(false);
        }

        void Start()
        {
            if (m_AutoIgnite)
                StartCoroutine(DelayedIgnition());
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>Bùng phát lửa ngay lập tức.</summary>
        public void Ignite()
        {
            if (m_IsExtinguished) return;

            SetFireActive(true);
            m_IsActive    = true;
            m_Manager?.OnFireIgnited();
            OnFireIgnited?.Invoke();
            Debug.Log("[FireSource] 🔥 Lửa bùng phát!");
        }

        /// <summary>
        /// Áp dụng hiệu ứng dập lửa theo thời gian delta.
        /// Gọi liên tục từ CO2Extinguisher khi đang phun.
        /// </summary>
        /// <param name="deltaTime">Thời gian phun trong frame này.</param>
        public void ApplyExtinguishing(float deltaTime)
        {
            if (!m_IsActive || m_IsExtinguished) return;

            m_ExtinguishProgress = Mathf.Clamp01(
                m_ExtinguishProgress + deltaTime / m_ExtinguishDuration
            );

            // Giảm kích thước và tốc độ phát hạt theo tiến độ
            float remaining = 1f - m_ExtinguishProgress;
            UpdateFireScale(remaining);

            m_Manager?.OnSprayProgress(m_ExtinguishProgress);

            // Tắt hoàn toàn khi đạt 100%
            if (m_ExtinguishProgress >= 1f)
                Extinguish();
        }

        /// <summary>Tắt lửa hoàn toàn.</summary>
        public void Extinguish()
        {
            if (m_IsExtinguished) return;

            m_IsExtinguished = true;
            m_IsActive       = false;

            SetFireActive(false);
            m_Manager?.OnFireExtinguished();
            OnFireExtinguished?.Invoke();
            Debug.Log("[FireSource] ✅ Lửa đã tắt hoàn toàn!");
        }

        /// <summary>Đặt lại đám lửa về trạng thái ban đầu.</summary>
        public void ResetFire()
        {
            m_IsExtinguished     = false;
            m_IsActive           = false;
            m_ExtinguishProgress = 0f;
            UpdateFireScale(1f);
            SetFireActive(false);

            if (m_AutoIgnite)
                StartCoroutine(DelayedIgnition());
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal Helpers
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

        /// <summary>Điều chỉnh kích thước và emission của lửa theo hệ số còn lại (0–1).</summary>
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

            // Giảm âm lượng tiếng cháy
            if (m_AudioSource != null)
                m_AudioSource.volume = Mathf.Lerp(0f, 1f, remaining);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Gizmos
        // ──────────────────────────────────────────────────────────────────── //

        void OnDrawGizmos()
        {
            // Vẽ collider gốc lửa màu đỏ trong Scene view
            if (m_RootFireCollider != null)
            {
                Gizmos.color  = new Color(1f, 0.2f, 0f, 0.35f);
                var bounds    = m_RootFireCollider.bounds;
                Gizmos.DrawCube(bounds.center, bounds.size);

                Gizmos.color  = new Color(1f, 0.4f, 0f, 0.9f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Vẽ vòng khoảng cách an toàn
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
            DrawCircle(transform.position, 2f, 32);

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            DrawCircle(transform.position, 3f, 32);
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
