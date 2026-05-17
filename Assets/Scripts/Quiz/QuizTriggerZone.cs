using UnityEngine;

namespace VRPCCC.Quiz
{
    /// <summary>
    /// Trigger Zone kích hoạt Quiz khi người chơi bước vào.
    ///
    /// SETUP TRONG UNITY:
    /// ─────────────────────────────────────────────────────────────────
    ///  1. Tạo một GameObject trong scene (VD: Empty Object đặt tên "QuizTriggerZone")
    ///  2. Thêm Collider (VD: Box Collider) → bật "Is Trigger"
    ///  3. Gắn script này vào
    ///  4. Kéo QuizManager vào field [quizManager]
    ///  5. Đặt vùng trigger ở nơi muốn quiz bắt đầu trong scene tổng gộp
    ///
    ///  LƯU Ý TAG:
    ///    - XR Origin (hoặc object tay/đầu người chơi) PHẢI có tag "Player"
    ///    - Hoặc đổi field [playerTag] về đúng tag đang dùng trong project
    /// ─────────────────────────────────────────────────────────────────
    /// </summary>
    public class QuizTriggerZone : MonoBehaviour
    {
        [Header("Tham Chiếu")]
        [Tooltip("Kéo QuizManager vào đây.")]
        [SerializeField] QuizManager m_QuizManager;

        [Header("Cài Đặt")]
        [Tooltip("Tag của XR Origin / Player. Phải khớp với tag đang dùng trong scene.")]
        [SerializeField] string m_PlayerTag = "Player";

        [Tooltip("Chỉ kích hoạt quiz 1 lần duy nhất (bước vào lần 2 không tính). Khuyến nghị: bật.")]
        [SerializeField] bool m_TriggerOnce = true;

        [Tooltip("(Tùy chọn) Ẩn Collider Renderer khi bị kích hoạt. Hữu ích nếu có hình dạng nhìn thấy.")]
        [SerializeField] bool m_DisableRendererOnTrigger = false;

        [Tooltip("(Tùy chọn) Highlight visual object khi chưa được kích hoạt (VD: mũi tên chỉ đường).")]
        [SerializeField] GameObject m_VisualIndicator;

        bool m_HasTriggered = false;

        // ──────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────── //

        void Start()
        {
            // Tự động tìm QuizManager nếu người dùng quên nối Prefab
            if (m_QuizManager == null)
            {
                m_QuizManager = Object.FindFirstObjectByType<QuizManager>();
            }

            // Đảm bảo collider là trigger
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[QuizTriggerZone] Collider trên '{gameObject.name}' chưa bật 'Is Trigger'. Đã tự động bật.");
            }

            // Hiện visual indicator (nếu có)
            if (m_VisualIndicator != null)
                m_VisualIndicator.SetActive(true);
        }

        void OnTriggerEnter(Collider other)
        {
            // Kiểm tra đúng tag
            if (!other.CompareTag(m_PlayerTag)) return;

            // Kiểm tra đã trigger chưa (nếu bật TriggerOnce)
            if (m_TriggerOnce && m_HasTriggered) return;

            m_HasTriggered = true;

            Debug.Log($"[QuizTriggerZone] 👟 Người chơi bước vào vùng Quiz! Kích hoạt quiz...");

            // Ẩn visual indicator
            if (m_VisualIndicator != null)
                m_VisualIndicator.SetActive(false);

            // Ẩn renderer nếu cần
            if (m_DisableRendererOnTrigger)
            {
                var rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
            }

            // Kích hoạt quiz
            if (m_QuizManager != null)
                m_QuizManager.StartQuiz();
            else
                Debug.LogError("[QuizTriggerZone] ❌ Chưa gán QuizManager! Hãy kéo QuizManager vào Inspector.");
        }

        // ──────────────────────────────────────────────────────── //
        //  Public API (dùng nếu cần reset từ bên ngoài)
        // ──────────────────────────────────────────────────────── //

        /// <summary>
        /// Reset trigger về trạng thái ban đầu (cho phép kích hoạt lại).
        /// </summary>
        public void ResetTrigger()
        {
            m_HasTriggered = false;
            if (m_VisualIndicator != null)
                m_VisualIndicator.SetActive(true);
            Debug.Log("[QuizTriggerZone] 🔄 Đã reset trigger zone.");
        }
    }
}
