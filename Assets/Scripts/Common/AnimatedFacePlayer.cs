using UnityEngine;

public class AnimatedFacePlayer : MonoBehaviour
{
    [Header("Cài đặt xoay (Billboard)")]
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

    private Transform mainCamera;
    
    // Dùng vị trí thế giới thay vì vị trí tương đối để tránh lỗi do Parent bị xoay
    private Vector3 startWorldPosition;

    void Start()
    {
        if (Camera.main != null) mainCamera = Camera.main.transform;
        
        // Lưu lại vị trí thực tế trong thế giới
        startWorldPosition = transform.position;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // 1. XOAY MẶT VÀO NGƯỜI CHƠI
        Vector3 directionToCamera = transform.position - mainCamera.position;
        if (keepUpright) directionToCamera.y = 0;

        if (directionToCamera != Vector3.zero)
        {
            // Kết hợp góc nhìn Camera và bù trừ góc Z để mũi tên chỉ đúng hướng
            Quaternion lookRot = Quaternion.LookRotation(directionToCamera);
            transform.rotation = lookRot * Quaternion.Euler(0, 0, zRotationOffset);
        }

        // 2. TẠO ANIMATION BAY/LẮC
        if (enableFloat || enableSway)
        {
            float floatOffset = 0f;
            float swayOffset = 0f;

            if (enableFloat) floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            if (enableSway) swayOffset = Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;

            // Vector3.up đảm bảo mũi tên LUÔN bay lên/xuống theo trọng lực của thế giới
            // transform.right đảm bảo lắc trái/phải luôn theo góc nhìn hiện tại
            transform.position = startWorldPosition 
                               + Vector3.up * floatOffset 
                               + transform.right * swayOffset;
        }
    }
}