using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Quản lý toàn bộ kịch bản "Chống cháy tại chỗ".
    /// State machine trung tâm: nhận sự kiện từ các script khác và điều phối UI + điểm số.
    /// Gắn script này lên một GameObject trống (ScenarioManager) trong scene.
    /// </summary>
    public class FirefightingScenarioManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Enum – Trạng thái kịch bản
        // ──────────────────────────────────────────────────────────────────── //

        public enum ScenarioState
        {
            Idle,               // Chờ lửa bùng phát
            ApproachCabinet,    // Bước 1: Tiếp cận tủ PCCC
            SelectEquipment,    // Bước 2: Lựa chọn bình
            CheckDistance,      // Kỹ thuật 1: Kiểm tra khoảng cách
            PullPin,            // Kỹ thuật 2: Rút chốt an toàn
            AimNozzle,          // Kỹ thuật 3: Hướng vòi
            Spraying,           // Kỹ thuật 4: Phun
            Success,            // Kết thúc thành công
            Failed              // Kết thúc thất bại (timeout)
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham chiếu Scene")]
        [Tooltip("Script điều khiển đám lửa.")]
        [SerializeField] FireSource m_FireSource;

        [Tooltip("HUD / UI quản lý hiển thị.")]
        [SerializeField] ScenarioHUD m_HUD;

        [Header("Cài Đặt Điểm Số")]
        [Tooltip("Điểm bắt đầu kịch bản.")]
        [SerializeField] int m_InitialScore = 100;

        [Tooltip("Điểm bị trừ khi chọn sai bình bột ABC.")]
        [SerializeField] int m_WrongChoicePenalty = 20;

        [Tooltip("Thời gian tối đa hoàn thành kịch bản (giây). 0 = không giới hạn.")]
        [SerializeField] float m_ScenarioTimeLimit = 0f;

        [Header("Khoảng Cách An Toàn (Check 1)")]
        [Tooltip("Khoảng cách tối thiểu tính từ người dùng đến gốc lửa (m).")]
        [SerializeField] public float m_MinFireDistance = 2f;

        [Tooltip("Khoảng cách tối đa tính từ người dùng đến gốc lửa (m).")]
        [SerializeField] public float m_MaxFireDistance = 3f;

        [Header("Events")]
        public UnityEvent OnScenarioStart;
        public UnityEvent<int> OnScenarioSuccess;   // int = điểm cuối
        public UnityEvent OnScenarioFailed;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        ScenarioState m_State = ScenarioState.Idle;
        int           m_Score;
        float         m_ElapsedTime;
        bool          m_TimerRunning;

        /// <summary>Trạng thái kịch bản hiện tại (read-only).</summary>
        public ScenarioState CurrentState => m_State;

        /// <summary>Điểm số hiện tại.</summary>
        public int Score => m_Score;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_Score = m_InitialScore;
        }

        void Start()
        {
            // Kịch bản bắt đầu ở trạng thái Idle cho đến khi lửa bùng
            SetState(ScenarioState.Idle);
            m_HUD?.ShowStep("Quan sát môi trường xung quanh...");
        }

        void Update()
        {
            if (m_TimerRunning && m_ScenarioTimeLimit > 0f)
            {
                m_ElapsedTime += Time.deltaTime;
                if (m_ElapsedTime >= m_ScenarioTimeLimit)
                {
                    m_TimerRunning = false;
                    SetState(ScenarioState.Failed);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API – Được gọi bởi các script khác
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Gọi khi lửa bùng phát (ví dụ: FireSource.Start() gọi sau delay).
        /// Chuyển sang bước 1: Yêu cầu tiếp cận tủ PCCC.
        /// </summary>
        public void OnFireIgnited()
        {
            if (m_State != ScenarioState.Idle) return;

            SetState(ScenarioState.ApproachCabinet);
            m_TimerRunning = true;
            OnScenarioStart?.Invoke();

            m_HUD?.ShowStep(
                "<b>🔥 PHÁT HIỆN ĐÁM CHÁY!</b>\n" +
                "Tiếp cận tủ PCCC trên tường hành lang."
            );

            Debug.Log("[Scenario2] 🔥 Lửa bùng phát - Bước 1: Tiếp cận tủ PCCC");
        }

        /// <summary>
        /// [Bước 1] Người dùng vào trong vùng 1.5m quanh tủ PCCC.
        /// Chuyển sang bước chọn thiết bị.
        /// </summary>
        public void OnPlayerApproachCabinet()
        {
            if (m_State != ScenarioState.ApproachCabinet) return;

            SetState(ScenarioState.SelectEquipment);
            m_HUD?.ShowStep(
                "<b>✅ Đã đến tủ PCCC.</b>\n" +
                "Chọn bình chữa cháy phù hợp.\n\n" +
                "• Bình bột ABC (màu đỏ)\n" +
                "• Bình khí CO₂ (màu đen)"
            );

            Debug.Log("[Scenario2] Bước 2: Chọn thiết bị");
        }

        /// <summary>
        /// [Bước 2] Người dùng lấy bình chữa cháy.
        /// </summary>
        /// <param name="isCO2">true = bình CO2 (đúng), false = bình ABC (sai).</param>
        public void OnExtinguisherGrabbed(bool isCO2)
        {
            if (m_State != ScenarioState.SelectEquipment) return;

            if (isCO2)
            {
                SetState(ScenarioState.CheckDistance);
                m_HUD?.ShowStep(
                    "<b>✅ Chọn đúng! Bình CO₂ phù hợp cho đám cháy điện.</b>\n\n" +
                    "Di chuyển đến khoảng cách <b>2–3m</b> so với đám cháy."
                );
                Debug.Log("[Scenario2] ✅ Chọn CO2 - Kỹ thuật 1: Kiểm tra khoảng cách");
            }
            else
            {
                // Sai lựa chọn - trừ điểm nhưng không block (cho phép thử lại)
                m_Score = Mathf.Max(0, m_Score - m_WrongChoicePenalty);
                m_HUD?.ShowWarning(
                    "⚠️ Bình bột có thể gây hư hỏng thiết bị điện và khó vệ sinh.\nHãy chọn lại.",
                    4f
                );
                Debug.Log($"[Scenario2] ❌ Chọn sai (ABC) - Trừ {m_WrongChoicePenalty} điểm. Điểm còn: {m_Score}");
            }
        }

        /// <summary>
        /// [Kỹ thuật 1] Kiểm tra khoảng cách người dùng đến gốc lửa.
        /// Gọi mỗi frame hoặc theo poll từ CO2Extinguisher.
        /// </summary>
        /// <param name="distance">Khoảng cách hiện tại (m).</param>
        /// <returns>true nếu khoảng cách hợp lệ.</returns>
        public bool OnDistanceCheck(float distance)
        {
            if (m_State != ScenarioState.CheckDistance) return false;

            if (distance < m_MinFireDistance)
            {
                m_HUD?.ShowWarning($"⚠️ Quá gần! Hãy lùi ra xa hơn (cần ≥ {m_MinFireDistance}m).", 1.5f);
                return false;
            }
            else if (distance > m_MaxFireDistance)
            {
                m_HUD?.ShowWarning($"⚠️ Quá xa! Hãy tiến lại gần hơn (cần ≤ {m_MaxFireDistance}m).", 1.5f);
                return false;
            }
            else
            {
                SetState(ScenarioState.PullPin);
                m_HUD?.ShowStep(
                    "<b>✅ Khoảng cách tốt!</b>\n\n" +
                    "Kỹ thuật 2: Rút chốt an toàn khỏi bình."
                );
                Debug.Log("[Scenario2] ✅ Khoảng cách OK - Kỹ thuật 2: Rút chốt");
                return true;
            }
        }

        /// <summary>
        /// [Kỹ thuật 2] Người dùng đã rút chốt an toàn.
        /// </summary>
        public void OnPinPulled()
        {
            if (m_State != ScenarioState.PullPin) return;

            SetState(ScenarioState.AimNozzle);
            m_HUD?.ShowStep(
                "<b>✅ Đã rút chốt!</b>\n\n" +
                "Kỹ thuật 3: Hướng vòi phun vào <b>gốc lửa</b>.\n" +
                "(Không hướng vào ngọn lửa phía trên)"
            );
            Debug.Log("[Scenario2] ✅ Đã rút chốt - Kỹ thuật 3: Hướng vòi");
        }

        /// <summary>
        /// [Kỹ thuật 3] Cập nhật trạng thái aim.
        /// </summary>
        /// <param name="onTarget">Raycast đang trúng Root_Fire collider.</param>
        public void OnNozzleAimedUpdate(bool onTarget)
        {
            if (m_State != ScenarioState.AimNozzle) return;

            if (onTarget)
            {
                SetState(ScenarioState.Spraying);
                m_HUD?.ShowStep(
                    "<b>✅ Đang nhắm đúng gốc lửa!</b>\n\n" +
                    "Kỹ thuật 4: Giữ nút bóp cò để phun.\n" +
                    "Phun liên tục, quét từ trái sang phải."
                );
                Debug.Log("[Scenario2] ✅ Nhắm đúng - Kỹ thuật 4: Phun");
            }
        }

        /// <summary>
        /// [Kỹ thuật 4] Tiến trình phun (0–1). Được gọi liên tục khi phun.
        /// </summary>
        /// <param name="progress">Tiến độ dập lửa (0 = bắt đầu, 1 = hoàn tất).</param>
        public void OnSprayProgress(float progress)
        {
            if (m_State != ScenarioState.Spraying) return;
            // HUD có thể cập nhật progress bar nếu muốn
        }

        /// <summary>
        /// Được gọi khi đám lửa tắt hoàn toàn.
        /// </summary>
        public void OnFireExtinguished()
        {
            if (m_State == ScenarioState.Success) return;

            m_TimerRunning = false;
            SetState(ScenarioState.Success);

            m_HUD?.ShowSuccess(m_Score);
            m_HUD?.ShowLegalNote(
                "Theo <b>Thông tư 150/2020/TT-BCA</b> và <b>TCVN 7435</b>:\n" +
                "Bình chữa cháy phải được kiểm tra định kỳ ít nhất 1 lần/năm và " +
                "bảo dưỡng định kỳ để đảm bảo khả năng hoạt động."
            );

            OnScenarioSuccess?.Invoke(m_Score);
            Debug.Log($"[Scenario2] 🎉 DẬP LỬA THÀNH CÔNG! Điểm cuối: {m_Score}");
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal Helpers
        // ──────────────────────────────────────────────────────────────────── //

        void SetState(ScenarioState newState)
        {
            m_State = newState;
            Debug.Log($"[Scenario2] ── State → {newState}");
        }

        /// <summary>Đặt lại kịch bản từ đầu (dùng cho nút "Thử lại").</summary>
        [ContextMenu("Reset Scenario")]
        public void ResetScenario()
        {
            m_Score       = m_InitialScore;
            m_ElapsedTime = 0f;
            m_TimerRunning = false;
            SetState(ScenarioState.Idle);
            m_FireSource?.ResetFire();
            m_HUD?.ResetHUD();
            Debug.Log("[Scenario2] 🔄 Kịch bản đã được đặt lại.");
        }
    }
}
