using System.Collections.Generic;
using UnityEngine;

namespace VRPCCC.Quiz
{
    /// <summary>
    /// ScriptableObject lưu danh sách câu hỏi Đúng/Sai cho hệ thống Quiz.
    ///
    /// CÁCH TẠO ASSET:
    ///   Chuột phải trong Project window → Create → VR-PCCC → Quiz Question Data
    ///
    /// CÁCH THÊM CÂU HỎI:
    ///   Chọn file .asset vừa tạo → Inspector → nhấn "+" để thêm câu hỏi mới.
    ///   Không cần sửa code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuizData_New",
        menuName = "VR-PCCC/Quiz Question Data",
        order = 0)]
    public class QuizQuestionData : ScriptableObject
    {
        // ──────────────────────────────────────────────────────── //
        //  Enum nhãn kịch bản
        // ──────────────────────────────────────────────────────── //

        public enum ScenarioTag
        {
            [InspectorName("Tổng quát")]        General    = 0,
            [InspectorName("Kịch bản 1 — Kiểm tra nhiệt độ cửa")] Scenario1 = 1,
            [InspectorName("Kịch bản 2 — Chữa cháy bằng bình")]   Scenario2 = 2,
            [InspectorName("Kịch bản 3 — Thoát hiểm")]             Scenario3 = 3,
            [InspectorName("Kịch bản 4 — Kiểm tra nguy cơ")]       Scenario4 = 4,
        }

        // ──────────────────────────────────────────────────────── //
        //  Data class cho từng câu hỏi
        // ──────────────────────────────────────────────────────── //

        [System.Serializable]
        public class Question
        {
            [Tooltip("Nội dung câu hỏi hiển thị trên HUD.")]
            [TextArea(2, 5)]
            public string questionText = "Nội dung câu hỏi...";

            [Tooltip("Đáp án đúng của câu hỏi này.\n✔ = ĐÚNG | ✘ = SAI")]
            public bool correctAnswer = true;

            [Tooltip("Giải thích lý do sau khi người chơi trả lời. Hiện ở panel Feedback.")]
            [TextArea(2, 5)]
            public string explanation = "Giải thích lý do ở đây...";

            [Tooltip("Kịch bản mà câu hỏi này thuộc về. Chỉ dùng để hiển thị nhãn, không ảnh hưởng logic.")]
            public ScenarioTag scenarioTag = ScenarioTag.General;
        }

        // ──────────────────────────────────────────────────────── //
        //  Danh sách câu hỏi
        // ──────────────────────────────────────────────────────── //

        [Header("Danh sách câu hỏi")]
        [Tooltip("Kéo thả hoặc nhấn '+' để thêm câu hỏi mới. Thứ tự trong list = thứ tự hiển thị (nếu không shuffle).")]
        public List<Question> questions = new List<Question>();
    }
}
