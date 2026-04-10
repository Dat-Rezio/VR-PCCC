using UnityEngine;
using System.Collections;
using TMPro;


public class DoorSafetySimulation : MonoBehaviour
{
    [Header("Cấu hình Giả lập")]
    public bool isHot = true;
    public float toggleInterval = 5f;

    [Header("Cấu hình Dịch chuyển")]
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider; // Kéo XR Origin (hoặc Main Camera Rig) vào đây
    public Transform exitPoint;      // Vị trí đích ở hành lang (tạo một Empty Object ngoài hành lang)

    [Header("Giao diện UI")]
    public GameObject temperaturePanel;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText; // (Tùy chọn) Hiển thị giây còn lại để đổi trạng thái

    private void Start()
    {
        if (temperaturePanel != null) temperaturePanel.SetActive(false);
        
        // Bắt đầu vòng lặp tự động chuyển đổi trạng thái
        StartCoroutine(ToggleTemperatureRoutine());
    }

    IEnumerator ToggleTemperatureRoutine()
    {
        float timer = toggleInterval;
        while (true)
        {
            yield return new WaitForSeconds(1f);
            timer -= 1f;

            if (timerText != null) timerText.text = $"Đổi trạng thái sau: {timer}s";

            if (timer <= 0)
            {
                isHot = !isHot;
                timer = toggleInterval;
                Debug.Log("Trạng thái cửa đã đổi sang: " + (isHot ? "NÓNG" : "AN TOÀN"));
                UpdateUI();
            }
        }
    }

    // Hàm này sẽ được gọi khi người chơi kích hoạt "Trigger" hoặc "Grab" trên cửa
    // Trong Unity: Thêm event vào XR Simple Interactable -> Select Entered
    public void OnPlayerInteract()
    {
        if (!isHot)
        {
            ExecuteTeleport();
        }
        else
        {
            Debug.Log("Cửa đang nóng, không thể mở để dịch chuyển!");
            // Có thể thêm hiệu ứng âm thanh cảnh báo ở đây
        }
    }

    private void ExecuteTeleport()
    {
        if (teleportationProvider != null && exitPoint != null)
        {
            // Tạo một yêu cầu dịch chuyển (Teleport Request)
            UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
            {
                destinationPosition = exitPoint.position,
                destinationRotation = exitPoint.rotation,
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward // Khớp cả vị trí và hướng nhìn
            };

            // Gửi yêu cầu vào hàng đợi của Provider
            teleportationProvider.QueueTeleportRequest(request);
            
            Debug.Log("XRI: Đã thực hiện dịch chuyển bù trừ offset tự động.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            UpdateUI();
            temperaturePanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            temperaturePanel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = isHot ? "NÓNG QUÁ!!! Nhiệt độ ngoài cửa đang rất cao.\nĐừng mở cửa, nguy hiểm!" : "AN TOÀN! Nhiệt độ ngoài cửa đang bình thường.\nBạn có thể mở cửa.";
            statusText.color = isHot ? Color.red : Color.green;
        }
    }
}