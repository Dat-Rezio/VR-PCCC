using UnityEngine;
using UnityEngine.InputSystem;

namespace VRPCCC.Player
{
    /// <summary>
    /// Cho phép giả lập cúi người (crouch) bằng bàn phím/controller khi chạy trong Editor
    /// mà không cần kính VR.
    /// Script sẽ thay đổi độ cao của camera offset dựa trên nút bấm.
    /// </summary>
    public class SimulatorCrouch : MonoBehaviour
    {
        [Header("Simulator Settings")]
        [Tooltip("Có tính năng này khi build ra ứng dụng thật không? (Thường chỉ dùng trong Editor)")]
        [SerializeField] bool m_EditorOnly = true;

        [Tooltip("Transform chứa Camera để điều chỉnh độ cao (thường là Camera Offset). Tự động tìm nếu để trống.")]
        [SerializeField] Transform m_CameraOffset;

        [Tooltip("Chiều cao đứng bình thường (thường là 1.7m - 1.8m)")]
        [SerializeField] float m_StandingHeight = 1.7f;
        
        [Tooltip("Chiều cao khi cúi người giả lập (phải thấp hơn ngưỡng DuckThreshold của DuckingDetector)")]
        [SerializeField] float m_CrouchHeight = 1.0f;

        [Tooltip("Tốc độ chuyển đổi giữa đứng và cúi")]
        [SerializeField] float m_CrouchSpeed = 5f;

        [Header("Input")]
        [Tooltip("Phím dùng để bật/tắt trạng thái cúi")]
        [SerializeField] Key m_ToggleKey = Key.C;

        float m_TargetY;
        bool m_IsCrouching = false;
        
        void Awake()
        {
            // Tự động vô hiệu hóa nếu cấu hình chỉ chạy trong Editor mà đang build thực tế
            if (m_EditorOnly && !Application.isEditor)
            {
                enabled = false;
                return;
            }

            if (m_CameraOffset == null)
            {
                // XR Origin thường cấu trúc: XR Origin -> Camera Offset -> Main Camera
                // Cố gắng tìm Camera Offset từ Main Camera
                if (Camera.main != null && Camera.main.transform.parent != null)
                {
                    m_CameraOffset = Camera.main.transform.parent;
                }
            }

            m_TargetY = m_StandingHeight;
            
            if (m_CameraOffset != null)
            {
                Vector3 pos = m_CameraOffset.localPosition;
                pos.y = m_StandingHeight;
                m_CameraOffset.localPosition = pos;
            }
        }

        void Update()
        {
            if (m_CameraOffset == null) return;

            // Kiểm tra phím bấm để toggle trạng thái
            if (Keyboard.current != null && Keyboard.current[m_ToggleKey].wasPressedThisFrame)
            {
                m_IsCrouching = !m_IsCrouching;
                m_TargetY = m_IsCrouching ? m_CrouchHeight : m_StandingHeight;
                Debug.Log($"[SimulatorCrouch] Chuyển trạng thái: {(m_IsCrouching ? "CÚI (Crouch)" : "ĐỨNG (Stand)")}");
            }

            // Lerp mượt mà độ cao
            Vector3 currentPos = m_CameraOffset.localPosition;
            if (Mathf.Abs(currentPos.y - m_TargetY) > 0.01f)
            {
                currentPos.y = Mathf.Lerp(currentPos.y, m_TargetY, Time.deltaTime * m_CrouchSpeed);
                m_CameraOffset.localPosition = currentPos;
            }
        }
    }
}
