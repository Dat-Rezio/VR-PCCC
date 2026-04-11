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

        [Tooltip("Đám cháy 1: Tủ lạnh nhà bếp (Cần CO2)")]
        [SerializeField] FireSource m_FridgeFire;

        [Tooltip("Đám cháy 2: Trong phòng ngủ (Cần ABC)")]
        [SerializeField] FireSource m_BedroomFire;

        public int CurrentPhase { get; private set; } = 1;

        /// <summary>Trả về FireSource của Phase hiện tại.</summary>
        public FireSource GetActiveFire() => CurrentPhase == 1 ? m_FridgeFire : m_BedroomFire;

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
            CurrentPhase = 1;
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

            string fireMsg = CurrentPhase == 1 
                ? "<b>🔥 PHÁT HIỆN CHÁY TỦ LẠNH KHU BẾP!</b>" 
                : "<b>🔥 PHÁT HIỆN CHÁY LIỀM NỆM TRONG PHÒNG NGỦ!</b>";

            m_HUD?.ShowStep(
                $"{fireMsg}\n" +
                "Kế tiếp: Tiếp cận tủ PCCC.\n\n" +
                "<size=14><i><color=#FFD700>[Simulator] Kích chuột vào màn hình Game rồi giữ phím chữ 'I' dể đi lướt tới tủ</color></i></size>"
            );

            Debug.Log($"[Scenario2] 🔥 Lửa bùng phát (Phase {CurrentPhase}) - Bước 1: Tiếp cận tủ PCCC");
        }

        /// <summary>
        /// [Bước 1] Người dùng vào trong vùng 1.5m quanh tủ PCCC.
        /// Chuyển sang bước chọn thiết bị.
        /// </summary>
        public void OnPlayerApproachCabinet()
        {
            if (m_State != ScenarioState.ApproachCabinet) return;

            SetState(ScenarioState.SelectEquipment);
            
            string requestedExt = CurrentPhase == 1 ? "bình khí CO₂ (màu đen)" : "bình bột ABC (màu đỏ)";
            
            m_HUD?.ShowStep(
                "<b> Đã đến tủ PCCC.</b>\n" +
                $"Hãy chọn ĐÚNG {requestedExt}.\n\n" +
                "<size=13><i><color=#FFD700>[Simulator]\n1. Bấm 'Tab' chuyển sang [Right Controller]\n2. Đưa tay chạm vào bình\n3. Bấm phím 'G' một lần (đừng đè) để cầm</color></i></size>"
            );

            Debug.Log($"[Scenario2] Bước 2: Chọn thiết bị (Cần {(CurrentPhase == 1 ? "CO2" : "ABC")})");
        }

        /// <summary>
        /// [Bước 2] Người dùng lấy bình chữa cháy.
        /// </summary>
        /// <param name="isCO2">true = bình CO2 (đúng), false = bình ABC (sai).</param>
        public void OnExtinguisherGrabbed(bool isCO2)
        {
            if (m_State != ScenarioState.SelectEquipment) return;

            bool isCorrect = (CurrentPhase == 1 && isCO2) || (CurrentPhase == 2 && !isCO2);

            if (isCorrect)
            {
                SetState(ScenarioState.CheckDistance);
                string extName = isCO2 ? "CO₂" : "ABC";
                m_HUD?.ShowStep(
                    $"<b> Đã cầm bình {extName}!</b>\n\n" +
                    "Di chuyển đến khoảng cách <b>2–3m</b> so với đám cháy.\n\n" +
                    "<size=13><i><color=#FFD700>[Simulator] Bấm 'Tab' về [HMD] rướn cái đầu lùi ra, HOẶC giữ phím 'K' lùi nguyên cơ thể</color></i></size>"
                );
                Debug.Log($"[Scenario2] Chọn đúng bình {extName} - Kỹ thuật 1: Kiểm tra khoảng cách");
            }
            else
            {
                m_Score = Mathf.Max(0, m_Score - m_WrongChoicePenalty);
                string wrongMsg = CurrentPhase == 1 
                    ? " Bình bột ABC không nên dùng cho đồ điện tử tản nhiệt như tủ lạnh (dễ hỏng mạch).\nHãy chọn bình CO₂."
                    : " Bình khí CO₂ không dập triệt để cháy nệm/chất rắn phòng ngủ (dễ ngạt, cháy lại).\nHãy chọn bình bột ABC.";
                m_HUD?.ShowWarning(wrongMsg, 4.5f);
                Debug.Log($"[Scenario2] Chọn sai bình - Trừ {m_WrongChoicePenalty} điểm. Điểm còn: {m_Score}");
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
                m_HUD?.ShowWarning($" Quá gần! Hãy lùi ra xa hơn (cần ≥ {m_MinFireDistance}m).", 1.5f);
                return false;
            }
            else if (distance > m_MaxFireDistance)
            {
                m_HUD?.ShowWarning($" Quá xa! Hãy tiến lại gần hơn (cần ≤ {m_MaxFireDistance}m).", 1.5f);
                return false;
            }
            else
            {
                SetState(ScenarioState.PullPin);
                m_HUD?.ShowStep(
                    "<b> Khoảng cách tốt!</b>\n" +
                    "Kỹ thuật 2: Rút chốt an toàn của bình ở cụm van trên đỉnh bình.\n\n" +
                    "<size=14><i><color=#FFD700>[Simulator] Bấm phím 'Tab' đổi sang [Left Controller], đưa 2 tay chạm vào chốt bình rồi ngắt phím 'G' lần nữa</color></i></size>"
                );
                Debug.Log("[Scenario2]  Khoảng cách OK - Kỹ thuật 2: Rút chốt");
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
                "<b> Đã rút chốt!</b>\n" +
                "Kỹ thuật 3: Hướng vòi phun vào <b>gốc lửa</b>.\n\n" +
                "<size=14><i><color=#FFD700>[Simulator] Bấm 'Tab' qua lại [Right Controller]. Nhấn GIỮ CHUỘT PHẢI xoay tay chĩa vòi xuống (Gắn tia Xanh Lá)</color></i></size>"
            );
            Debug.Log("[Scenario2]Đã rút chốt - Kỹ thuật 3: Hướng vòi");
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
                    "<b> Đang nhắm đúng gốc lửa!</b>\n" +
                    "Kỹ thuật 4: Bóp cò.\n\n" +
                    "<size=15><i><color=#FFFE00>[Simulator] Đang dùng tay phải -> Hãy nhấp GIỮ CHUỘT TRÁI liên tục 5 giây không thả!</color></i></size>"
                );
                Debug.Log("[Scenario2] Nhắm đúng - Kỹ thuật 4: Phun");
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

            if (CurrentPhase == 1)
            {
                // Hoàn thành Phase 1
                m_TimerRunning = false; 
                SetState(ScenarioState.Idle);
                m_HUD?.ShowStep(
                    "<b>✅ TUYỆT VỜI! Đã dập tắt đám cháy tủ lạnh.</b>\n" +
                    "Nhưng khoan đã... có tiếng khét nổ phát ra từ phòng ngủ!"
                );
                Debug.Log($"[Scenario2] Hoàn thành Phase 1. Chuyển sang Phase 2...");
                
                CurrentPhase = 2; // Chuyển Phase
                StartCoroutine(TriggerPhase2Delayed());
            }
            else
            {
                // Hoàn thành Phase 2 tổng thể
                m_TimerRunning = false;
                SetState(ScenarioState.Success);

                m_HUD?.ShowSuccess(m_Score);
                m_HUD?.ShowLegalNote(
                    "Theo <b>Thông tư 150/2020/TT-BCA</b> và <b>TCVN 7435</b>:\n" +
                    "Bảo dưỡng bình chữa cháy định kỳ 1 năm/lần.\n" +
                    "Bình ABC dùng cho đám cháy rắn lỏng khí, Bình CO2 tốt cho thiết bị điện!"
                );

                OnScenarioSuccess?.Invoke(m_Score);
                Debug.Log($"[Scenario2] DẬP LỬA TỔNG THỂ THÀNH CÔNG! Điểm cuối: {m_Score}");
            }
        }

        IEnumerator TriggerPhase2Delayed()
        {
            yield return new WaitForSeconds(4f);
            if (m_BedroomFire != null) m_BedroomFire.Ignite();
            else Debug.LogWarning("[Scenario2] Chưa kéo file BedroomFire vào Manager!");
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
            CurrentPhase  = 1;
            SetState(ScenarioState.Idle);
            m_FridgeFire?.ResetFire();
            m_BedroomFire?.ResetFire();
            m_HUD?.ResetHUD();
            Debug.Log("[Scenario2] Kịch bản đã được đặt lại.");
        }
    }
}
