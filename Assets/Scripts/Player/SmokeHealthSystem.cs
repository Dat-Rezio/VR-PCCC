using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Player
{
    /// <summary>
    /// Hệ thống sức khỏe / oxygen khi người chơi tiếp xúc khói.
    /// Kết hợp với DuckingDetector: cúi người → không mất oxygen.
    /// Đứng thẳng trong khói → oxygen giảm dần.
    ///
    /// Kết nối Events:
    ///   SmokeZone.OnPlayerEnterSmoke  → SmokeHealthSystem.BeginExposure(float)
    ///   SmokeZone.OnPlayerExitSmoke   → SmokeHealthSystem.EndExposure()
    ///   DuckingDetector.OnDuckStart   → SmokeHealthSystem.OnDuckStart()
    ///   DuckingDetector.OnDuckEnd     → SmokeHealthSystem.OnDuckEnd()
    /// </summary>
    public class SmokeHealthSystem : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector Fields
        // ------------------------------------------------------------------ //

        [Header("Oxygen")]
        [Tooltip("Lượng oxygen tối đa (giây trụ được trong khói).")]
        [SerializeField] float m_MaxOxygen = 20f;

        [Tooltip("Tốc độ giảm oxygen khi đứng trong khói (đơn vị/giây). Nhân thêm với SmokeDensity.")]
        [SerializeField] float m_OxygenDrainRate = 5f;

        [Tooltip("Tốc độ hồi phục oxygen khi ra khỏi khói hoặc cúi người an toàn (đơn vị/giây).")]
        [SerializeField] float m_OxygenRecoveryRate = 8f;

        [Header("Ngưỡng Cảnh Báo")]
        [Tooltip("Phần trăm oxygen còn lại để kích hoạt cảnh báo nguy hiểm.")]
        [Range(0f, 0.5f)]
        [SerializeField] float m_DangerThresholdPercent = 0.30f;

        [Tooltip("Phần trăm oxygen còn lại để kích hoạt sự kiện bất tỉnh / thua.")]
        [Range(0f, 0.15f)]
        [SerializeField] float m_KnockoutThresholdPercent = 0.0f;

        [Header("SmokeVisionEffect (tuỳ chọn)")]
        [Tooltip("Nếu có, sẽ tự gọi TriggerDangerWarning() khi oxygen nguy hiểm.")]
        [SerializeField] SmokeVisionEffect m_VisionEffect;

        [Header("Events")]
        [Tooltip("Ngay khi oxygen chạm ngưỡng nguy hiểm.")]
        public UnityEvent OnDangerLevel;

        [Tooltip("Khi oxygen về 0 → người chơi bị bất tỉnh / thua.")]
        public UnityEvent OnKnockout;

        [Tooltip("Khi người chơi phục hồi từ ngưỡng nguy hiểm về an toàn.")]
        public UnityEvent OnRecoveredFromDanger;

        [Tooltip("Cập nhật mỗi frame, trả về tỉ lệ oxygen (0-1) để cập nhật UI.")]
        public UnityEvent<float> OnOxygenChanged;

        // ------------------------------------------------------------------ //
        //  Runtime State
        // ------------------------------------------------------------------ //

        float m_CurrentOxygen;
        float m_SmokeDensity;
        bool m_InSmokeZone;
        bool m_IsDucking;
        bool m_InDangerState;
        bool m_IsKnockedOut;

        /// <summary>Tỉ lệ oxygen hiện tại (0 = hết, 1 = đầy).</summary>
        public float OxygenRatio => m_CurrentOxygen / m_MaxOxygen;

        /// <summary>Đang ở trạng thái nguy hiểm (oxygen thấp).</summary>
        public bool IsInDanger => m_InDangerState;

        /// <summary>Đã bất tỉnh (oxygen = 0).</summary>
        public bool IsKnockedOut => m_IsKnockedOut;

        // ------------------------------------------------------------------ //
        //  Unity Lifecycle
        // ------------------------------------------------------------------ //

        void Awake()
        {
            m_CurrentOxygen = m_MaxOxygen;
        }

        void Update()
        {
            if (m_IsKnockedOut) return;

            UpdateOxygen();
            CheckThresholds();
        }

        // ------------------------------------------------------------------ //
        //  Public API
        // ------------------------------------------------------------------ //

        /// <summary>Gọi khi người chơi vào vùng khói. density: 0-1.</summary>
        public void BeginExposure(float density)
        {
            m_InSmokeZone = true;
            m_SmokeDensity = Mathf.Clamp01(density);
        }

        /// <summary>Gọi khi người chơi thoát vùng khói.</summary>
        public void EndExposure()
        {
            m_InSmokeZone = false;
            m_SmokeDensity = 0f;
        }

        /// <summary>Gọi khi DuckingDetector phát hiện cúi người.</summary>
        public void OnDuckStart()
        {
            m_IsDucking = true;
        }

        /// <summary>Gọi khi DuckingDetector phát hiện đứng dậy.</summary>
        public void OnDuckEnd()
        {
            m_IsDucking = false;
        }

        /// <summary>Reset về trạng thái ban đầu (dùng khi restart kịch bản).</summary>
        public void ResetOxygen()
        {
            m_CurrentOxygen = m_MaxOxygen;
            m_InDangerState = false;
            m_IsKnockedOut = false;
        }

        // ------------------------------------------------------------------ //
        //  Internal Logic
        // ------------------------------------------------------------------ //

        void UpdateOxygen()
        {
            bool isSafe = !m_InSmokeZone || m_IsDucking;

            if (!isSafe)
            {
                // Đứng thẳng trong khói → mất oxygen
                float drain = m_OxygenDrainRate * m_SmokeDensity * Time.deltaTime;
                m_CurrentOxygen = Mathf.Max(0f, m_CurrentOxygen - drain);
            }
            else
            {
                // An toàn (cúi người hoặc ngoài khói) → hồi phục oxygen
                if (m_CurrentOxygen < m_MaxOxygen)
                {
                    float recovery = m_OxygenRecoveryRate * Time.deltaTime;
                    m_CurrentOxygen = Mathf.Min(m_MaxOxygen, m_CurrentOxygen + recovery);
                }
            }

            OnOxygenChanged?.Invoke(OxygenRatio);
        }

        void CheckThresholds()
        {
            float ratio = OxygenRatio;
            float dangerThreshold = m_DangerThresholdPercent;
            float knockoutThreshold = m_KnockoutThresholdPercent;

            // --- Knockout ---
            if (ratio <= knockoutThreshold && !m_IsKnockedOut && m_InSmokeZone && !m_IsDucking)
            {
                m_IsKnockedOut = true;
                OnKnockout?.Invoke();
                Debug.Log("[SmokeHealthSystem] ❌ Người chơi bất tỉnh vì khói!");
                return;
            }

            // --- Vào vùng nguy hiểm ---
            if (ratio <= dangerThreshold && !m_InDangerState)
            {
                m_InDangerState = true;
                OnDangerLevel?.Invoke();
                m_VisionEffect?.TriggerDangerWarning(1f - ratio); // Intensity tỉ lệ với mức thiếu
                Debug.Log($"[SmokeHealthSystem] ⚠️ NGUY HIỂM! Oxygen còn {ratio * 100f:F0}%");
            }
            // --- Hồi phục khỏi nguy hiểm ---
            else if (ratio > dangerThreshold + 0.1f && m_InDangerState)
            {
                m_InDangerState = false;
                OnRecoveredFromDanger?.Invoke();
                Debug.Log($"[SmokeHealthSystem] ✅ Đã an toàn! Oxygen: {ratio * 100f:F0}%");
            }
        }

        // ------------------------------------------------------------------ //
        //  Editor
        // ------------------------------------------------------------------ //

        void OnValidate()
        {
            m_MaxOxygen = Mathf.Max(1f, m_MaxOxygen);
            m_OxygenDrainRate = Mathf.Max(0f, m_OxygenDrainRate);
            m_OxygenRecoveryRate = Mathf.Max(0f, m_OxygenRecoveryRate);
        }
    }
}
