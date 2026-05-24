using UnityEngine;
using UnityEngine.InputSystem;
 // Bắt buộc để gọi Locomotion System

public class DoorHandleDirectPhysics : MonoBehaviour
{
    [Header("Cài đặt UI Cảnh báo")]
    public GameObject warningUI;

    [Header("Cài đặt Dịch chuyển (Locomotion)")]
    [Tooltip("Kéo object chứa Teleportation Provider vào đây")]
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider; 
    [Tooltip("Kéo một object rỗng đặt ngoài hành lang vào đây")]
    public Transform fireCorridorSpawnPoint;

    [Header("Cài đặt Nút bấm (Trigger)")]
    [Tooltip("Kéo Action bóp cò của tay cầm vào đây (VD: XRI RightHand/Select)")]
    public InputActionReference triggerAction;

    private bool isHandHovering = false;

    void Awake()
    {
        // Tắt UI khi game mới chạy
        if (warningUI != null) warningUI.SetActive(false);
    }

    void Update()
    {
        // Nếu tay đang chạm vào nắm cửa VÀ bóp cò (Trigger)
        if (isHandHovering && triggerAction != null && triggerAction.action.WasPressedThisFrame())
        {
            ExecuteTeleport();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem object chạm vào có đúng là bàn tay không
        if (other.CompareTag("PlayerHand"))
        {
            isHandHovering = true;
            if (warningUI != null) warningUI.SetActive(true);
        }
        Scenario5Manager.Instance.CompleteTask(0);
    }

    private void OnTriggerExit(Collider other)
    {
        // Khi rút tay ra
        if (other.CompareTag("PlayerHand"))
        {
            isHandHovering = false;
            if (warningUI != null) warningUI.SetActive(false);
        }
    }

    private void ExecuteTeleport()
    {
        if (teleportationProvider != null && fireCorridorSpawnPoint != null)
        {
            // 1. Tạo một yêu cầu dịch chuyển chuẩn của XR Locomotion
            UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest()
            {
                destinationPosition = fireCorridorSpawnPoint.position,
                destinationRotation = fireCorridorSpawnPoint.rotation,
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward // Ép người chơi xoay mặt theo đúng trục Z của điểm đích
            };

            // 2. Đẩy yêu cầu vào hàng đợi để Locomotion System xử lý
            teleportationProvider.QueueTeleportRequest(request);
            
            // 3. Reset trạng thái
            isHandHovering = false;
            if (warningUI != null) warningUI.SetActive(false);
            
            Debug.Log("Game Over: Đã dịch chuyển bằng Locomotion!");
        }
        else
        {
            Debug.LogWarning("Chưa gán Teleportation Provider hoặc Điểm đích!");
        }
    }
}