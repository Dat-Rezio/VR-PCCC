using UnityEngine;
using System.Collections;
using VRPCCC.Scenario2; // Gọi namespace này để dùng lại ScenarioHUD và FireSource
using UnityEngine.XR.Interaction.Toolkit; // Dùng cho TeleportationProvider

namespace VRPCCC.Scenario3
{
    public class EscapeSceneManager : MonoBehaviour
    {
        public enum EscapeState { Discovery, AttemptExtinguish, Escaping, Evacuating, Finished }

        [Header("Tham chiếu Hệ thống")]
        [Tooltip("Kéo script ScenarioHUD vào đây")]
        [SerializeField] ScenarioHUD m_HUD;
        
        [Tooltip("Kéo object đám cháy vào đây")]
        [SerializeField] FireSource m_FireSource;
        
        [Tooltip("Kéo AudioSource chứa tiếng chuông báo cháy tòa nhà vào đây")]
        [SerializeField] AudioSource m_FireAlarmAudio;

        [Header("Hệ thống Dịch chuyển (Respawn)")]
        [Tooltip("Kéo Teleportation Provider từ XR Origin vào đây")]
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider m_TeleportationProvider;

        [Tooltip("Điểm người chơi sẽ bị đưa về để chọn lại (thường là ở hành lang)")]
        [SerializeField] Transform m_RestartChoicePoint;

        [Header("Cài đặt Thời gian")]
        [Tooltip("Thời gian cho phép xịt lửa trước khi lửa bùng to và bắt đầu thoát hiểm (giây)")]
        [SerializeField] float m_ExtinguishAttemptTime = 4f;

        [Header("Nội dung Hướng dẫn (Tùy chỉnh)")]
        [TextArea(2, 4)] public string txt_Discovery = "<b>🔥 PHÁT HIỆN ĐÁM CHÁY!</b>\nHãy dùng bình chữa cháy để cố gắng dập lửa.";
        [TextArea(2, 4)] public string txt_Escalation = "<b>⚠️ NGUY HIỂM! Lửa đã vượt ngoài tầm kiểm soát!</b>\nBỏ lại bình chữa cháy. Chạy ra hành lang và <b>Ấn Nút Báo Cháy</b>!";
        [TextArea(2, 4)] public string txt_Evacuating = "<b>🔔 ĐÃ BÁO ĐỘNG!</b>\nTìm lối thoát hiểm gần nhất. KHÔNG sử dụng thang máy!";
        [TextArea(2, 4)] public string txt_ElevatorFail = "<b>❌ THẤT BẠI: SỬ DỤNG THANG MÁY</b>\nTuyệt đối không dùng thang máy khi cháy! Hệ thống điện có thể ngắt gây kẹt và ngạt khói. Hãy dùng thang bộ!";
        [TextArea(2, 4)] public string txt_StairsSuccess = "<b>✅ THOÁT HIỂM THÀNH CÔNG!</b>\nBạn đã chọn đúng lối thoát hiểm thang bộ và tuân thủ nguyên tắc PCCC.";

        private EscapeState m_State = EscapeState.Discovery;
        private bool m_IsSpraying = false;

        void Start()
        {
            m_State = EscapeState.Discovery;
            m_HUD?.ShowStep(txt_Discovery);
            
            // Đảm bảo lửa luôn cháy ngay từ đầu scene
            if (m_FireSource != null && !m_FireSource.IsActive)
            {
                m_FireSource.Ignite();
            }
        }

        // --- BƯỚC 2: NGƯỜI CHƠI BẮT ĐẦU XỊT LỬA ---
        // Hàm này sẽ được gọi từ UnityEvent của bình chữa cháy
        public void OnPlayerStartSpraying()
        {
            if (m_State == EscapeState.Discovery && !m_IsSpraying)
            {
                m_IsSpraying = true;
                m_State = EscapeState.AttemptExtinguish;
                StartCoroutine(EscalateFireRoutine());
                Debug.Log("[Scene3] Người chơi bắt đầu xịt. Đếm ngược thời gian lửa bùng to...");
            }
        }

        // --- BƯỚC 3: LỬA LAN TO HƠN ---
        IEnumerator EscalateFireRoutine()
        {
            // Chờ người chơi xịt 1 lúc (tạo cảm giác bất lực)
            yield return new WaitForSeconds(m_ExtinguishAttemptTime);

            m_State = EscapeState.Escaping;
            Debug.Log("[Scene3] Lửa bùng to! Chuyển sang pha thoát hiểm.");

            m_HUD?.ShowWarning("Bình chữa cháy không còn tác dụng! Lửa đang lan quá nhanh!", 4f);
            
            yield return new WaitForSeconds(2.5f);
            
            // --- KIỂM TRA CỜ GHI NHỚ Ở ĐÂY ---
            if (m_IsAlarmPressedEarly)
            {
                // Nếu đã ấn chuông từ trước -> Nhảy cóc thẳng sang pha tìm lối thoát
                m_State = EscapeState.Evacuating;
                m_HUD?.ShowStep("(Bạn đã báo động từ trước)\n" + txt_Evacuating);
            }
            else
            {
                // Nếu chưa ấn -> Bắt chạy ra ấn theo kịch bản bình thường
                m_HUD?.ShowStep(txt_Escalation);
            }
        }

        // --- BƯỚC 4: ẤN NÚT BÁO CHÁY ---
        private bool m_IsAlarmPressedEarly = false;
        public void OnAlarmButtonPressed()
        {
            // 1. Luôn ghi nhớ là người chơi ĐÃ ấn chuông và rú chuông ngay lập tức
            m_IsAlarmPressedEarly = true;
            
            if (m_FireAlarmAudio != null && !m_FireAlarmAudio.isPlaying)
            {
                m_FireAlarmAudio.Play();
            }
            Debug.Log("[Scene3] Nút báo cháy đã được kích hoạt!");

            // 2. Nếu đang ở đúng bước yêu cầu ấn chuông, thì cho đi tiếp ngay
            if (m_State == EscapeState.Escaping)
            {
                m_State = EscapeState.Evacuating;
                m_HUD?.ShowStep(txt_Evacuating);
            }
        }

        // --- CẬP NHẬT BƯỚC 5A: CHỌN THANG MÁY (CẢNH BÁO & DỊCH CHUYỂN) ---
        public void OnElevatorSelected()
        {
            if (m_State == EscapeState.Evacuating)
            {
                // 1. Hiện cảnh báo thất bại trên HUD
                m_HUD?.ShowWarning(txt_ElevatorFail, 8f);
                
                // 2. Thực hiện dịch chuyển người chơi về vị trí cũ
                ExecuteRespawn();

                Debug.Log("[Scene3] Người chơi chọn nhầm thang máy. Đã dịch chuyển về điểm chọn lại.");
            }
        }

        private void ExecuteRespawn()
        {
            if (m_TeleportationProvider != null && m_RestartChoicePoint != null)
            {
                UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
                {
                    destinationPosition = m_RestartChoicePoint.position,
                    destinationRotation = m_RestartChoicePoint.rotation,
                    matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward
                };
                
                m_TeleportationProvider.QueueTeleportRequest(request);
            }
        }

        // --- BƯỚC 5B: CHỌN THANG BỘ (THÀNH CÔNG) ---
        public void OnStairsSelected()
        {
            if (m_State == EscapeState.Evacuating)
            {
                m_State = EscapeState.Finished;
                m_HUD?.ShowSuccess(100); 
                m_HUD?.ShowLegalNote(txt_StairsSuccess);
                Debug.Log("[Scene3] Người chơi chọn đúng thang bộ. Thoát hiểm thành công!");
            }
        }
    }
}