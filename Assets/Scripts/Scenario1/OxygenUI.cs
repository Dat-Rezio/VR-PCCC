using UnityEngine;
using UnityEngine.UI;

namespace VRPCCC.UI
{
    public class OxygenUI : MonoBehaviour
    {
        [Header("Giao diện")]
        [Tooltip("Kéo Image OxygenFill vào đây")]
        public Image oxygenFillImage;

        [Header("Màu sắc cảnh báo")]
        [Tooltip("Thiết lập màu từ Đỏ (Hết oxy) sang Xanh (Đầy oxy)")]
        public Gradient colorGradient;

        /// <summary>
        /// Hàm này sẽ nhận giá trị từ OnOxygenChanged (0 đến 1)
        /// </summary>
        public void UpdateOxygenBar(float ratio)
        {
            if (oxygenFillImage != null)
            {
                // Cập nhật độ dài của thanh
                oxygenFillImage.fillAmount = ratio;
                
                // Cập nhật màu sắc dựa trên tỷ lệ
                oxygenFillImage.color = colorGradient.Evaluate(ratio);
            }
        }
    }
}