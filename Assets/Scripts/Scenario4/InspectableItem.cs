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
        [Tooltip("Icon ✅. Kéo Prefab vào đây. Code sẽ tự sinh ra bên trong vùng Collider.")]
        [SerializeField] GameObject m_CorrectIcon;

        [Tooltip("Icon ❌. Kéo Prefab vào đây. Code sẽ tự sinh ra bên trong vùng Collider.")]
        [SerializeField] GameObject m_WrongIcon;

        [Header("Cài Đặt Vị Trí Icon")]
        [Tooltip("Nếu tích, icon sinh ra trên ĐỈNH collider. Nếu bỏ tích, sinh ra ngay GIỮA collider.")]
        [SerializeField] bool m_SpawnAtTop = true;
        [Tooltip("Chỉnh lệch lên/xuống một chút so với điểm tự động tính.")]
        [SerializeField] float m_YOffset = 0.2f;
        [Tooltip("Làm icon luôn xoay mặt về phía người chơi?")]
        [SerializeField] bool m_AlwaysFacePlayer = true;

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
        GameObject m_ActiveIconInstance; // Lưu icon đang hiển thị
        Transform m_MainCamera;

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
            // Nếu icon là object có sẵn trong scene thì ẩn nó đi
            if (m_CorrectIcon != null && m_CorrectIcon.scene.IsValid()) 
                m_CorrectIcon.SetActive(false);
                
            if (m_WrongIcon != null && m_WrongIcon.scene.IsValid()) 
                m_WrongIcon.SetActive(false);

            if (Camera.main != null)
                m_MainCamera = Camera.main.transform;
        }

        void Update()
        {
            // Làm icon luôn quay về camera
            if (m_IsSelected && m_AlwaysFacePlayer && m_ActiveIconInstance != null && m_MainCamera != null)
            {
                m_ActiveIconInstance.transform.LookAt(
                    m_ActiveIconInstance.transform.position + m_MainCamera.rotation * Vector3.forward,
                    m_MainCamera.rotation * Vector3.up
                );
            }
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
                m_ActiveIconInstance = ShowIcon(m_CorrectIcon);

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
                m_ActiveIconInstance = ShowIcon(m_WrongIcon);

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
            
            // Xóa/ẩn icon
            if (m_ActiveIconInstance != null)
            {
                if (m_ActiveIconInstance.scene.IsValid() && m_ActiveIconInstance != m_CorrectIcon && m_ActiveIconInstance != m_WrongIcon)
                {
                    Destroy(m_ActiveIconInstance); // Xóa bản sao
                }
                else
                {
                    m_ActiveIconInstance.SetActive(false); // Ẩn bản gốc
                }
                m_ActiveIconInstance = null;
            }

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

        GameObject ShowIcon(GameObject iconRef)
        {
            if (iconRef == null) return null;

            if (iconRef.scene.IsValid())
            {
                // Nếu là Object có sẵn trong Scene -> Bật lên
                iconRef.SetActive(true);
                return iconRef;
            }
            else
            {
                // Nếu là Prefab -> Tính vị trí dựa vào Collider
                Vector3 spawnPos = transform.position; // Dự phòng
                Collider col = GetComponent<Collider>();
                
                if (col != null)
                {
                    if (m_SpawnAtTop)
                    {
                        // Đặt ở trên đỉnh
                        spawnPos = col.bounds.center;
                        spawnPos.y = col.bounds.max.y;
                    }
                    else
                    {
                        // Đặt ngay chính giữa
                        spawnPos = col.bounds.center;
                    }
                }

                // Cộng thêm bù trừ Y
                spawnPos.y += m_YOffset;

                // Sinh ra bản sao
                GameObject spawned = Instantiate(iconRef, spawnPos, Quaternion.identity, transform);
                spawned.SetActive(true);
                return spawned;
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
