using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

namespace VRPCCC.Scenario4
{
    /// <summary>
    /// HUD độc lập cho Scenario 4: Kiểm Tra An Toàn PCCC.
    /// Không phụ thuộc vào ScenarioHUD của Scenario 2.
    /// 
    /// 3 giai đoạn hiển thị:
    ///   1. BẮT ĐẦU  — Thông báo "Hãy tìm các vật thể nguy cơ" + số lượng
    ///   2. ĐANG TÌM — Tiến độ (đã tìm / tổng) + số lần chọn sai
    ///   3. KẾT THÚC — Điểm số + Giải thích chi tiết
    /// 
    /// Setup trong Unity:
    ///   1. Tạo Canvas (World Space) gắn theo tay hoặc đầu người chơi
    ///   2. Tạo các Panel con: MainPanel (chứa MainText), EndPanel (chứa ScoreText + ExplanationText)
    ///   3. Kéo các tham chiếu vào Inspector
    /// </summary>
    public class InspectionHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Bật/Tắt HUD
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Cấu hình Bật/Tắt")]
        [Tooltip("Object cha chứa toàn bộ UI (Canvas hoặc Panel chính).")]
        [SerializeField] GameObject m_HUDContainer;

        [Tooltip("Nút bấm để bật/tắt HUD (VD: Menu Button trên tay cầm).")]
        [SerializeField] InputActionReference m_ToggleAction;

        // ──────────────────────────────────────────────────────────────────── //
        //  Panel chính — Hiện ở giai đoạn Bắt đầu & Đang tìm
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Panel Chính (Bắt đầu & Tiến độ)")]
        [Tooltip("Panel chứa nội dung chính (hướng dẫn / tiến độ).")]
        [SerializeField] GameObject m_MainPanel;

        [Tooltip("Text hiển thị nội dung chính.")]
        [SerializeField] TextMeshProUGUI m_MainText;

        // ──────────────────────────────────────────────────────────────────── //
        //  Panel kết thúc — Hiện ở giai đoạn Kết thúc
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Panel Kết Thúc (Điểm + Giải thích)")]
        [Tooltip("Panel chứa kết quả cuối cùng.")]
        [SerializeField] GameObject m_EndPanel;

        [Tooltip("Text hiển thị điểm số.")]
        [SerializeField] TextMeshProUGUI m_ScoreText;

        [Tooltip("Text hiển thị giải thích chi tiết (có scroll nếu dài).")]
        [SerializeField] TextMeshProUGUI m_ExplanationText;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            HideAll();
        }

        void OnEnable()
        {
            if (m_ToggleAction != null)
            {
                m_ToggleAction.action.Enable();
                m_ToggleAction.action.performed += OnToggleTriggered;
            }
        }

        void OnDisable()
        {
            if (m_ToggleAction != null)
            {
                m_ToggleAction.action.performed -= OnToggleTriggered;
            }
        }

        void OnToggleTriggered(InputAction.CallbackContext context)
        {
            ToggleHUD();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Bật/tắt toàn bộ HUD.
        /// </summary>
        public void ToggleHUD()
        {
            if (m_HUDContainer != null)
            {
                bool isActive = m_HUDContainer.activeSelf;
                m_HUDContainer.SetActive(!isActive);
                Debug.Log($"[InspectionHUD] {(isActive ? "Ẩn" : "Hiện")} HUD.");
            }
        }

        /// <summary>
        /// Hiện nội dung trên panel chính (dùng cho giai đoạn Bắt đầu & Tiến độ).
        /// Tự động ẩn panel kết thúc.
        /// </summary>
        public void ShowMain(string message)
        {
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
            if (m_MainPanel != null) m_MainPanel.SetActive(true);
            if (m_MainText != null) m_MainText.text = message;

            EnsureVisible();
        }

        /// <summary>
        /// Hiện panel kết thúc với điểm số và giải thích.
        /// Tự động ẩn panel chính.
        /// </summary>
        /// <param name="scoreText">Nội dung điểm số (VD: "Điểm: 85 / 100")</param>
        /// <param name="explanationText">Nội dung giải thích chi tiết</param>
        public void ShowEnd(string scoreText, string explanationText)
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(true);
            if (m_ScoreText != null) m_ScoreText.text = scoreText;
            if (m_ExplanationText != null) m_ExplanationText.text = explanationText;

            EnsureVisible();
        }

        /// <summary>
        /// Reset HUD về trạng thái ban đầu (ẩn tất cả).
        /// </summary>
        public void ResetHUD()
        {
            HideAll();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal
        // ──────────────────────────────────────────────────────────────────── //

        void HideAll()
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
        }

        void EnsureVisible()
        {
            if (m_HUDContainer != null && !m_HUDContainer.activeSelf)
                m_HUDContainer.SetActive(true);
        }
    }
}
