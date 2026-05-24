using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VRPCCC.Scenario4
{
    /// <summary>
    /// HUD độc lập cho Scenario 4: Kiểm Tra An Toàn PCCC.
    /// Không phụ thuộc vào ScenarioHUD của Scenario 2.
    /// 
    /// 3 giai đoạn hiển thị:
    ///   1. BẮT ĐẦU  — Thông báo "Hãy tìm các vật thể nguy cơ" + số lượng
    ///   2. ĐANG TÌM — Tiến độ (đã tìm / tổng) + số lần chọn sai
    ///   3. KẾT THÚC — Điểm số + Từng giải thích theo vật phẩm
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

        [Tooltip("Nút chuyển sang giải thích vật phẩm tiếp theo.")]
        [SerializeField] Button m_NextExplanationButton;

        [Tooltip("Label của nút chuyển tiếp (tuỳ chọn).")]
        [SerializeField] TextMeshProUGUI m_NextExplanationButtonLabel;

        [Header("Cỡ Chữ Màn Kết Thúc")]
        [Tooltip("Cỡ chữ cho dòng điểm số ở màn kết thúc.")]
        [SerializeField] float m_EndScoreFontSize = 40f;

        [Tooltip("Cỡ chữ tối đa cho phần giải thích chi tiết ở màn kết thúc.")]
        [SerializeField] float m_EndExplanationFontSize = 30f;

        [Tooltip("Cỡ chữ tối thiểu cho phần giải thích chi tiết khi tự co giãn.")]
        [SerializeField] float m_EndExplanationMinFontSize = 22f;

        [Tooltip("Kích thước của EndPanel khi hiển thị ở giữa HUDCanvas.")]
        [SerializeField] Vector2 m_EndPanelSize = new Vector2(650f, 450f);

        List<InspectionScenarioManager.InspectionResult> m_EndResults = new List<InspectionScenarioManager.InspectionResult>();
        int m_EndResultIndex = -1;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            if (m_NextExplanationButton != null)
                m_NextExplanationButton.onClick.AddListener(ShowNextExplanation);

            HideAll();
        }

        void OnDestroy()
        {
            if (m_NextExplanationButton != null)
                m_NextExplanationButton.onClick.RemoveListener(ShowNextExplanation);
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
            CenterEndPanel();

            m_EndResults.Clear();
            m_EndResultIndex = -1;

            if (m_ScoreText != null)
            {
                m_ScoreText.text = scoreText;
                m_ScoreText.fontSize = m_EndScoreFontSize;
            }

            if (m_ExplanationText != null)
            {
                m_ExplanationText.text = explanationText;
                m_ExplanationText.enableAutoSizing = true;
                m_ExplanationText.fontSizeMax = m_EndExplanationFontSize;
                m_ExplanationText.fontSizeMin = m_EndExplanationMinFontSize;
                m_ExplanationText.fontSize = m_EndExplanationFontSize;
            }

            if (m_NextExplanationButton != null)
                m_NextExplanationButton.gameObject.SetActive(false);

            EnsureVisible();
        }

        /// <summary>
        /// Hiện panel kết thúc và cho phép xem giải thích từng vật phẩm bằng nút tiếp.
        /// Chỉ hiển thị những vật phẩm có chứa nội dung giải thích.
        /// </summary>
        public void ShowEnd(string scoreText, List<InspectionScenarioManager.InspectionResult> results)
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(true);
            CenterEndPanel();

            // Lọc: Chỉ đưa vào danh sách những Result có explanation hợp lệ (không rỗng, không chứa toàn dấu cách)
            m_EndResults = new List<InspectionScenarioManager.InspectionResult>();
            if (results != null)
            {
                foreach (var res in results)
                {
                    if (!string.IsNullOrWhiteSpace(res.explanation))
                    {
                        m_EndResults.Add(res);
                    }
                }
            }
            
            m_EndResultIndex = -1;

            if (m_ScoreText != null)
            {
                m_ScoreText.text = scoreText;
                m_ScoreText.fontSize = m_EndScoreFontSize;
            }

            // Nếu không có vật phẩm nào có giải thích, ẩn nút tiếp
            if (m_NextExplanationButton != null)
                m_NextExplanationButton.gameObject.SetActive(m_EndResults.Count > 0);

            ShowNextExplanation();
            EnsureVisible();
        }

        /// <summary>
        /// Chuyển sang giải thích vật phẩm tiếp theo.
        /// </summary>
        public void ShowNextExplanation()
        {
            if (m_EndResults == null || m_EndResults.Count == 0)
            {
                if (m_ExplanationText != null)
                    m_ExplanationText.text = "<b>Không có vật phẩm nào cần giải thích thêm.</b>";

                if (m_NextExplanationButtonLabel != null)
                    m_NextExplanationButtonLabel.text = "Đóng"; // Chuyển thành Đóng vì không có danh sách để xem

                return;
            }

            if (m_EndResultIndex >= m_EndResults.Count - 1)
            {
                CloseEndPanel();
                return;
            }

            m_EndResultIndex++;
            InspectionScenarioManager.InspectionResult result = m_EndResults[m_EndResultIndex];

            if (m_ExplanationText != null)
            {
                string status = result.wasCorrect ? "✅ ĐÚNG" : "❌ SAI";
                
                // Đã lọc danh sách ở ShowEnd nên không cần check string rỗng nữa
                string detail = result.explanation;

                m_ExplanationText.text =
                    $"<b>{status}: {result.itemName}</b>\n" +
                    $"<size=85%>{detail}</size>\n\n" +
                    $"<i>{m_EndResultIndex + 1}/{m_EndResults.Count}</i>";

                m_ExplanationText.enableAutoSizing = true;
                m_ExplanationText.fontSizeMax = m_EndExplanationFontSize;
                m_ExplanationText.fontSizeMin = m_EndExplanationMinFontSize;
                m_ExplanationText.fontSize = m_EndExplanationFontSize;
            }

            if (m_NextExplanationButtonLabel != null)
                m_NextExplanationButtonLabel.text = (m_EndResultIndex >= m_EndResults.Count - 1) ? "Đóng" : "Tiếp";
        }

        /// <summary>
        /// Reset HUD về trạng thái ban đầu (ẩn tất cả).
        /// </summary>
        public void ResetHUD()
        {
            m_EndResults.Clear();
            m_EndResultIndex = -1;
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

        void CenterEndPanel()
        {
            if (m_EndPanel == null)
                return;

            RectTransform endRect = m_EndPanel.transform as RectTransform;
            if (endRect == null)
                return;

            endRect.anchorMin = new Vector2(0.5f, 0.5f);
            endRect.anchorMax = new Vector2(0.5f, 0.5f);
            endRect.pivot = new Vector2(0.5f, 0.5f);
            endRect.anchoredPosition = Vector2.zero;
            endRect.sizeDelta = m_EndPanelSize;
            endRect.localScale = Vector3.one;
        }

        void CloseEndPanel()
        {
            if (m_EndPanel != null)
                m_EndPanel.SetActive(false);

            if (m_NextExplanationButtonLabel != null)
                m_NextExplanationButtonLabel.text = "Tiếp";
        }

        void EnsureVisible()
        {
            if (m_HUDContainer != null && !m_HUDContainer.activeSelf)
                m_HUDContainer.SetActive(true);
        }
    }
}
