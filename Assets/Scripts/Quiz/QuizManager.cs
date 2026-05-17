using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace VRPCCC.Quiz
{
    /// <summary>
    /// Quản lý toàn bộ logic hệ thống Quiz Đúng/Sai.
    ///
    /// SETUP TRONG UNITY:
    /// ─────────────────────────────────────────────────────────────────
    ///  1. Tạo Empty GameObject trong scene → đặt tên "QuizManager"
    ///  2. Gắn script này vào
    ///  3. Kéo QuizHUD (đã setup Canvas) vào field [quizHUD]
    ///  4. Kéo QuizQuestionData asset vào field [questionData]
    ///  5. Kết nối 2 nút ĐÚNG/SAI trên Canvas:
    ///       - Nút ĐÚNG:  XR Simple Interactable → Select Entered → QuizManager.SubmitTrue()
    ///       - Nút SAI:   XR Simple Interactable → Select Entered → QuizManager.SubmitFalse()
    ///  6. Kết nối QuizTriggerZone: OnPlayerEnter → QuizManager.StartQuiz()
    /// ─────────────────────────────────────────────────────────────────
    ///
    /// DEBUG (không cần VR):
    ///   Chuột phải vào component trong Inspector → "Start Quiz" / "Simulate True" / "Simulate False"
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────── //

        [Header("Dữ Liệu Câu Hỏi")]
        [Tooltip("ScriptableObject chứa danh sách câu hỏi. Tạo bằng: Create → VR-PCCC → Quiz Question Data")]
        [SerializeField] QuizQuestionData m_QuestionData;

        [Header("Tham Chiếu HUD")]
        [Tooltip("Script QuizHUD đã gắn vào Canvas. Kéo vào đây.")]
        [SerializeField] QuizHUD m_QuizHUD;

        [Header("Cài Đặt Quiz")]
        [Tooltip("Bật để xáo trộn thứ tự câu hỏi mỗi lần chơi.")]
        [SerializeField] bool m_ShuffleQuestions = true;

        [Tooltip("Điểm cộng mỗi câu trả lời đúng.")]
        [SerializeField] int m_PointsPerCorrect = 10;

        [Tooltip("Điểm trừ mỗi câu trả lời sai (nhập số dương). Đặt 0 = không trừ điểm.")]
        [SerializeField] int m_PenaltyPerWrong = 0;

        [Header("Cài Đặt Thời Gian")]
        [Tooltip("Thời gian hiển thị feedback sau khi trả lời (giây).")]
        [SerializeField] float m_FeedbackDuration = 2.5f;

        [Tooltip("Thời gian giới hạn mỗi câu hỏi (giây). Đặt 0 = không giới hạn. Hết giờ tự động tính là SAI.")]
        [SerializeField] float m_QuestionTimeLimit = 0f;

        [Tooltip("Độ trễ nhỏ giữa feedback và câu hỏi tiếp theo (giây).")]
        [SerializeField] float m_DelayBetweenQuestions = 0.4f;

        [Header("Events")]
        [Tooltip("Gọi khi quiz bắt đầu.")]
        public UnityEvent OnQuizStart;

        [Tooltip("Gọi khi quiz kết thúc. Trả về điểm số cuối.")]
        public UnityEvent<int> OnQuizComplete;

        [Tooltip("Gọi khi trả lời đúng (tham số: điểm hiện tại).")]
        public UnityEvent<int> OnCorrectAnswer;

        [Tooltip("Gọi khi trả lời sai.")]
        public UnityEvent OnWrongAnswer;

        // ──────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────── //

        List<QuizQuestionData.Question> m_ActiveQuestions;
        int   m_CurrentIndex = 0;
        int   m_Score        = 0;
        int   m_CorrectCount = 0;
        bool  m_IsAnswering  = false; // Đang chờ trả lời?
        bool  m_IsRunning    = false; // Quiz đang chạy?

        Coroutine m_TimerCoroutine;
        Coroutine m_FeedbackCoroutine;

        // Public getters
        public int  Score        => m_Score;
        public int  CorrectCount => m_CorrectCount;
        public int  TotalQuestions => m_ActiveQuestions?.Count ?? 0;
        public bool IsRunning    => m_IsRunning;

        // ──────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────── //

        void Awake()
        {
            // Tự động tìm QuizHUD nếu người dùng kéo Prefab vào nhưng quên nối
            if (m_QuizHUD == null)
            {
                m_QuizHUD = Object.FindFirstObjectByType<QuizHUD>();
                if (m_QuizHUD != null)
                {
                    Debug.Log("[QuizManager] Tự động tìm và liên kết QuizHUD thành công.");
                }
            }
        }

        /// <summary>
        /// Bắt đầu quiz. Gọi từ QuizTriggerZone hoặc UnityEvent.
        /// Nếu quiz đang chạy thì bỏ qua lời gọi này.
        /// </summary>
        public void StartQuiz()
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("[QuizManager] Quiz đang chạy, bỏ qua lời gọi StartQuiz().");
                return;
            }

            if (m_QuestionData == null || m_QuestionData.questions == null || m_QuestionData.questions.Count == 0)
            {
                Debug.LogError("[QuizManager] ❌ Không có dữ liệu câu hỏi! Hãy kéo QuizQuestionData vào Inspector.");
                return;
            }

            // Khởi tạo danh sách hoạt động
            m_ActiveQuestions = new List<QuizQuestionData.Question>(m_QuestionData.questions);

            if (m_ShuffleQuestions)
                ShuffleList(m_ActiveQuestions);

            // Reset state
            m_CurrentIndex = 0;
            m_Score        = 0;
            m_CorrectCount = 0;
            m_IsRunning    = true;

            m_QuizHUD?.ShowHUD();
            OnQuizStart?.Invoke();

            Debug.Log($"[QuizManager] 🎯 Bắt đầu Quiz — {m_ActiveQuestions.Count} câu hỏi.");
            ShowCurrentQuestion();
        }

        /// <summary>
        /// Người chơi bấm nút ĐÚNG.
        /// Gắn vào XR Simple Interactable → Select Entered của nút ĐÚNG.
        /// </summary>
        public void SubmitTrue()  => SubmitAnswer(true);

        /// <summary>
        /// Người chơi bấm nút SAI.
        /// Gắn vào XR Simple Interactable → Select Entered của nút SAI.
        /// </summary>
        public void SubmitFalse() => SubmitAnswer(false);

        /// <summary>
        /// Nhận đáp án từ người chơi. Gọi trực tiếp nếu cần.
        /// </summary>
        public void SubmitAnswer(bool playerAnswer)
        {
            if (!m_IsAnswering || !m_IsRunning) return;

            // Dừng timer nếu đang chạy
            if (m_TimerCoroutine != null)
            {
                StopCoroutine(m_TimerCoroutine);
                m_TimerCoroutine = null;
            }

            m_IsAnswering = false;

            var question = m_ActiveQuestions[m_CurrentIndex];
            bool isCorrect = (playerAnswer == question.correctAnswer);

            // Tính điểm
            if (isCorrect)
            {
                m_Score += m_PointsPerCorrect;
                m_CorrectCount++;
                OnCorrectAnswer?.Invoke(m_Score);
                Debug.Log($"[QuizManager] ✅ Đúng! Câu {m_CurrentIndex + 1}. Điểm: {m_Score}");
            }
            else
            {
                m_Score = Mathf.Max(0, m_Score - m_PenaltyPerWrong);
                OnWrongAnswer?.Invoke();
                Debug.Log($"[QuizManager] ❌ Sai! Câu {m_CurrentIndex + 1}. Đáp án đúng: {(question.correctAnswer ? "ĐÚNG" : "SAI")}. Điểm: {m_Score}");
            }

            // Hiện feedback
            m_QuizHUD?.ShowFeedback(isCorrect, question.explanation);

            // Chờ rồi chuyển câu tiếp
            if (m_FeedbackCoroutine != null) StopCoroutine(m_FeedbackCoroutine);
            m_FeedbackCoroutine = StartCoroutine(ProceedAfterFeedback());
        }

        /// <summary>
        /// Reset và dừng quiz (gọi nếu người chơi rời khỏi scene).
        /// </summary>
        public void AbortQuiz()
        {
            StopAllCoroutines();
            m_IsRunning   = false;
            m_IsAnswering = false;
            m_QuizHUD?.HideHUD();
            Debug.Log("[QuizManager] Quiz bị hủy.");
        }

        // ──────────────────────────────────────────────────────── //
        //  Internal Logic
        // ──────────────────────────────────────────────────────── //

        void ShowCurrentQuestion()
        {
            if (m_CurrentIndex >= m_ActiveQuestions.Count)
            {
                EndQuiz();
                return;
            }

            var q = m_ActiveQuestions[m_CurrentIndex];
            m_IsAnswering = true;

            float timerNormalized = m_QuestionTimeLimit > 0f ? 1f : -1f;

            m_QuizHUD?.ShowQuestion(
                questionIndex    : m_CurrentIndex,
                totalQuestions   : m_ActiveQuestions.Count,
                questionText     : q.questionText,
                scenarioTag      : q.scenarioTag,
                timerNormalized  : timerNormalized
            );

            // Khởi động timer nếu có giới hạn thời gian
            if (m_QuestionTimeLimit > 0f)
            {
                if (m_TimerCoroutine != null) StopCoroutine(m_TimerCoroutine);
                m_TimerCoroutine = StartCoroutine(TimerCountdown());
            }
        }

        IEnumerator TimerCountdown()
        {
            float elapsed = 0f;
            while (elapsed < m_QuestionTimeLimit)
            {
                elapsed += Time.deltaTime;
                float normalized = 1f - (elapsed / m_QuestionTimeLimit);
                m_QuizHUD?.UpdateTimer(normalized);
                yield return null;
            }

            // Hết giờ → tự động tính SAI
            if (m_IsAnswering)
            {
                Debug.Log($"[QuizManager] ⏱ Hết thời gian câu {m_CurrentIndex + 1}! Tự động tính SAI.");
                SubmitAnswer(!m_ActiveQuestions[m_CurrentIndex].correctAnswer); // Chọn sai cố tình
            }
        }

        IEnumerator ProceedAfterFeedback()
        {
            yield return new WaitForSeconds(m_FeedbackDuration + m_DelayBetweenQuestions);

            m_CurrentIndex++;
            ShowCurrentQuestion();
        }

        void EndQuiz()
        {
            m_IsRunning   = false;
            m_IsAnswering = false;

            int maxScore = m_ActiveQuestions.Count * m_PointsPerCorrect;

            m_QuizHUD?.ShowResult(
                score        : m_Score,
                maxScore     : maxScore,
                correctCount : m_CorrectCount,
                totalCount   : m_ActiveQuestions.Count
            );

            OnQuizComplete?.Invoke(m_Score);

            Debug.Log($"[QuizManager] 🏁 Quiz kết thúc! Điểm: {m_Score}/{maxScore} | Đúng: {m_CorrectCount}/{m_ActiveQuestions.Count}");
        }

        // ──────────────────────────────────────────────────────── //
        //  Utilities
        // ──────────────────────────────────────────────────────── //

        static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ──────────────────────────────────────────────────────── //
        //  Debug Helpers (Context Menu trong Inspector)
        // ──────────────────────────────────────────────────────── //

        [ContextMenu("▶ Start Quiz (Debug)")]
        void Debug_StartQuiz() => StartQuiz();

        [ContextMenu("✔ Simulate Answer: ĐÚNG")]
        void Debug_AnswerTrue()
        {
            if (!m_IsRunning) { Debug.LogWarning("[QuizManager] Quiz chưa bắt đầu!"); return; }
            SubmitTrue();
        }

        [ContextMenu("✘ Simulate Answer: SAI")]
        void Debug_AnswerFalse()
        {
            if (!m_IsRunning) { Debug.LogWarning("[QuizManager] Quiz chưa bắt đầu!"); return; }
            SubmitFalse();
        }

        [ContextMenu("⏹ Abort Quiz (Debug)")]
        void Debug_AbortQuiz() => AbortQuiz();

        // ──────────────────────────────────────────────────────── //
        //  Keyboard Shortcuts (chỉ hoạt động trong Unity Editor)
        //
        //  [Space] → Bắt đầu quiz (khi quiz chưa chạy)
        //  [T]     → Chọn ĐÚNG
        //  [F]     → Chọn SAI
        //  [Esc]   → Hủy quiz
        // ──────────────────────────────────────────────────────── //

#if UNITY_EDITOR
        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // [Space] — Bắt đầu quiz khi chưa chạy
            if (keyboard.spaceKey.wasPressedThisFrame && !m_IsRunning)
            {
                Debug.Log("[QuizManager] ⌨ [Space] → StartQuiz()");
                StartQuiz();
                return;
            }

            // [I] — Trả lời ĐÚNG
            if (keyboard.iKey.wasPressedThisFrame)
            {
                if (!m_IsRunning)
                {
                    Debug.LogWarning("[QuizManager] ⌨ [I] nhấn nhưng quiz chưa bắt đầu. Nhấn [Space] để bắt đầu.");
                    return;
                }
                Debug.Log("[QuizManager] ⌨ [I] → SubmitTrue()");
                SubmitTrue();
                return;
            }

            // [O] — Trả lời SAI
            if (keyboard.oKey.wasPressedThisFrame)
            {
                if (!m_IsRunning)
                {
                    Debug.LogWarning("[QuizManager] ⌨ [O] nhấn nhưng quiz chưa bắt đầu. Nhấn [Space] để bắt đầu.");
                    return;
                }
                Debug.Log("[QuizManager] ⌨ [O] → SubmitFalse()");
                SubmitFalse();
                return;
            }

            // [Esc] — Hủy quiz
            if (keyboard.escapeKey.wasPressedThisFrame && m_IsRunning)
            {
                Debug.Log("[QuizManager] ⌨ [Esc] → AbortQuiz()");
                AbortQuiz();
            }
        }
#endif
    }
}
