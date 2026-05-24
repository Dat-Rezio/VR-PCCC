using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

namespace VRPCCC.Scenario2
{
    public class FirefightingScenarioManager : MonoBehaviour
    {
        public enum ScenarioState
        {
            Idle, ApproachCabinet, SelectEquipment, CheckDistance, PullPin, AimNozzle, Spraying, Success, Failed
        }

        [Header("Tham Chiếu Scene")]
        [SerializeField] FireSource m_FridgeFire;
        [SerializeField] FireSource m_BedroomFire;
        [SerializeField] ScenarioHUD m_HUD;

        [Header("Cấu Hình Di Chuyển Canvas")]
        [Tooltip("Kéo Object Canvas Hướng Dẫn vào đây")]
        public Transform tutorialCanvas; 

        // --- CẬP NHẬT: TÁCH RIÊNG CÁC ĐIỂM NEO THEO TỪNG GIAI ĐOẠN ---
        
        [Header("Điểm Neo Khởi Đầu")]
        public Transform anchor_StartIdle;

        [Header("Điểm Neo Phase 1 (Cháy Tủ Lạnh)")]
        public Transform anchor_FirePhase1;
        public Transform anchor_AtCabinetPhase1;
        public Transform anchor_CheckDistance_Phase1;
        public Transform anchor_PullPin_Phase1;
        public Transform anchor_AimNozzle_Phase1;
        public Transform anchor_Spraying_Phase1;
        public Transform anchor_Phase1Done;

        [Header("Điểm Neo Phase 2 (Cháy Phòng Ngủ)")]
        public Transform anchor_FirePhase2;
        public Transform anchor_AtCabinetPhase2;
        public Transform anchor_CheckDistance_Phase2;
        public Transform anchor_PullPin_Phase2;
        public Transform anchor_AimNozzle_Phase2;
        public Transform anchor_Spraying_Phase2;

        public int CurrentPhase { get; private set; } = 1;
        public FireSource GetActiveFire() => CurrentPhase == 1 ? m_FridgeFire : m_BedroomFire;

        [Header("Cài Đặt Điểm Số & Khoảng Cách")]
        [SerializeField] int m_InitialScore = 100;
        [SerializeField] int m_WrongChoicePenalty = 20;
        [SerializeField] float m_ScenarioTimeLimit = 0f;
        [SerializeField] public float m_MinFireDistance = 2f;
        [SerializeField] public float m_MaxFireDistance = 3f;

        [Header("Nội Dung Hướng Dẫn (Có thể tùy chỉnh)")]
        [TextArea(2, 4)] public string txt_StartIdle = "Quan sát môi trường xung quanh...";
        [TextArea(2, 4)] public string txt_FirePhase1 = "<b>🔥 PHÁT HIỆN CHÁY TỦ LẠNH KHU BẾP!</b>\nTiếp cận tủ PCCC.";
        [TextArea(2, 4)] public string txt_FirePhase2 = "<b>🔥 PHÁT HIỆN CHÁY TRONG PHÒNG NGỦ!</b>\nTiếp cận tủ PCCC.";
        [TextArea(2, 4)] public string txt_AtCabinetPhase1 = "<b>Đã đến tủ PCCC.</b>\nHãy chọn ĐÚNG bình khí CO₂ (màu đen).";
        [TextArea(2, 4)] public string txt_AtCabinetPhase2 = "<b>Đã đến tủ PCCC.</b>\nHãy chọn ĐÚNG bình bột ABC (màu đỏ).";
        [TextArea(2, 4)] public string txt_CheckDistance = "<b>Đã cầm bình!</b>\nDi chuyển đến khoảng cách 2–3m so với đám cháy.";
        [TextArea(2, 4)] public string txt_PullPin = "<b>Khoảng cách tốt!</b>\nHãy rút chốt an toàn của bình.";
        [TextArea(2, 4)] public string txt_AimNozzle = "<b>Đã rút chốt!</b>\nHướng vòi phun vào <b>gốc lửa</b>.";
        [TextArea(2, 4)] public string txt_Spraying = "<b>Đang nhắm đúng gốc lửa!</b>\nBóp cò và giữ liên tục để dập lửa.";
        [TextArea(2, 4)] public string txt_Phase1Done = "<b>✅ TUYỆT VỜI! Đã dập tắt đám cháy tủ lạnh.</b>\nNhưng khoan đã... có tiếng khét nổ phát ra từ phòng ngủ!";

        [Header("Events")]
        public UnityEvent OnScenarioStart;
        public UnityEvent<int> OnScenarioSuccess;   
        public UnityEvent OnScenarioFailed;

        ScenarioState m_State = ScenarioState.Idle;
        int           m_Score;
        float         m_ElapsedTime;
        bool          m_TimerRunning;
        bool          m_IsPinAlreadyPulled = false;

        public ScenarioState CurrentState => m_State;
        public int Score => m_Score;

        void Awake() { m_Score = m_InitialScore; }

        void Start()
        {
            CurrentPhase = 1;
            SetState(ScenarioState.Idle);
            ShowStepAtAnchor(txt_StartIdle, anchor_StartIdle);
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

        private void ShowStepAtAnchor(string text, Transform anchor)
        {
            m_HUD?.ShowStep(text);
            if (tutorialCanvas != null && anchor != null)
            {
                tutorialCanvas.position = anchor.position;
                tutorialCanvas.rotation = anchor.rotation;
            }
        }

        public void OnFireIgnited()
        {
            if (m_State != ScenarioState.Idle) return;
            SetState(ScenarioState.ApproachCabinet);
            m_TimerRunning = true;
            OnScenarioStart?.Invoke();

            if (CurrentPhase == 1) ShowStepAtAnchor(txt_FirePhase1, anchor_FirePhase1);
            else ShowStepAtAnchor(txt_FirePhase2, anchor_FirePhase2);
        }

        public void OnPlayerApproachCabinet()
        {
            if (m_State != ScenarioState.ApproachCabinet) return;
            SetState(ScenarioState.SelectEquipment);
            
            if (CurrentPhase == 1) ShowStepAtAnchor(txt_AtCabinetPhase1, anchor_AtCabinetPhase1);
            else ShowStepAtAnchor(txt_AtCabinetPhase2, anchor_AtCabinetPhase2);
        }

        public void OnExtinguisherGrabbed(bool isCO2)
        {
            if (m_State != ScenarioState.SelectEquipment) return;
            bool isCorrect = (CurrentPhase == 1 && isCO2) || (CurrentPhase == 2 && !isCO2);

            if (isCorrect)
            {
                SetState(ScenarioState.CheckDistance);
                
                // Lựa chọn điểm neo dựa theo Phase hiện tại
                Transform anchor = CurrentPhase == 1 ? anchor_CheckDistance_Phase1 : anchor_CheckDistance_Phase2;
                ShowStepAtAnchor(txt_CheckDistance, anchor);
            }
            else
            {
                m_Score = Mathf.Max(0, m_Score - m_WrongChoicePenalty);
                string wrongMsg = CurrentPhase == 1 
                    ? "Bình bột ABC dễ làm hỏng mạch tủ lạnh. Hãy chọn loại bình khí CO2."
                    : "Bình CO2 không dập triệt để cháy nệm. Hãy chọn bình bột ABC.";
                m_HUD?.ShowWarning(wrongMsg, 4.5f);
            }
        }

        public bool OnDistanceCheck(float distance)
        {
            if (m_State != ScenarioState.CheckDistance) return false;

            if (distance < m_MinFireDistance)
            {
                m_HUD?.ShowWarning($"Quá gần! Hãy lùi ra (khoảng cách ≥ {m_MinFireDistance}m).", 1.5f);
                return false;
            }
            else if (distance > m_MaxFireDistance)
            {
                m_HUD?.ShowWarning($"Quá xa! Hãy tiến lại (khoảng cách ≤ {m_MaxFireDistance}m).", 1.5f);
                return false;
            }
            else
            {
                if (m_IsPinAlreadyPulled)
                {
                    SetState(ScenarioState.AimNozzle);
                    Transform anchor = CurrentPhase == 1 ? anchor_AimNozzle_Phase1 : anchor_AimNozzle_Phase2;
                    ShowStepAtAnchor("<b>Khoảng cách tốt và chốt đã mở!</b>\n" + txt_AimNozzle, anchor);
                }
                else
                {
                    SetState(ScenarioState.PullPin);
                    Transform anchor = CurrentPhase == 1 ? anchor_PullPin_Phase1 : anchor_PullPin_Phase2;
                    ShowStepAtAnchor(txt_PullPin, anchor);
                }
                return true;
            }
        }

        public void OnPinPulled()
        {
            m_IsPinAlreadyPulled = true; 

            if (m_State == ScenarioState.PullPin)
            {
                SetState(ScenarioState.AimNozzle);
                Transform anchor = CurrentPhase == 1 ? anchor_AimNozzle_Phase1 : anchor_AimNozzle_Phase2;
                ShowStepAtAnchor(txt_AimNozzle, anchor);
            }
        }

        public void OnNozzleAimedUpdate(bool onTarget)
        {
            if (m_State != ScenarioState.AimNozzle) return;
            if (onTarget)
            {
                SetState(ScenarioState.Spraying);
                Transform anchor = CurrentPhase == 1 ? anchor_Spraying_Phase1 : anchor_Spraying_Phase2;
                ShowStepAtAnchor(txt_Spraying, anchor);
            }
        }

        public void OnSprayProgress(float progress)
        {
            if (m_State != ScenarioState.Spraying) return;
        }

        public void OnFireExtinguished()
        {
            if (m_State == ScenarioState.Success) return;

            if (CurrentPhase == 1)
            {
                m_TimerRunning = false; 
                SetState(ScenarioState.Idle);
                ShowStepAtAnchor(txt_Phase1Done, anchor_Phase1Done);
                CurrentPhase = 2; 
                StartCoroutine(TriggerPhase2Delayed());
            }
            else
            {
                m_TimerRunning = false;
                SetState(ScenarioState.Success);
                m_HUD?.ShowSuccess(m_Score);
                m_HUD?.ShowLegalNote("");
                OnScenarioSuccess?.Invoke(m_Score);
            }
        }

        IEnumerator TriggerPhase2Delayed()
        {
            yield return new WaitForSeconds(4f);
            m_IsPinAlreadyPulled = false; 
            if (m_BedroomFire != null) m_BedroomFire.Ignite();
        }

        void SetState(ScenarioState newState)
        {
            m_State = newState;
        }

        [ContextMenu("Reset Scenario")]
        public void ResetScenario()
        {
            m_Score = m_InitialScore;
            m_ElapsedTime = 0f;
            m_TimerRunning = false;
            m_IsPinAlreadyPulled = false; 
            CurrentPhase = 1;
            SetState(ScenarioState.Idle);
            ShowStepAtAnchor(txt_StartIdle, anchor_StartIdle);
            m_FridgeFire?.ResetFire();
            m_BedroomFire?.ResetFire();
            m_HUD?.ResetHUD();
        }
    }
}