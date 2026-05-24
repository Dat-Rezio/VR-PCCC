using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AnimatedFacePlayer : MonoBehaviour
{
    [Header("Cài đặt xoay (Billboard)")]
    public bool facePlayer = true; 
    public bool keepUpright = true;
    
    [Tooltip("Nếu mũi tên bị xoay ngang, hãy nhập 90, -90 hoặc 180 vào đây để chỉnh lại hướng.")]
    public float zRotationOffset = 0f;

    [Header("Animation - Bay Lên Xuống")]
    public bool enableFloat = true;
    public float floatAmplitude = 0.03f; 
    public float floatSpeed = 2f;

    [Header("Animation - Lắc Trái Phải")]
    public bool enableSway = false;
    public float swayAmplitude = 0.03f;
    public float swaySpeed = 2f;

    [Header("Tương tác Ẩn/Hiện")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable targetInteractable;
    public bool showAgainOnRelease = false;

    // --- CẬP NHẬT: TÙY CHỌN BÁM THEO VẬT THỂ ---
    [Header("Bám theo vật thể (Tùy chọn)")]
    [Tooltip("Kéo đồ vật (ví dụ: Điện thoại) vào đây. Mũi tên sẽ luôn đi theo vật này. Nếu để trống, nó sẽ đứng yên ở vị trí ban đầu.")]
    public Transform followTarget;
    
    private Transform mainCamera;
    private Vector3 startWorldPosition;
    private Vector3 positionOffset; // Lưu khoảng cách giữa mũi tên và đồ vật

    void Start()
    {
        if (Camera.main != null) mainCamera = Camera.main.transform;
        
        // Kiểm tra xem có cần bám theo vật nào không
        if (followTarget != null)
        {
            // Ghi nhớ khoảng cách từ vật thể đến mũi tên (ví dụ: cao hơn 20cm)
            positionOffset = transform.position - followTarget.position;
        }
        else
        {
            // Nếu không có, lưu cứng vị trí ban đầu như cũ
            startWorldPosition = transform.position;
        }

        if (targetInteractable != null)
        {
            targetInteractable.selectEntered.AddListener(OnGrabbed);
            targetInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnDestroy()
    {
        if (targetInteractable != null)
        {
            targetInteractable.selectEntered.RemoveListener(OnGrabbed);
            targetInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        gameObject.SetActive(false);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (showAgainOnRelease) gameObject.SetActive(true);
    }

    public void HideIndicator() => gameObject.SetActive(false);
    public void ShowIndicator() => gameObject.SetActive(true);

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // 1. XOAY MẶT VÀO NGƯỜI CHƠI
        if (facePlayer)
        {
            Vector3 directionToCamera = transform.position - mainCamera.position;
            if (keepUpright) directionToCamera.y = 0;

            if (directionToCamera != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(directionToCamera);
                transform.rotation = lookRot * Quaternion.Euler(0, 0, zRotationOffset);
            }
        }

        // 2. XÁC ĐỊNH TÂM DAO ĐỘNG
        Vector3 basePosition;
        if (followTarget != null)
        {
            // Lấy vị trí hiện tại của đồ vật + khoảng cách ban đầu
            basePosition = followTarget.position + positionOffset;
        }
        else
        {
            basePosition = startWorldPosition;
        }

        // 3. TẠO ANIMATION BAY/LẮC
        if (enableFloat || enableSway)
        {
            float floatOffset = 0f;
            float swayOffset = 0f;

            if (enableFloat) floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            if (enableSway) swayOffset = Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;

            transform.position = basePosition 
                               + Vector3.up * floatOffset 
                               + transform.right * swayOffset;
        }
    }
}