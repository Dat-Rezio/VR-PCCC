using UnityEngine;
using System.Collections;
using TMPro;

namespace VRPCCC.Scenario2
{
    /// <summary>
    /// Quản lý toàn bộ UI hiển thị cho kịch bản "Chống cháy tại chỗ".
    ///
    /// Các panel:
    ///   • stepPanel    – Hướng dẫn bước hiện tại (xuất hiện liên tục)
    ///   • warningPanel – Cảnh báo/hướng dẫn nhanh (tự mất sau vài giây)
    ///   • endPanel     – Màn hình kết quả (thành công / thất bại)
    ///
    /// Gắn script này lên Canvas (World Space hoặc Screen Space - Overlay).
    /// </summary>
    public class ScenarioHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Panel – Hướng Dẫn Bước")]
        [Tooltip("GameObject chứa panel hướng dẫn bước hiện tại.")]
        [SerializeField] GameObject m_StepPanel;

        [Tooltip("TextMeshPro hiển thị nội dung bước hiện tại.")]
        [SerializeField] TextMeshProUGUI m_StepText;

        [Header("Panel – Cảnh Báo Nhanh")]
        [Tooltip("GameObject chứa panel cảnh báo tạm thời.")]
        [SerializeField] GameObject m_WarningPanel;

        [Tooltip("TextMeshPro hiển thị nội dung cảnh báo.")]
        [SerializeField] TextMeshProUGUI m_WarningText;

        [Header("Panel – Kết Thúc")]
        [Tooltip("GameObject của màn hình kết thúc.")]
        [SerializeField] GameObject m_EndPanel;

        [Tooltip("Tiêu đề kết quả (DẬP LỬA THÀNH CÔNG! hoặc THẤT BẠI).")]
        [SerializeField] TextMeshProUGUI m_EndTitleText;

        [Tooltip("Điểm số cuối cùng.")]
        [SerializeField] TextMeshProUGUI m_ScoreText;

        [Tooltip("Trích dẫn văn bản pháp lý.")]
        [SerializeField] TextMeshProUGUI m_LegalNoteText;

        [Header("Bộ Đếm Tiến Độ (tùy chọn)")]
        [Tooltip("Thanh progress bar cuộn từ trái sang phải cho tiến trình dập lửa.")]
        [SerializeField] UnityEngine.UI.Slider m_ExtinguishProgressBar;

        [Header("Hiệu ứng Animation")]
        [Tooltip("Animator trên EndPanel để chạy animation xuất hiện.")]
        [SerializeField] Animator m_EndPanelAnimator;

        [Tooltip("Tên trigger animation thành công.")]
        [SerializeField] string m_SuccessTrigger = "ShowSuccess";

        [Header("Cài Đặt Màu Sắc")]
        [Tooltip("Màu văn bản khi cảnh báo nguy hiểm.")]
        [SerializeField] Color m_WarningColor = new Color(1f, 0.4f, 0f);

        [Tooltip("Màu nền panel cảnh báo.")]
        [SerializeField] Color m_WarningBgColor = new Color(0.8f, 0.1f, 0.05f, 0.85f);

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        Coroutine m_WarningCoroutine;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            HideAll();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>Cập nhật nội dung hướng dẫn bước hiện tại.</summary>
        public void ShowStep(string message)
        {
            if (m_StepPanel != null) m_StepPanel.SetActive(true);
            if (m_StepText  != null) m_StepText.text = message;
        }

        /// <summary>
        /// Hiển thị cảnh báo tạm thời. Tự mất sau <paramref name="duration"/> giây.
        /// </summary>
        public void ShowWarning(string message, float duration = 3f)
        {
            if (m_WarningPanel == null) return;

            // Dừng coroutine cũ nếu đang chạy
            if (m_WarningCoroutine != null)
                StopCoroutine(m_WarningCoroutine);

            m_WarningPanel.SetActive(true);

            if (m_WarningText != null)
            {
                m_WarningText.text  = message;
                m_WarningText.color = m_WarningColor;
            }

            m_WarningCoroutine = StartCoroutine(HideWarningAfterDelay(duration));
        }

        /// <summary>Hiển thị màn hình kết thúc thành công.</summary>
        public void ShowSuccess(int score)
        {
            if (m_StepPanel   != null) m_StepPanel.SetActive(false);
            if (m_WarningPanel != null) m_WarningPanel.SetActive(false);
            if (m_EndPanel    != null) m_EndPanel.SetActive(true);

            if (m_EndTitleText != null)
            {
                m_EndTitleText.text  = "🎉 DẬP LỬA THÀNH CÔNG!";
                m_EndTitleText.color = Color.green;
            }

            if (m_ScoreText != null)
                m_ScoreText.text = $"Điểm số: <b>{score}</b> / 100";

            if (m_EndPanelAnimator != null && !string.IsNullOrEmpty(m_SuccessTrigger))
                m_EndPanelAnimator.SetTrigger(m_SuccessTrigger);
        }

        /// <summary>Hiển thị màn hình thất bại (hết thời gian).</summary>
        public void ShowFailed()
        {
            if (m_StepPanel   != null) m_StepPanel.SetActive(false);
            if (m_WarningPanel != null) m_WarningPanel.SetActive(false);
            if (m_EndPanel    != null) m_EndPanel.SetActive(true);

            if (m_EndTitleText != null)
            {
                m_EndTitleText.text  = "❌ CHƯA HOÀN THÀNH";
                m_EndTitleText.color = Color.red;
            }
        }

        /// <summary>Hiển thị trích dẫn văn bản pháp lý sau khi thành công.</summary>
        public void ShowLegalNote(string text)
        {
            if (m_LegalNoteText != null)
            {
                m_LegalNoteText.gameObject.SetActive(true);
                m_LegalNoteText.text = text;
            }
        }

        /// <summary>Cập nhật thanh tiến trình dập lửa (0–1).</summary>
        public void UpdateExtinguishProgress(float progress)
        {
            if (m_ExtinguishProgressBar != null)
                m_ExtinguishProgressBar.value = progress;
        }

        /// <summary>Đặt lại HUD về trạng thái ban đầu.</summary>
        public void ResetHUD()
        {
            HideAll();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal
        // ──────────────────────────────────────────────────────────────────── //

        void HideAll()
        {
            if (m_StepPanel    != null) m_StepPanel.SetActive(false);
            if (m_WarningPanel != null) m_WarningPanel.SetActive(false);
            if (m_EndPanel     != null) m_EndPanel.SetActive(false);

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
