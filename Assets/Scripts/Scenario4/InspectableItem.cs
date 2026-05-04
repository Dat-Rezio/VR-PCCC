using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Scenario4
{
    /// <summary>
    /// Gắn lên MỌI vật thể có thể tương tác trong Scenario 4 (cả nguy cơ lẫn vật an toàn).
    /// Khi người chơi chọn vật thể:
    ///   - Nếu là nguy cơ (IsHazard = true) → hiện icon ✅ tại chỗ, cộng điểm
    ///   - Nếu là vật an toàn (IsHazard = false) → hiện icon ❌ tại chỗ, trừ điểm
    /// 
    /// Không hiện gì trên HUD khi chọn — chỉ icon 3D ngay tại vật thể.
    /// Explanation được lưu lại và tổng hợp hiện trên HUD sau khi hoàn thành.
    /// 
    /// Setup trong Unity:
    ///   1. Gắn script này lên GameObject (VD: ổ cắm, tủ lạnh, bình gas...)
    ///   2. Thêm XRSimpleInteractable component
    ///   3. Trong XRSimpleInteractable → Select Entered → kéo InspectableItem.OnPlayerInteract()
    ///   4. Đánh dấu m_IsHazard = true nếu là nguy cơ, false nếu vật an toàn
    ///   5. Tạo 2 child object (icon ✅ và ❌) rồi kéo vào m_CorrectIcon / m_WrongIcon
    ///   6. Kéo InspectionScenarioManager vào ô m_Manager
    /// 
    /// Cách tạo Icon 3D đơn giản:
    ///   - Tạo child GameObject → Add TextMeshPro (3D) → gõ "✅" hoặc "❌"
    ///   - Hoặc dùng Sprite/Quad với material tương ứng
    ///   - Ẩn ban đầu (SetActive false)
    /// </summary>
    public class InspectableItem : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────── //
        //  Inspector Fields
        // ──────────────────────────────────────────────────────────────────── //

        [Header("Phân Loại Vật Thể")]
        [Tooltip("TRUE = đây là nguy cơ cháy nổ (chọn đúng). FALSE = vật an toàn (chọn sai).")]
        [SerializeField] bool m_IsHazard = true;

        [Header("Thông Tin")]
        [Tooltip("Tên vật thể (VD: 'Ổ cắm quá tải', 'Tủ lạnh').")]
        [SerializeField] string m_ItemName = "Vật thể";

        [Tooltip("Giải thích chi tiết (chỉ dùng cho nguy cơ). Sẽ hiện trong tổng hợp cuối.")]
        [TextArea(3, 5)]
        [SerializeField] string m_Explanation = "";

        [Header("Điểm Số")]
        [Tooltip("Điểm cộng khi chọn đúng nguy cơ.")]
        [SerializeField] int m_PointsCorrect = 25;

        [Tooltip("Điểm trừ khi chọn sai (vật an toàn). Nhập số dương, sẽ tự trừ.")]
        [SerializeField] int m_PointsWrong = 5;

        [Header("Icon Hiển Thị Tại Chỗ")]
        [Tooltip("Icon ✅ (3D object, ẩn ban đầu). Hiện khi chọn ĐÚNG nguy cơ.")]
        [SerializeField] GameObject m_CorrectIcon;

        [Tooltip("Icon ❌ (3D object, ẩn ban đầu). Hiện khi chọn SAI (vật an toàn) hoặc chọn nhầm.")]
        [SerializeField] GameObject m_WrongIcon;

        [Header("Tham Chiếu")]
        [Tooltip("Kéo InspectionScenarioManager vào đây.")]
        [SerializeField] InspectionScenarioManager m_Manager;

        [Header("Hiệu Ứng Bổ Sung (Tùy chọn)")]
        [Tooltip("Outline/highlight gợi ý. Sẽ tắt sau khi chọn.")]
        [SerializeField] GameObject m_HighlightEffect;

        [Header("Âm Thanh (Tùy chọn)")]
        [Tooltip("Âm thanh khi chọn đúng.")]
        [SerializeField] AudioClip m_CorrectSound;
        [Tooltip("Âm thanh khi chọn sai.")]
        [SerializeField] AudioClip m_WrongSound;
        [SerializeField] AudioSource m_AudioSource;

        [Header("Events")]
        public UnityEvent OnItemSelected;

        // ──────────────────────────────────────────────────────────────────── //
        //  Runtime State
        // ──────────────────────────────────────────────────────────────────── //

        bool m_IsSelected = false;

        /// <summary>Vật thể này đã được chọn chưa.</summary>
        public bool IsSelected => m_IsSelected;
        /// <summary>Đây có phải nguy cơ không.</summary>
        public bool IsHazard => m_IsHazard;
        public string ItemName => m_ItemName;
        public string Explanation => m_Explanation;

        // ──────────────────────────────────────────────────────────────────── //
        //  Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────── //

        void Start()
        {
            // Ẩn cả 2 icon ban đầu
            if (m_CorrectIcon != null) m_CorrectIcon.SetActive(false);
            if (m_WrongIcon != null) m_WrongIcon.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Public API
        // ──────────────────────────────────────────────────────────────────── //

        /// <summary>
        /// Gọi từ XRSimpleInteractable → Select Entered (UnityEvent).
        /// Hiện icon tại chỗ và thông báo cho Manager.
        /// </summary>
        public void OnPlayerInteract()
        {
            if (m_IsSelected) return; // Đã chọn rồi, bỏ qua

            m_IsSelected = true;

            if (m_IsHazard)
            {
                // ═══ CHỌN ĐÚNG — Đây là nguy cơ ═══
                if (m_CorrectIcon != null)
                    m_CorrectIcon.SetActive(true);

                // Phát âm thanh đúng
                PlaySound(m_CorrectSound);

                // Thông báo Manager
                if (m_Manager != null)
                    m_Manager.OnCorrectSelection(m_ItemName, m_Explanation, m_PointsCorrect);

                Debug.Log($"[Inspectable] ✅ ĐÚNG: {m_ItemName} (+{m_PointsCorrect} điểm)");
            }
            else
            {
                // ═══ CHỌN SAI — Đây là vật an toàn ═══
                if (m_WrongIcon != null)
                    m_WrongIcon.SetActive(true);

                // Phát âm thanh sai
                PlaySound(m_WrongSound);

                // Thông báo Manager
                if (m_Manager != null)
                    m_Manager.OnWrongSelection(m_ItemName, m_PointsWrong);

                Debug.Log($"[Inspectable] ❌ SAI: {m_ItemName} (-{m_PointsWrong} điểm)");
            }

            // Tắt highlight gợi ý
            if (m_HighlightEffect != null)
                m_HighlightEffect.SetActive(false);

            // Phát event
            OnItemSelected?.Invoke();
        }

        /// <summary>Reset lại trạng thái (dùng khi chơi lại).</summary>
        public void ResetItem()
        {
            m_IsSelected = false;
            if (m_CorrectIcon != null) m_CorrectIcon.SetActive(false);
            if (m_WrongIcon != null) m_WrongIcon.SetActive(false);
            if (m_HighlightEffect != null) m_HighlightEffect.SetActive(true);
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Internal
        // ──────────────────────────────────────────────────────────────────── //

        void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            if (m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        // ──────────────────────────────────────────────────────────────────── //
        //  Gizmos
        // ──────────────────────────────────────────────────────────────────── //

        void OnDrawGizmos()
        {
            Gizmos.color = m_IsHazard ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);

#if UNITY_EDITOR
            // Hiện label trong Scene View
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                m_IsHazard ? $"🔥 {m_ItemName}" : $"✓ {m_ItemName} (an toàn)"
            );
#endif
        }
    }
}
