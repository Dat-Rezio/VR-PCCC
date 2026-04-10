using UnityEngine;
using TMPro;


public class StaircaseEvacuation : MonoBehaviour
{
    [Header("Cài đặt Hướng đi")]
    [Tooltip("Tích vào đây nếu Object này đặt ở cầu thang đi LÊN")]
    public bool isSafeRoute = true; 

    [Header("Giao diện & Phản hồi")]
    public GameObject feedbackPanel;     // Panel UI nổi trước mặt
    public TextMeshProUGUI feedbackText; 
    
    // Nếu bạn muốn ngăn người chơi đi xuống, kéo Teleportation Provider vào đây
    // để reset vị trí họ về lại chiếu nghỉ cầu thang
    public Transform resetPosition; 
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;

    private void OnTriggerEnter(Collider other)
    {
        // Nhận diện Đầu của người chơi (Camera)
        if (other.CompareTag("Player"))
        {
            ShowFeedback();
        }
    }

    private void ShowFeedback()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(true);

        if (isSafeRoute)
        {
            // --- HƯỚNG ĐI LÊN (ĐÚNG) ---
            if (feedbackText != null)
            {
                feedbackText.text = "CHÍNH XÁC!\nBạn đang di chuyển lên tầng lánh nạn.";
                feedbackText.color = Color.green;
            }
            Debug.Log("Thành công: Đi đúng hướng lánh nạn!");
        }
        else
        {
            // --- HƯỚNG ĐI XUỐNG (SAI) ---
            if (feedbackText != null)
            {
                feedbackText.text = "NGUY HIỂM!\nKhói lửa bốc lên từ tầng dưới. Hãy di chuyển lên tầng lánh nạn ở tầng trên!";
                feedbackText.color = Color.orange;
            }
            Debug.Log("Thất bại: Đi vào vùng cháy. Bắt đầu đẩy lùi bằng XRI.");

            // Gọi hàm dịch chuyển đẩy lùi
            TeleportPlayerBack();
        }
    }

    private void TeleportPlayerBack()
    {
        // Kiểm tra xem đã gắn đủ Provider và Vị trí đích chưa
        if (teleportationProvider != null && resetPosition != null)
        {
            // Tạo một yêu cầu dịch chuyển
            UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
            {
                destinationPosition = resetPosition.position,
                destinationRotation = resetPosition.rotation,
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward // Khớp hướng nhìn
            };

            // Gửi yêu cầu cho hệ thống XRI xử lý
            teleportationProvider.QueueTeleportRequest(request);
        }
        else
        {
            Debug.LogWarning("Chưa gắn Teleportation Provider hoặc Reset Position trong Inspector!");
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         if (feedbackPanel != null) feedbackPanel.SetActive(false);
    //     }
    // }
}