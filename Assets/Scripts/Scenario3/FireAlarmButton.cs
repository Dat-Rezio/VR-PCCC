using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Scenario3
{
    public class FireAlarmButton : MonoBehaviour
    {
        [Header("Cài đặt Vật lý")]
        [Tooltip("Tag của tay cầm VR (VD: PlayerHand)")]
        public string handTag = "PlayerHand";
        
        [Tooltip("Phần lưới 3D của nút bấm (để làm hiệu ứng lún xuống)")]
        public Transform buttonMesh;
        
        [Tooltip("Trục di chuyển lún xuống (thường là Z cục bộ)")]
        public Vector3 pushDirection = new Vector3(0, 0, 0.02f); 

        [Header("Âm thanh")]
        [Tooltip("Âm thanh 'Tách' hoặc tiếng vỡ kính khi ấn")]
        public AudioSource audioSource;
        public AudioClip pressSound;

        [Header("Sự kiện")]
        [Tooltip("Kéo Scene3_Manager vào đây")]
        public UnityEvent OnAlarmPressed;

        private bool isPressed = false;
        private Vector3 originalLocalPos;

        void Start()
        {
            if (buttonMesh != null)
            {
                originalLocalPos = buttonMesh.localPosition;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // Kiểm tra nếu chưa bị ấn và vật chạm vào có tag là bàn tay
            if (!isPressed && other.CompareTag(handTag))
            {
                PressAction();
            }
        }

        void PressAction()
        {
            isPressed = true;

            // 1. Hiệu ứng hình ảnh: Ấn lún nút xuống
            if (buttonMesh != null)
            {
                buttonMesh.localPosition = originalLocalPos - pushDirection;
            }

            // 2. Hiệu ứng âm thanh: Tiếng "tách"
            if (audioSource != null && pressSound != null)
            {
                audioSource.PlayOneShot(pressSound);
            }

            // 3. Gửi tín hiệu cho EscapeSceneManager
            OnAlarmPressed?.Invoke();
            Debug.Log("[FireAlarm] ĐÃ BẤM NÚT BÁO CHÁY!");
        }

        // Dùng khi gọi hàm Reset kịch bản
        public void ResetButton()
        {
            isPressed = false;
            if (buttonMesh != null)
            {
                buttonMesh.localPosition = originalLocalPos;
            }
        }
    }
}