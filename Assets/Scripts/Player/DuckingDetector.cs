using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Player
{
    /// <summary>
    /// Phát hiện khi người chơi VR cúi người / né khói dựa trên vị trí đầu thực (HMD).
    /// Gắn script này lên XR Origin hoặc một GameObject trống trong scene.
    /// </summary>
    public class DuckingDetector : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector Fields
        // ------------------------------------------------------------------ //

        [Header("References")]
        [Tooltip("Transform của camera VR (HMD). Để trống sẽ tự tìm Camera.main.")]
        [SerializeField] Transform m_CameraTransform;

        [Header("Ngưỡng Cúi Người")]
        [Tooltip("Phần trăm chiều cao đứng để xác định cúi người (0.5 = 50%, 0.75 = 75%).")]
        [Range(0.4f, 0.9f)]
        [SerializeField] float m_DuckThresholdPercent = 0.70f;

        [Tooltip("Độ cao đứng mặc định (m) nếu không hiệu chỉnh được từ HMD.")]
        [SerializeField] float m_DefaultStandingHeight = 1.7f;

        [Tooltip("Thời gian giữ tư thế cúi trước khi kích hoạt sự kiện (tránh giật/rung).")]
        [SerializeField] float m_DuckHoldTime = 0.1f;

        [Tooltip("Tự động hiệu chỉnh chiều cao đứng khi Start.")]
        [SerializeField] bool m_CalibrateOnStart = true;

        [Tooltip("Hiệu chỉnh lại sau bao nhiêu giây (để headset khởi động ổn định). 0 = ngay lập tức.")]
        [SerializeField] float m_CalibrationDelay = 2f;

        [Header("Mức Khói An Toàn")]
        [Tooltip("Chiều cao (Y thế giới) dưới đây là vùng an toàn, không có khói.")]
        [SerializeField] float m_SmokeFloorHeight = 1.2f;

        [Header("Events")]
        [Tooltip("Khi bắt đầu cúi thấp hơn ngưỡng an toàn.")]
        public UnityEvent OnDuckStart;
        [Tooltip("Khi đứng dậy vượt ngưỡng.")]
        public UnityEvent OnDuckEnd;
        [Tooltip("Khi đầu vào vùng khói (trên mức smoke floor).")]
        public UnityEvent OnEnterSmoke;
        [Tooltip("Khi đầu ra khỏi vùng khói (dưới mức smoke floor).")]
        public UnityEvent OnExitSmoke;

        // ------------------------------------------------------------------ //
        //  Runtime State
        // ------------------------------------------------------------------ //

        float m_StandingHeight;
        float m_DuckThresholdHeight;
        float m_DuckTimer;
        bool m_IsDucking;
        bool m_IsInSmoke;
        bool m_IsCalibrated;

        // ------------------------------------------------------------------ //
        //  Public Properties
        // ------------------------------------------------------------------ //

        /// <summary>Người chơi hiện đang cúi người (đầu dưới ngưỡng).</summary>
        public bool IsDucking => m_IsDucking;

        /// <summary>Đầu người chơi đang trong vùng khói.</summary>
        public bool IsInSmoke => m_IsInSmoke;

        /// <summary>Chiều cao đầu hiện tại (Y world space).</summary>
        public float HeadHeight => m_CameraTransform != null ? m_CameraTransform.position.y : 0f;

        /// <summary>Ngưỡng Y để xác định cúi người.</summary>
        public float DuckThresholdHeight => m_DuckThresholdHeight;

        /// <summary>Mức sàn khói an toàn.</summary>
        public float SmokeFloorHeight
        {
            get => m_SmokeFloorHeight;
            set => m_SmokeFloorHeight = value;
        }

        /// <summary>Tỉ lệ cúi (0 = đứng thẳng, 1 = cúi tối đa).</summary>
        public float DuckRatio
        {
            get
            {
                if (!m_IsCalibrated || m_StandingHeight <= 0f) return 0f;
                float ratio = 1f - Mathf.Clamp01((HeadHeight - 0f) / m_StandingHeight);
                return ratio;
            }
        }

        // ------------------------------------------------------------------ //
        //  Unity Lifecycle
        // ------------------------------------------------------------------ //

        void Awake()
        {
            if (m_CameraTransform == null && Camera.main != null)
                m_CameraTransform = Camera.main.transform;
        }

        void Start()
        {
            if (m_CalibrateOnStart)
            {
                if (m_CalibrationDelay > 0f)
                    Invoke(nameof(CalibrateStandingHeight), m_CalibrationDelay);
                else
                    CalibrateStandingHeight();
            }
            else
            {
                SetStandingHeight(m_DefaultStandingHeight);
            }
        }

        void Update()
        {
            if (m_CameraTransform == null) return;

            float headY = m_CameraTransform.position.y;

            UpdateDuckingState(headY);
            UpdateSmokeState(headY);
        }

        // ------------------------------------------------------------------ //
        //  Calibration
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Hiệu chỉnh chiều cao đứng từ vị trí HMD hiện tại.
        /// Gọi khi người chơi đứng thẳng.
        /// </summary>
        [ContextMenu("Calibrate Standing Height Now")]
        public void CalibrateStandingHeight()
        {
            if (m_CameraTransform == null)
            {
                Debug.LogWarning("[DuckingDetector] Không tìm thấy Camera transform để hiệu chỉnh.");
                SetStandingHeight(m_DefaultStandingHeight);
                return;
            }

            float measuredHeight = m_CameraTransform.position.y;

            // Sanity check: nếu chiều cao đo được quá thấp (headset chưa sẵn), dùng default
            if (measuredHeight < 0.5f)
            {
                Debug.LogWarning($"[DuckingDetector] Chiều cao đo được ({measuredHeight:F2}m) quá thấp, dùng mặc định {m_DefaultStandingHeight}m.");
                SetStandingHeight(m_DefaultStandingHeight);
            }
            else
            {
                SetStandingHeight(measuredHeight);
            }
        }

        void SetStandingHeight(float height)
        {
            m_StandingHeight = height;
            m_DuckThresholdHeight = height * m_DuckThresholdPercent;
            m_IsCalibrated = true;
            Debug.Log($"[DuckingDetector] Chiều cao đứng: {m_StandingHeight:F2}m | Ngưỡng cúi: {m_DuckThresholdHeight:F2}m");
        }

        // ------------------------------------------------------------------ //
        //  Detection Logic
        // ------------------------------------------------------------------ //

        void UpdateDuckingState(float headY)
        {
            if (!m_IsCalibrated) return;

            bool headBelowThreshold = headY < m_DuckThresholdHeight;

            if (headBelowThreshold && !m_IsDucking)
            {
                // Đếm thời gian giữ tư thế cúi
                m_DuckTimer += Time.deltaTime;
                if (m_DuckTimer >= m_DuckHoldTime)
                {
                    m_IsDucking = true;
                    m_DuckTimer = 0f;
                    OnDuckStart?.Invoke();
                    Debug.Log($"[DuckingDetector] BẮT ĐẦU CÚI - Đầu tại {headY:F2}m");
                }
            }
            else if (!headBelowThreshold && m_IsDucking)
            {
                m_IsDucking = false;
                m_DuckTimer = 0f;
                OnDuckEnd?.Invoke();
                Debug.Log($"[DuckingDetector] ĐỨNG DẬY - Đầu tại {headY:F2}m");
            }
            else if (!headBelowThreshold)
            {
                m_DuckTimer = 0f; // reset nếu không cúi
            }
        }

        void UpdateSmokeState(float headY)
        {
            bool inSmoke = headY > m_SmokeFloorHeight;

            if (inSmoke && !m_IsInSmoke)
            {
                m_IsInSmoke = true;
                OnEnterSmoke?.Invoke();
            }
            else if (!inSmoke && m_IsInSmoke)
            {
                m_IsInSmoke = false;
                OnExitSmoke?.Invoke();
            }
        }

        // ------------------------------------------------------------------ //
        //  Gizmos (Scene View)
        // ------------------------------------------------------------------ //

        void OnDrawGizmosSelected()
        {
            var center = transform.position;

            // --- Mức sàn khói (vùng an toàn) ---
            Gizmos.color = new Color(0.0f, 0.8f, 1.0f, 0.4f);
            DrawHorizontalLine(center, m_SmokeFloorHeight, 3f);
            var labelPos = new Vector3(center.x, m_SmokeFloorHeight + 0.05f, center.z);
            // Gizmo label chỉ hiện trong Editor

            // --- Ngưỡng cúi người ---
            if (Application.isPlaying && m_IsCalibrated)
            {
                Gizmos.color = new Color(1.0f, 0.6f, 0.0f, 0.7f);
                DrawHorizontalLine(center, m_DuckThresholdHeight, 3f);

                // Chiều cao đứng
                Gizmos.color = new Color(0.2f, 1.0f, 0.2f, 0.5f);
                DrawHorizontalLine(center, m_StandingHeight, 3f);
            }

            // --- Vị trí đầu hiện tại ---
            if (Application.isPlaying && m_CameraTransform != null)
            {
                Gizmos.color = m_IsDucking ? Color.green : (m_IsInSmoke ? Color.red : Color.white);
                Gizmos.DrawSphere(m_CameraTransform.position, 0.08f);
            }
        }

        static void DrawHorizontalLine(Vector3 center, float y, float halfLength)
        {
            Gizmos.DrawLine(
                new Vector3(center.x - halfLength, y, center.z),
                new Vector3(center.x + halfLength, y, center.z)
            );
            Gizmos.DrawLine(
                new Vector3(center.x, y, center.z - halfLength),
                new Vector3(center.x, y, center.z + halfLength)
            );
        }
    }
}
