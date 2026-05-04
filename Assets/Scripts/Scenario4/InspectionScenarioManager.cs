using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VRPCCC.Scenario4
{
    /// <summary>
    /// Quản lý kịch bản Scenario 4: Kiểm Tra An Toàn PCCC.
    /// Người chơi đi quanh căn hộ tìm các nguy cơ cháy nổ.
    /// 
    /// LUỒNG MỚI:
    ///   - Trong lúc chơi: HUD chỉ hiện tiến độ tối thiểu
    ///   - Khi chọn vật thể: icon ✅/❌ hiện TẠI CHỖ vật thể (do InspectableItem xử lý)
    ///   - Chọn đúng → cộng điểm, chọn sai → trừ điểm
    ///   - Sau khi hoàn thành → HUD hiện TỔNG HỢP tất cả Explanation
    /// </summary>
    public class InspectionScenarioManager : MonoBehaviour
    {
        public enum InspectionState { WaitingToStart, Inspecting, Completed }

        // ──────────────────────────────────────────────────────────────────── //
        //  Struct lưu kết quả mỗi lần chọn
        // ──────────────────────────────────────────────────────────────────── //

        [System.Serializable]
        public struct InspectionResult
        {
            public string itemName;
            public string explanation;
            public bool wasCorrect;
            public int pointsChanged;
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Tham Chiếu Hệ Thống")]
        [Tooltip("Kéo InspectionHUD vào đây.")]
        [SerializeField] InspectionHUD m_HUD;

        [Header("Cài Đặt Kịch Bản")]
        [Tooltip("Tổng số nguy cơ cần tìm trong scene.")]
        [SerializeField] int m_TotalHazards = 6;

        [Tooltip("Điểm khởi đầu.")]
        [SerializeField] int m_InitialScore = 100;

        [Header("Nội Dung Hướng Dẫn (Tùy chỉnh trong Inspector)")]
        [TextArea(2, 4)] public string txt_Welcome = "<b>📋 KIỂM TRA AN TOÀN PCCC</b>\nHãy tìm các vật thể có nguy cơ cháy nổ!\nSố lượng vật thể nguy cơ: <b>{0}</b>";
        [TextArea(2, 4)] public string txt_Progress = "<b>📋 TIẾN ĐỘ KIỂM TRA</b>\nĐã tìm được: <b>{0} / {1}</b>\nSố lần chọn sai: <b>{2}</b>";
        [TextArea(2, 4)] public string txt_Success = "<b>✅ HOÀN THÀNH KIỂM TRA!</b>\nĐiểm số: <b>{0}</b> / {1}";

        [Header("Nội Dung Tổng Hợp")]
        [TextArea(2, 4)] public string txt_SummaryHeader = "<b>📝 GIẢI THÍCH CHI TIẾT</b>\n\n";
        [TextArea(2, 4)] public string txt_CorrectFormat = "✅ <b>{0}</b>\n<i>{1}</i>\n";
        [TextArea(2, 4)] public string txt_WrongFormat = "❌ <b>{0}</b> (chọn sai, -{1} điểm)\n";

        [Header("Âm Thanh (Tùy chọn)")]
        [Tooltip("Âm thanh phát khi hoàn thành toàn bộ.")]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_CompletedSound;

        [Header("Events")]
        public UnityEvent OnInspectionStart;
        public UnityEvent<int> OnInspectionCompleted; // Trả về điểm số

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        InspectionState m_State = InspectionState.WaitingToStart;
        int m_CorrectCount = 0;
        int m_WrongCount = 0;
        int m_Score;
        float m_ElapsedTime;
        List<InspectionResult> m_Results = new List<InspectionResult>();

        public InspectionState CurrentState => m_State;
        public int CorrectCount => m_CorrectCount;
        public int WrongCount => m_WrongCount;
        public int Score => m_Score;
        public List<InspectionResult> Results => m_Results;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Awake()
        {
            m_Score = m_InitialScore;
        }

        void Start()
        {
            // Tự động bắt đầu kịch bản khi scene load
            StartInspection();
        }

        void Update()
        {
            // Đếm thời gian khi đang kiểm tra
            if (m_State == InspectionState.Inspecting)
            {
                m_ElapsedTime += Time.deltaTime;
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Bắt đầu kịch bản kiểm tra.
        /// </summary>
        public void StartInspection()
        {
            m_State = InspectionState.Inspecting;
            m_CorrectCount = 0;
            m_WrongCount = 0;
            m_ElapsedTime = 0f;
            m_Score = m_InitialScore;
            m_Results.Clear();

            m_HUD?.ShowMain(string.Format(txt_Welcome, m_TotalHazards));
            OnInspectionStart?.Invoke();

            // Sau vài giây, chuyển sang hiện bộ đếm tiến độ
            StartCoroutine(ShowProgressAfterDelay(4f));

            Debug.Log("[Scenario4] 📋 Bắt đầu kiểm tra an toàn PCCC!");
        }

        /// <summary>
        /// Gọi từ InspectableItem khi người chơi chọn ĐÚNG (nguy cơ cháy nổ).
        /// Icon ✅ đã được InspectableItem hiện tại chỗ rồi, ở đây chỉ cập nhật điểm.
        /// </summary>
        /// <param name="itemName">Tên vật thể</param>
        /// <param name="explanation">Giải thích nguy cơ (lưu để hiện tổng hợp cuối)</param>
        /// <param name="points">Số điểm cộng</param>
        public void OnCorrectSelection(string itemName, string explanation, int points)
        {
            if (m_State != InspectionState.Inspecting) return;

            m_CorrectCount++;
            m_Score += points;

            // Lưu kết quả
            m_Results.Add(new InspectionResult
            {
                itemName = itemName,
                explanation = explanation,
                wasCorrect = true,
                pointsChanged = points
            });

            Debug.Log($"[Scenario4] ✅ Đúng! {itemName} (+{points}). Tổng: {m_CorrectCount}/{m_TotalHazards}. Điểm: {m_Score}");

            // Cập nhật HUD tiến độ
            UpdateProgressHUD();

            // Kiểm tra hoàn thành
            if (m_CorrectCount >= m_TotalHazards)
            {
                StartCoroutine(CompleteAfterDelay(1.5f));
            }
        }

        /// <summary>
        /// Gọi từ InspectableItem khi người chơi chọn SAI (vật an toàn).
        /// Icon ❌ đã được InspectableItem hiện tại chỗ rồi, ở đây chỉ trừ điểm.
        /// </summary>
        /// <param name="itemName">Tên vật thể</param>
        /// <param name="penalty">Số điểm trừ (nhập dương, sẽ tự trừ)</param>
        public void OnWrongSelection(string itemName, int penalty)
        {
            if (m_State != InspectionState.Inspecting) return;

            m_WrongCount++;
            m_Score -= penalty;

            // Lưu kết quả
            m_Results.Add(new InspectionResult
            {
                itemName = itemName,
                explanation = "",
                wasCorrect = false,
                pointsChanged = -penalty
            });

            Debug.Log($"[Scenario4] ❌ Sai! {itemName} (-{penalty}). Điểm: {m_Score}");

            // Cập nhật HUD tiến độ
            UpdateProgressHUD();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal
        // ──────────────────────────────────────────────────────────────────── //

        void UpdateProgressHUD()
        {
            if (m_State == InspectionState.Inspecting)
            {
                string progress = string.Format(txt_Progress, m_CorrectCount, m_TotalHazards, m_WrongCount);
                m_HUD?.ShowMain(progress);
            }
        }

        IEnumerator ShowProgressAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateProgressHUD();
        }

        IEnumerator CompleteAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            CompleteInspection();
        }

        void CompleteInspection()
        {
            m_State = InspectionState.Completed;

            // Phát âm thanh hoàn thành
            if (m_AudioSource != null && m_CompletedSound != null)
                m_AudioSource.PlayOneShot(m_CompletedSound);

            // Hiện kết quả: Điểm số + Giải thích
            string scoreText = string.Format(txt_Success, m_Score, m_InitialScore);
            string summary = BuildSummary();
            m_HUD?.ShowEnd(scoreText, summary);

            OnInspectionCompleted?.Invoke(m_Score);

            Debug.Log($"[Scenario4] ✅ Hoàn thành! Điểm: {m_Score}, Thời gian: {m_ElapsedTime:F1}s, Đúng: {m_CorrectCount}, Sai: {m_WrongCount}");
        }

        /// <summary>
        /// Xây dựng nội dung tổng hợp từ tất cả kết quả.
        /// </summary>
        string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(txt_SummaryHeader);

            foreach (var result in m_Results)
            {
                if (result.wasCorrect)
                {
                    sb.AppendFormat(txt_CorrectFormat, result.itemName, result.explanation);
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendFormat(txt_WrongFormat, result.itemName, Mathf.Abs(result.pointsChanged));
                    sb.AppendLine();
                }
            }

            // Thêm tóm tắt cuối
            sb.AppendLine();
            sb.AppendFormat("<b>Tổng kết:</b> Tìm đúng {0}/{1} | Chọn sai {2} lần | Điểm: <b>{3}</b> / {4}",
                m_CorrectCount, m_TotalHazards, m_WrongCount, m_Score, m_InitialScore);

            return sb.ToString();
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Editor Utilities
        // ──────────────────────────────────────────────────────────────────── //

        [ContextMenu("Reset Scenario")]
        public void ResetScenario()
        {
            m_CorrectCount = 0;
            m_WrongCount = 0;
            m_ElapsedTime = 0f;
            m_Score = m_InitialScore;
            m_Results.Clear();
            m_State = InspectionState.WaitingToStart;
            m_HUD?.ResetHUD();

            // Reset tất cả InspectableItem trong scene
            var items = FindObjectsOfType<InspectableItem>();
            foreach (var item in items)
                item.ResetItem();

            Debug.Log("[Scenario4] 🔄 Đã reset kịch bản.");
        }
    }
}
