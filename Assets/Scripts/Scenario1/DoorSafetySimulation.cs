using UnityEngine;
using System.Collections;
using TMPro;
using Unity.Tutorials.Core.Editor;

public class DoorSafetySimulation : MonoBehaviour
{
    [Header("Cấu hình Giả lập")]
    public bool isHot = true;
    public float toggleInterval = 5f;

    [Header("Cấu hình Dịch chuyển")]
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider; 
    public Transform exitPoint;      

    [Header("Giao diện UI (2 Panel riêng biệt)")]
    public GameObject hotPanel;   // Kéo Panel "NÓNG" vào đây (có thể chứa hình lửa, text màu đỏ...)
    public GameObject safePanel;  // Kéo Panel "AN TOÀN" vào đây (có thể chứa hình check xanh, text...)
    public TextMeshProUGUI timerText; 

    [Header("Cấu hình Âm thanh")]
    public AudioSource audioSource;
    public AudioClip openDoorSound;   
    public AudioClip lockedSound;     

    [Header("Kết nối với Tutorial Manager")]
    public VRPCCC.UI.TutorialManager tutorialManager; 
    
    // Biến theo dõi xem tay người chơi có đang ở gần cửa không
    private bool isHandInZone = false; 
    
    private void Start()
    {
        // Đảm bảo cả 2 panel đều tắt khi mới bắt đầu
        if (hotPanel != null) hotPanel.SetActive(false);
        if (safePanel != null) safePanel.SetActive(false);
        
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
                
                // Cập nhật lại UI ngay lập tức nếu tay đang để ở cửa
                UpdateUI();
            }
        }
    }

    public void OnPlayerInteract()
    {
        if (!isHot)
        {
            if (audioSource != null && openDoorSound != null)
            {
                audioSource.PlayOneShot(openDoorSound);
            }

            ExecuteTeleport();
            
            if (tutorialManager != null)
            {
                tutorialManager.CompleteAction("OpenDoor");
            }
        }
        else
        {
            Debug.Log("Cửa đang nóng, không thể mở để dịch chuyển!");
            
            if (audioSource != null && lockedSound != null)
            {
                audioSource.PlayOneShot(lockedSound);
            }
        }
    }

    private void ExecuteTeleport()
    {
        if (teleportationProvider != null && exitPoint != null)
        {
            UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
            {
                destinationPosition = exitPoint.position,
                destinationRotation = exitPoint.rotation,
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward 
            };

            teleportationProvider.QueueTeleportRequest(request);
            
            Debug.Log("XRI: Đã thực hiện dịch chuyển bù trừ offset tự động.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            isHandInZone = true;
            UpdateUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            isHandInZone = false;
            // Khi rút tay ra, tắt cả 2 panel
            if (hotPanel != null) hotPanel.SetActive(false);
            if (safePanel != null) safePanel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        // Chỉ xử lý bật/tắt hình ảnh khi tay người chơi thực sự đang ở trong vùng Trigger
        if (isHandInZone)
        {
            if (hotPanel != null) hotPanel.SetActive(isHot);
            if (safePanel != null) safePanel.SetActive(!isHot);
        }
    }
}