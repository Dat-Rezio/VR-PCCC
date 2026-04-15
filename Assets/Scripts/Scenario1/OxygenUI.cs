using UnityEngine;
using UnityEngine.UI;

namespace VRPCCC.UI
{
    public class OxygenUI : MonoBehaviour
    {
        [Header("Quản lý hiển thị")]
        [Tooltip("Kéo Object 'Canvas' hoặc 'Panel' chứa thanh Oxy vào đây")]
        public GameObject OxygenUICanvas; 

        [Header("Giao diện")]
        public Image oxygenFillImage;
        public Gradient colorGradient;

        private void Awake()
        {
            // Mặc định ẩn đi khi mới vào game
            SetVisibility(false);
        }

        /// <summary>
        /// Hàm này dùng để bật/tắt cả cụm giao diện
        /// </summary>
        public void SetVisibility(bool isVisible)
        {
            if (OxygenUICanvas != null)
            {
                OxygenUICanvas.SetActive(isVisible);
                Debug.Log($"[OxygenUI] Trạng thái hiển thị: {isVisible}");
            }
        }

        public void UpdateOxygenBar(float ratio)
        {
            // Chỉ cập nhật nếu UI đang hiện để tiết kiệm hiệu năng
            if (OxygenUICanvas != null && OxygenUICanvas.activeSelf && oxygenFillImage != null)
            {
                oxygenFillImage.fillAmount = ratio;
                oxygenFillImage.color = colorGradient.Evaluate(ratio);
            }
        }
    }
}