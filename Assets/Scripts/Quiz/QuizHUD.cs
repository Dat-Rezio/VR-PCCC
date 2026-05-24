using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace VRPCCC.Quiz
{
    /// <summary>
    /// Điều khiển toàn bộ hiển thị UI cho hệ thống Quiz Đúng/Sai.
    /// Script này chỉ là "View" — không chứa logic quiz, chỉ nhận lệnh từ QuizManager.
    ///
    /// SETUP TRONG UNITY:
    /// ─────────────────────────────────────────────────────────────────
    ///  1. Tạo Canvas (Render Mode: World Space) → gắn vào Camera Rig hoặc đặt cố định trước mặt người chơi
    ///  2. Tạo cấu trúc con bên trong Canvas:
    ///
    ///     [QuizHUD_Canvas]  ← Canvas (World Space)
    ///       └── [HUDRoot]   ← Panel chứa tất cả (kéo vào m_HUDRoot)
    ///             ├── [QuestionPanel]      ← kéo vào m_QuestionPanel
    ///             │     ├── ScenarioTagText    (TMP) → m_ScenarioTagText
    ///             │     ├── QuestionNumberText (TMP) → m_QuestionNumberText
    ///             │     ├── QuestionBodyText   (TMP) → m_QuestionBodyText
    ///             │     ├── TimerBar           (Slider, tùy chọn) → m_TimerBar
    ///             │     ├── [TrueButton]   ← XR Simple Interactable → gọi QuizManager.SubmitAnswer(true)
    ///             │     │     └── TrueButtonLabel (TMP) → m_TrueBtnLabel
    ///             │     └── [FalseButton]  ← XR Simple Interactable → gọi QuizManager.SubmitAnswer(false)
    ///             │           └── FalseButtonLabel (TMP) → m_FalseBtnLabel
    ///             ├── [FeedbackPanel]      ← kéo vào m_FeedbackPanel
    ///             │     ├── FeedbackResultText  (TMP) → m_FeedbackResultText
    ///             │     └── FeedbackExplainText (TMP) → m_FeedbackExplainText
    ///             └── [ResultPanel]        ← kéo vào m_ResultPanel
    ///                   ├── FinalScoreText   (TMP) → m_FinalScoreText
    ///                   └── FinalSummaryText (TMP) → m_FinalSummaryText
    ///
    ///  3. Gắn script QuizHUD vào [HUDRoot] (hoặc object riêng trong Canvas)
    ///  4. Kéo tất cả tham chiếu vào Inspector
    /// ─────────────────────────────────────────────────────────────────
    /// </summary>
    public class QuizHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────── //

        [Header("Root Container")]
        [Tooltip("Object cha chứa toàn bộ HUD. Ẩn/hiện khi quiz bắt đầu/kết thúc.")]
        [SerializeField] GameObject m_HUDRoot;

        // ── Panel: Câu hỏi ──
        [Header("Panel — Câu Hỏi")]
        [Tooltip("Panel hiển thị câu hỏi và 2 nút ĐÚNG/SAI.")]
        [SerializeField] GameObject m_QuestionPanel;

        [Tooltip("VD: '📍 Kịch bản 2 — Chữa cháy'")]
        [SerializeField] TextMeshProUGUI m_ScenarioTagText;

        [Tooltip("VD: 'Câu 3 / 10'")]
        [SerializeField] TextMeshProUGUI m_QuestionNumberText;

        [Tooltip("Nội dung câu hỏi.")]
        [SerializeField] TextMeshProUGUI m_QuestionBodyText;

        [Tooltip("(Tùy chọn) Thanh đếm ngược thời gian. Ẩn nếu không dùng giới hạn thời gian.")]
        [SerializeField] Slider m_TimerBar;

        [Tooltip("Label trên nút ĐÚNG (có thể tùy chỉnh text).")]
        [SerializeField] TextMeshProUGUI m_TrueBtnLabel;

        [Tooltip("Label trên nút SAI (có thể tùy chỉnh text).")]
        [SerializeField] TextMeshProUGUI m_FalseBtnLabel;

        [Tooltip("GameObject chứa nút ĐÚNG — để disable sau khi trả lời.")]
        [SerializeField] GameObject m_TrueButtonObj;

        [Tooltip("GameObject chứa nút SAI — để disable sau khi trả lời.")]
        [SerializeField] GameObject m_FalseButtonObj;

        // ── Panel: Feedback ──
        [Header("Panel — Feedback")]
        [Tooltip("Panel hiển thị kết quả sau khi người chơi trả lời.")]
        [SerializeField] GameObject m_FeedbackPanel;

        [Tooltip("Text kết quả: 'ĐÚNG!' hoặc 'SAI!'")]
        [SerializeField] TextMeshProUGUI m_FeedbackResultText;

        [Tooltip("Text giải thích chi tiết lý do đúng/sai.")]
        [SerializeField] TextMeshProUGUI m_FeedbackExplainText;

        [Tooltip("(Tùy chọn) Image nền của FeedbackPanel — đổi màu xanh/đỏ theo kết quả.")]
        [SerializeField] Image m_FeedbackBackground;

        // ── Panel: Kết quả cuối ──
        [Header("Panel — Kết Quả Cuối")]
        [Tooltip("Panel hiển thị điểm số sau khi hoàn thành toàn bộ quiz.")]
        [SerializeField] GameObject m_ResultPanel;

        [Tooltip("Text điểm số cuối. VD: 'Điểm: 80 / 100'")]
        [SerializeField] TextMeshProUGUI m_FinalScoreText;

        [Tooltip("Text tổng kết: số câu đúng, số câu sai, đánh giá.")]
        [SerializeField] TextMeshProUGUI m_FinalSummaryText;

        // ── Màu sắc ──
        [Header("Màu Sắc")]
        [SerializeField] Color m_CorrectColor  = new Color(0.18f, 0.80f, 0.44f); // Xanh lá
        [SerializeField] Color m_WrongColor    = new Color(0.91f, 0.30f, 0.24f); // Đỏ
        [SerializeField] Color m_NeutralColor  = new Color(0.20f, 0.60f, 1.00f); // Xanh dương (câu hỏi)

        // ── Nội dung tùy chỉnh ──
        [Header("Nội Dung Tùy Chỉnh")]
        [SerializeField] string m_TrueBtnText  = "ĐÚNG";
        [SerializeField] string m_FalseBtnText = "SAI";
        [SerializeField] string m_CorrectFeedbackText = "ĐÚNG!";
        [SerializeField] string m_WrongFeedbackText   = "SAI!";

        [Header("Tiền tố nhãn kịch bản (Tùy chỉnh)")]
        [SerializeField] string[] m_ScenarioTagLabels = new string[]
        {
            "Tổng quát",
            "Kịch bản 1 — Kiểm tra nhiệt độ cửa",
            "Kịch bản 2 — Chữa cháy bằng bình",
            "Kịch bản 3 — Thoát hiểm",
            "Kịch bản 4 — Kiểm tra nguy cơ",
        };

        // ──────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────── //

        void Awake()
        {
            // Đặt label nút
            if (m_TrueBtnLabel  != null) m_TrueBtnLabel.text  = m_TrueBtnText;
            if (m_FalseBtnLabel != null) m_FalseBtnLabel.text = m_FalseBtnText;

            HideAll();
        }

        // ──────────────────────────────────────────────────────── //
        //  Public API — được gọi bởi QuizManager
        // ──────────────────────────────────────────────────────── //

        /// <summary>
        /// Hiện HUD và bắt đầu hiển thị câu hỏi.
        /// </summary>
        /// <param name="questionIndex">0-based index</param>
        /// <param name="totalQuestions">Tổng số câu hỏi</param>
        /// <param name="questionText">Nội dung câu hỏi</param>
        /// <param name="scenarioTag">Enum kịch bản (dùng để lấy label)</param>
        /// <param name="timerNormalized">0→1, dùng cho TimerBar. -1 = không dùng timer</param>
        public void ShowQuestion(
            int questionIndex,
            int totalQuestions,
            string questionText,
            QuizQuestionData.ScenarioTag scenarioTag,
            float timerNormalized = -1f)
        {
            EnsureHUDVisible();

            // Ẩn các panel khác
            if (m_FeedbackPanel != null) m_FeedbackPanel.SetActive(false);
            if (m_ResultPanel   != null) m_ResultPanel.SetActive(false);

            // Hiện QuestionPanel
            if (m_QuestionPanel != null) m_QuestionPanel.SetActive(true);

            // Điền nội dung
            if (m_ScenarioTagText != null)
            {
                int tagIndex = (int)scenarioTag;
                m_ScenarioTagText.text = (tagIndex < m_ScenarioTagLabels.Length)
                    ? m_ScenarioTagLabels[tagIndex]
                    : scenarioTag.ToString();
            }

            if (m_QuestionNumberText != null)
                m_QuestionNumberText.text = $"Câu {questionIndex + 1} / {totalQuestions}";

            if (m_QuestionBodyText != null)
            {
                m_QuestionBodyText.text  = questionText;
                m_QuestionBodyText.color = m_NeutralColor;
            }

            // Timer bar
            SetTimerBar(timerNormalized);

            // Bật lại nút (có thể đã bị tắt sau câu trước)
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// Cập nhật thanh đếm ngược thời gian (gọi mỗi frame từ QuizManager nếu có giới hạn thời gian).
        /// </summary>
        public void UpdateTimer(float normalizedValue)
        {
            SetTimerBar(normalizedValue);
        }

        /// <summary>
        /// Hiện panel feedback (kết quả + giải thích) sau khi người chơi trả lời.
        /// Tự động ẩn QuestionPanel và tắt nút.
        /// </summary>
        public void ShowFeedback(bool wasCorrect, string explanation)
        {
            // Tắt nút để tránh bấm tiếp
            SetButtonsInteractable(false);

            // Ẩn QuestionPanel, hiện FeedbackPanel
            if (m_QuestionPanel  != null) m_QuestionPanel.SetActive(false);
            if (m_FeedbackPanel  != null) m_FeedbackPanel.SetActive(true);

            // Kết quả text
            if (m_FeedbackResultText != null)
            {
                m_FeedbackResultText.text  = wasCorrect ? m_CorrectFeedbackText : m_WrongFeedbackText;
                m_FeedbackResultText.color = wasCorrect ? m_CorrectColor : m_WrongColor;
            }

            // Giải thích
            if (m_FeedbackExplainText != null)
                m_FeedbackExplainText.text = explanation;

            // Đổi màu nền (nếu có)
            if (m_FeedbackBackground != null)
            {
                Color bg = wasCorrect ? m_CorrectColor : m_WrongColor;
                bg.a = 0.15f; // Trong suốt nhẹ
                m_FeedbackBackground.color = bg;
            }
        }

        /// <summary>
        /// Hiện panel kết quả cuối quiz. Tự động ẩn các panel khác.
        /// </summary>
        /// <param name="score">Điểm đạt được</param>
        /// <param name="maxScore">Điểm tối đa</param>
        /// <param name="correctCount">Số câu đúng</param>
        /// <param name="totalCount">Tổng số câu</param>
        public void ShowResult(int score, int maxScore, int correctCount, int totalCount)
        {
            if (m_QuestionPanel != null) m_QuestionPanel.SetActive(false);
            if (m_FeedbackPanel != null) m_FeedbackPanel.SetActive(false);
            if (m_ResultPanel   != null) m_ResultPanel.SetActive(true);

            EnsureHUDVisible();

            float accuracy = totalCount > 0 ? (float)correctCount / totalCount : 0f;
            string grade = accuracy >= 0.8f ? "Xuất sắc!" :
                           accuracy >= 0.6f ? "Đạt yêu cầu" :
                                              "Cần ôn tập thêm";

            if (m_FinalScoreText != null)
                m_FinalScoreText.text = $"<b>Điểm: {score} / {maxScore}</b>";

            if (m_FinalSummaryText != null)
                m_FinalSummaryText.text =
                    $"Trả lời đúng: <b>{correctCount} / {totalCount}</b>\n" +
                    $"Tỉ lệ chính xác: <b>{accuracy * 100:F0}%</b>\n\n" +
                    $"{grade}";
        }

        /// <summary>
        /// Ẩn toàn bộ HUD (gọi khi quiz chưa bắt đầu hoặc sau khi người chơi rời trigger zone).
        /// </summary>
        public void HideHUD()
        {
            if (m_HUDRoot != null) m_HUDRoot.SetActive(false);
        }

        /// <summary>
        /// Hiện lại HUD (nếu đang ẩn).
        /// </summary>
        public void ShowHUD()
        {
            EnsureHUDVisible();
        }

        // ──────────────────────────────────────────────────────── //
        //  Internal Helpers
        // ──────────────────────────────────────────────────────── //

        void HideAll()
        {
            if (m_HUDRoot       != null) m_HUDRoot.SetActive(false);
            if (m_QuestionPanel != null) m_QuestionPanel.SetActive(false);
            if (m_FeedbackPanel != null) m_FeedbackPanel.SetActive(false);
            if (m_ResultPanel   != null) m_ResultPanel.SetActive(false);
        }

        void EnsureHUDVisible()
        {
            if (m_HUDRoot != null && !m_HUDRoot.activeSelf)
                m_HUDRoot.SetActive(true);
        }

        void SetButtonsInteractable(bool interactable)
        {
            if (m_TrueButtonObj  != null) m_TrueButtonObj.SetActive(interactable);
            if (m_FalseButtonObj != null) m_FalseButtonObj.SetActive(interactable);
        }

        void SetTimerBar(float normalizedValue)
        {
            if (m_TimerBar == null) return;

            if (normalizedValue < 0f)
            {
                m_TimerBar.gameObject.SetActive(false);
            }
            else
            {
                m_TimerBar.gameObject.SetActive(true);
                m_TimerBar.value = Mathf.Clamp01(normalizedValue);
            }
        }
    }
}
