using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem; // BẮT BUỘC: Để sử dụng hệ thống Input mới

namespace VRPCCC.Scenario2
{
    public class ScenarioHUD : MonoBehaviour
    {
        [Header("Cấu hình Bật/Tắt")]
        [Tooltip("Object cha chứa toàn bộ UI (Canvas hoặc Panel chính).")]
        [SerializeField] GameObject m_MainHUDContainer;

        [Tooltip("Nút bấm để bật/tắt (Ví dụ: Menu Button trên tay cầm).")]
        [SerializeField] InputActionReference m_ToggleAction;

        [Header("Panel – Hướng Dẫn Bước")]
        [SerializeField] GameObject m_StepPanel;
        [SerializeField] TextMeshProUGUI m_StepText;

        [Header("Panel – Cảnh Báo Nhanh")]
        [SerializeField] GameObject m_WarningPanel;
        [SerializeField] TextMeshProUGUI m_WarningText;

        [Header("Panel – Kết Thúc")]
        [SerializeField] GameObject m_EndPanel;
        [SerializeField] TextMeshProUGUI m_EndTitleText;
        [SerializeField] TextMeshProUGUI m_ScoreText;
        [SerializeField] TextMeshProUGUI m_LegalNoteText;

        // --- THÊM CÁC BIẾN NÀY ĐỂ TÙY CHỈNH TRÊN UNITY EDITOR ---
        [Header("Nội dung Kết thúc (Tùy chỉnh)")]
        [SerializeField] string m_SuccessTitle = "THÀNH CÔNG!";
        [SerializeField] string m_FailedTitle = "CHƯA HOÀN THÀNH";
        [SerializeField] string m_ScorePrefix = "Điểm số: ";

        [Header("Bộ Đếm Tiến Độ")]
        [SerializeField] UnityEngine.UI.Slider m_ExtinguishProgressBar;

        [Header("Màu Sắc")]
        [SerializeField] Color m_WarningColor = new Color(1f, 0.4f, 0f);

        Coroutine m_WarningCoroutine;

        // ------------------------------------------------------------------ //
        //  Xử lý Input (Toggle)
        // ------------------------------------------------------------------ //

        private void OnEnable()
        {
            if (m_ToggleAction != null)
            {
                m_ToggleAction.action.Enable();
                m_ToggleAction.action.performed += OnToggleTriggered;
            }
        }

        private void OnDisable()
        {
            if (m_ToggleAction != null)
            {
                m_ToggleAction.action.performed -= OnToggleTriggered;
            }
        }

        private void OnToggleTriggered(InputAction.CallbackContext context)
        {
            ToggleHUD();
        }

        public void ToggleHUD()
        {
            if (m_MainHUDContainer != null)
            {
                bool currentState = m_MainHUDContainer.activeSelf;
                m_MainHUDContainer.SetActive(!currentState);
                Debug.Log($"[ScenarioHUD] {(currentState ? "Ẩn" : "Hiện")} bảng hướng dẫn.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Logic cũ (Giữ nguyên các hàm hiển thị)
        // ------------------------------------------------------------------ //

        void Awake()
        {
            HideAll();
        }

        public void ShowStep(string message)
        {
            if (m_StepPanel != null) m_StepPanel.SetActive(true);
            if (m_StepText != null) m_StepText.text = message;

            // Tự động hiện lại HUD nếu đang bị ẩn khi có bước mới
            if (m_MainHUDContainer != null && !m_MainHUDContainer.activeSelf)
                m_MainHUDContainer.SetActive(true);
        }

        public void ShowWarning(string message, float duration = 3f)
        {
            if (m_WarningPanel == null) return;
            if (m_WarningCoroutine != null) StopCoroutine(m_WarningCoroutine);

            m_WarningPanel.SetActive(true);
            if (m_WarningText != null)
            {
                m_WarningText.text = message;
                m_WarningText.color = m_WarningColor;
            }
            m_WarningCoroutine = StartCoroutine(HideWarningAfterDelay(duration));
            
            // Luôn hiện HUD khi có cảnh báo nguy hiểm
            if (m_MainHUDContainer != null && !m_MainHUDContainer.activeSelf)
                m_MainHUDContainer.SetActive(true);
        }

        public void ShowSuccess(int score)
        {
            HideAll();
            if (m_EndPanel != null) m_EndPanel.SetActive(true);
            if (m_EndTitleText != null)
            {
                // SỬA TẠI ĐÂY: Dùng biến thay vì chữ fix cứng
                m_EndTitleText.text  = m_SuccessTitle; 
                //m_EndTitleText.color = Color.green;
            }
            if (m_ScoreText != null) m_ScoreText.text = $"Điểm số: <b>{score}</b> / 100";
        }

        public void ShowFailed()
        {
            HideAll();
            if (m_EndPanel != null) m_EndPanel.SetActive(true);
            if (m_EndTitleText != null)
            {
                // SỬA TẠI ĐÂY: Dùng biến thay vì chữ fix cứng
                m_EndTitleText.text  = m_FailedTitle;
                m_EndTitleText.color = Color.red;
            }
        }

        public void ShowLegalNote(string text)
        {
            if (m_LegalNoteText != null)
            {
                m_LegalNoteText.gameObject.SetActive(true);
                m_LegalNoteText.text = text;
            }
        }

        public void UpdateExtinguishProgress(float progress)
        {
            if (m_ExtinguishProgressBar != null)
                m_ExtinguishProgressBar.value = progress;
        }

        public void ResetHUD()
        {
            HideAll();
        }

        void HideAll()
        {
            if (m_StepPanel != null) m_StepPanel.SetActive(false);
            if (m_WarningPanel != null) m_WarningPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
            if (m_LegalNoteText != null) m_LegalNoteText.gameObject.SetActive(false);
            if (m_ExtinguishProgressBar != null) m_ExtinguishProgressBar.value = 0f;
        }

        IEnumerator HideWarningAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (m_WarningPanel != null) m_WarningPanel.SetActive(false);
        }
    }
}