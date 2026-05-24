using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VRPCCC.Scenario4
{
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
        //  Panel chính
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Panel Chính (Bắt đầu & Tiến độ)")]
        [SerializeField] GameObject m_MainPanel;
        [SerializeField] TextMeshProUGUI m_MainText;

        // ──────────────────────────────────────────────────────────────────── //
        //  Panel kết thúc
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Panel Kết Thúc (Điểm + Giải thích)")]
        [SerializeField] GameObject m_EndPanel;
        [SerializeField] TextMeshProUGUI m_ScoreText;
        [SerializeField] TextMeshProUGUI m_ExplanationText;

        [Header("Điều khiển Màn Kết Thúc")]
        [Tooltip("Nút chuyển sang giải thích vật phẩm tiếp theo.")]
        [SerializeField] Button m_NextExplanationButton;
        [SerializeField] TextMeshProUGUI m_NextExplanationButtonLabel;

        [Tooltip("Nút quay lại xem vật phẩm trước đó.")]
        [SerializeField] Button m_PrevExplanationButton; // THÊM NÚT QUAY LẠI Ở ĐÂY

        List<InspectionScenarioManager.InspectionResult> m_EndResults = new List<InspectionScenarioManager.InspectionResult>();
        int m_EndResultIndex = -1;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            if (m_NextExplanationButton != null)
                m_NextExplanationButton.onClick.AddListener(ShowNextExplanation);

            // Lắng nghe sự kiện cho nút Quay lại
            if (m_PrevExplanationButton != null)
                m_PrevExplanationButton.onClick.AddListener(ShowPrevExplanation);

            HideAll();
        }

        void OnDestroy()
        {
            if (m_NextExplanationButton != null)
                m_NextExplanationButton.onClick.RemoveListener(ShowNextExplanation);

            if (m_PrevExplanationButton != null)
                m_PrevExplanationButton.onClick.RemoveListener(ShowPrevExplanation);
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

        public void ToggleHUD()
        {
            if (m_HUDContainer != null)
            {
                bool isActive = m_HUDContainer.activeSelf;
                m_HUDContainer.SetActive(!isActive);
            }
        }

        public void ShowMain(string message)
        {
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
            if (m_MainPanel != null) m_MainPanel.SetActive(true);
            if (m_MainText != null) m_MainText.text = message;

            EnsureVisible();
        }

        public void ShowEnd(string scoreText, string explanationText)
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(true);

            m_EndResults.Clear();
            m_EndResultIndex = -1;

            if (m_ScoreText != null) m_ScoreText.text = scoreText;
            if (m_ExplanationText != null) m_ExplanationText.text = explanationText;

            if (m_NextExplanationButton != null) m_NextExplanationButton.gameObject.SetActive(false);
            if (m_PrevExplanationButton != null) m_PrevExplanationButton.gameObject.SetActive(false);

            EnsureVisible();
        }

        public void ShowEnd(string scoreText, List<InspectionScenarioManager.InspectionResult> results)
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(true);

            // Lọc bỏ các vật phẩm không có nội dung giải thích
            m_EndResults = new List<InspectionScenarioManager.InspectionResult>();
            if (results != null)
            {
                foreach (var res in results)
                {
                    if (!string.IsNullOrWhiteSpace(res.explanation))
                        m_EndResults.Add(res);
                }
            }
            
            m_EndResultIndex = -1;

            if (m_ScoreText != null) m_ScoreText.text = scoreText;

            if (m_NextExplanationButton != null)
                m_NextExplanationButton.gameObject.SetActive(m_EndResults.Count > 0);

            if (m_PrevExplanationButton != null)
                m_PrevExplanationButton.gameObject.SetActive(false); // Ẩn nút quay lại ở trang đầu tiên

            ShowNextExplanation();
            EnsureVisible();
        }

        /// <summary>
        /// Tiến tới vật phẩm tiếp theo
        /// </summary>
        public void ShowNextExplanation()
        {
            if (m_EndResults == null || m_EndResults.Count == 0)
            {
                if (m_ExplanationText != null)
                    m_ExplanationText.text = "<b>Không có vật phẩm nào cần giải thích thêm.</b>";
                if (m_NextExplanationButtonLabel != null)
                    m_NextExplanationButtonLabel.text = "Đóng";
                return;
            }

            // Nếu đang ở cuối danh sách mà bấm "Đóng" (lúc này Label là Đóng)
            if (m_EndResultIndex >= m_EndResults.Count - 1)
            {
                CloseEndPanel();
                return;
            }

            m_EndResultIndex++;
            UpdateExplanationUI();
        }

        /// <summary>
        /// Lùi về vật phẩm trước đó
        /// </summary>
        public void ShowPrevExplanation()
        {
            if (m_EndResults == null || m_EndResults.Count == 0) return;

            // Nếu đang ở đầu danh sách thì không làm gì cả
            if (m_EndResultIndex <= 0) return;

            m_EndResultIndex--;
            UpdateExplanationUI();
        }

        public void ResetHUD()
        {
            m_EndResults.Clear();
            m_EndResultIndex = -1;
            HideAll();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Cập nhật nội dung Text và trạng thái 2 nút Tiến/Lùi
        /// </summary>
        void UpdateExplanationUI()
        {
            InspectionScenarioManager.InspectionResult result = m_EndResults[m_EndResultIndex];

            if (m_ExplanationText != null)
            {
                string status = result.wasCorrect ? "ĐÚNG" : "SAI";
                string detail = result.explanation;

                m_ExplanationText.text =
                    $"<b>{status}: {result.itemName}</b>\n" +
                    $"<size=85%>{detail}</size>\n\n" +
                    $"<i>{m_EndResultIndex + 1}/{m_EndResults.Count}</i>";
            }

            // Cập nhật nhãn nút Tiếp (Đổi thành "Đóng" nếu đã đến vật phẩm cuối)
            if (m_NextExplanationButtonLabel != null)
                m_NextExplanationButtonLabel.text = (m_EndResultIndex >= m_EndResults.Count - 1) ? "Đóng" : "Tiếp";

            // Hiện nút Quay Lại nếu không phải vật phẩm đầu tiên (Index > 0)
            if (m_PrevExplanationButton != null)
                m_PrevExplanationButton.gameObject.SetActive(m_EndResultIndex > 0);
        }

        void HideAll()
        {
            if (m_MainPanel != null) m_MainPanel.SetActive(false);
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
        }

        void CloseEndPanel()
        {
            if (m_EndPanel != null) m_EndPanel.SetActive(false);
            if (m_NextExplanationButtonLabel != null) m_NextExplanationButtonLabel.text = "Tiếp";
        }

        void EnsureVisible()
        {
            if (m_HUDContainer != null && !m_HUDContainer.activeSelf)
                m_HUDContainer.SetActive(true);
        }
    }
}