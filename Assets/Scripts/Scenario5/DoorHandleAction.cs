using UnityEngine;

// Xóa RequireComponent để tránh tự động thêm nếu bạn đã lỡ thiết lập
public class DoorHandleAction : MonoBehaviour
{
    [Header("Cài đặt UI Cảnh báo")]
    public GameObject warningUI; 

    [Header("Cài đặt Dịch chuyển (Game Over)")]
    public Transform playerRig; 
    public Transform fireCorridorSpawnPoint; 

    void Awake()
    {
        // Đảm bảo UI luôn tắt khi bắt đầu
        if (warningUI != null) warningUI.SetActive(false);
    }

    // Hàm bật UI (Gán vào Hover Entered)
    public void ShowWarningUI()
    {
        if (warningUI != null) warningUI.SetActive(true);
    }

    // Hàm tắt UI (Gán vào Hover Exited)
    public void HideWarningUI()
    {
        if (warningUI != null) warningUI.SetActive(false);
    }

    // Hàm dịch chuyển (Gán vào Select Entered)
    public void TriggerGameOver()
    {
        if (playerRig != null && fireCorridorSpawnPoint != null)
        {
            playerRig.position = fireCorridorSpawnPoint.position;
            playerRig.rotation = fireCorridorSpawnPoint.rotation;
            Debug.Log("NGUY HIỂM! Đã mở cửa ra ngoài!");
        }
    }
}